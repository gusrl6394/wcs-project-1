using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using NModbus;
using Wcs.Domain.Field;
using Wcs.Domain.Equipment;

namespace Wcs.Workers.Workers
{
    /// <summary>
    /// Modbus TCP를 통해 주기적으로 태그 값을 읽어와
    /// 도메인 설비 상태(IEquipmentStatusService)에 반영하는 Worker.
    /// </summary>
    public class ModbusPollingWorker : BackgroundService
    {
        private readonly ILogger<ModbusPollingWorker> _logger;
        private readonly IFieldBusChannel _channel;
        private readonly IFieldTagRepository _tagRepository;            // 태그 메타데이터
        private readonly IEquipmentStatusService _equipmentStatus;      // 도메인 서비스

        public ModbusPollingWorker(
            ILogger<ModbusPollingWorker> logger,
            IFieldBusChannel channel,
            IFieldTagRepository tagRepository,
            IEquipmentStatusService equipmentStatus)
        {
            _logger = logger;
            _channel = channel;
            _tagRepository = tagRepository;
            _equipmentStatus = equipmentStatus;
        }

        /// <summary>
        /// 메인 폴링 루프
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Modbus polling worker started.");

            const byte slaveId = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var allTags = await _tagRepository.GetAllAsync(stoppingToken);

                    // 1) 입력 방향 태그만 폴링 (Output은 명령/Write 용)
                    var inputTags = allTags
                        .Where(t => t.Direction == IoDirection.Input)
                        .ToList();

                    if (inputTags.Count == 0)
                    {
                        _logger.LogDebug("입력(Input) 태그가 없습니다. 다음 주기에 다시 시도합니다.");
                    }
                    else
                    {
                        // 2) DeviceId + IoDataType 기준으로 그룹핑
                        var groups = inputTags
                            .GroupBy(t => new { t.DeviceId, t.DataType });

                        foreach (var group in groups)
                        {
                            await PollGroupAsync(
                                group.Key.DeviceId,
                                slaveId,
                                group.Key.DataType,
                                group.ToList(),
                                stoppingToken);
                        }
                    }
                }
                catch (SocketException)
                {
                    // 여기서는 스택트레이스 남기지 않고, "서버 안 켜져 있음" 정도만 경고로 기록
                    _logger.LogWarning(
                        "Modbus 서버에 연결할 수 없습니다. (슬레이브 서버가 꺼져 있거나, 포트 502가 닫혀 있음). 계속 재시도합니다.");

                    // 바로 다시 예외를 던지지 않고, 지정된 주기 뒤에 재시도
                }
                catch (IOException ex) when (ex.InnerException is SocketException)
                {
                    _logger.LogWarning(
                        "Modbus 통신 중 연결이 끊어졌습니다. (전송 중 소켓 연결 종료). 계속 재시도합니다.");
                }
                catch (SlaveException ex)
                {
                    _logger.LogWarning(
                        "Modbus 슬레이브에서 오류 응답을 보냈습니다. FunctionCode={FunctionCode}, ExceptionCode={ExceptionCode}",
                        ex.FunctionCode, ex.SlaveExceptionCode);
                    // 여기서는 시스템 죽이면 안 되고, 그냥 로그만 남기고 다음 주기에서 다시 시도
                }
                catch (Exception ex)
                {
                    // 그 외 예외는 한 번만 전체 스택 로그
                    _logger.LogError(ex, "Unexpected error while polling Modbus.");
                }

                // 폴링 주기 (필요에 따라 조정)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        /// <summary>
        /// 같은 DeviceId + SlaveId + IoDataType에 속한 태그들을
        /// 한 번의 Modbus 호출로 읽어와서 태그별 값으로 매핑.
        /// </summary>
        private async Task PollGroupAsync(
            string deviceId,
            byte slaveId,
            IoDataType dataType,
            List<FieldTag> tags,
            CancellationToken ct)
        {
            if (tags.Count == 0)
                return;

            var minAddress = tags.Min(t => t.Address);
            var maxAddress = tags.Max(t => t.Address);
            var numberOfPoints = (ushort)(maxAddress - minAddress + 1);

            _logger.LogDebug(
                "Polling Device={DeviceId}, SlaveId={SlaveId}, Type={Type}, AddressRange={Start}-{End}",
                deviceId, slaveId, dataType, minAddress, maxAddress);

            bool[]? boolValues = null;
            ushort[]? regValues = null;

            try
            {
                switch (dataType)
                {
                    case IoDataType.Coil:
                        boolValues = await _channel.ReadCoilsAsync(
                            slaveId, minAddress, numberOfPoints, ct);
                        break;

                    case IoDataType.DiscreteInput:
                        boolValues = await _channel.ReadDiscreteInputsAsync(
                            slaveId, minAddress, numberOfPoints, ct);
                        break;

                    case IoDataType.HoldingRegister:
                        regValues = await _channel.ReadHoldingRegistersAsync(
                            slaveId, minAddress, numberOfPoints, ct);
                        break;

                    case IoDataType.InputRegister:
                        regValues = await _channel.ReadInputRegistersAsync(
                            slaveId, minAddress, numberOfPoints, ct);
                        break;

                    default:
                        _logger.LogWarning("지원하지 않는 IoDataType: {Type}", dataType);
                        return;
                }
            }
            catch (SlaveException ex)
            {
                _logger.LogWarning(
                    "Modbus 슬레이브에서 오류 응답. Device={DeviceId}, SlaveId={SlaveId}, Type={Type}, " +
                    "Start={Start}, Count={Count}, FunctionCode={FunctionCode}, ExceptionCode={ExceptionCode}",
                    deviceId, slaveId, dataType,
                    minAddress, numberOfPoints,
                    ex.FunctionCode, ex.SlaveExceptionCode);
                return;
            }
            catch (SocketException)
            {
                _logger.LogWarning(
                    "Modbus 서버에 연결할 수 없습니다. Device={DeviceId}, SlaveId={SlaveId}. 계속 재시도합니다.",
                    deviceId, slaveId);
                return;
            }

            // 3) TagId -> Value 매핑
            var valueByTagId = new Dictionary<string, object?>();

            foreach (var tag in tags)
            {
                var offset = tag.Address - minAddress;
                if (offset < 0)
                    continue;

                switch (dataType)
                {
                    case IoDataType.Coil:
                    case IoDataType.DiscreteInput:
                        if (boolValues == null || offset >= boolValues.Length)
                        {
                            _logger.LogWarning(
                                "Bool index out of range. Tag={TagId}, Address={Address}, Offset={Offset}, Length={Length}",
                                tag.Id, tag.Address, offset, boolValues?.Length ?? 0);
                            continue;
                        }

                        valueByTagId[tag.Id] = boolValues[offset];
                        break;

                    case IoDataType.HoldingRegister:
                    case IoDataType.InputRegister:
                        if (regValues == null || offset >= regValues.Length)
                        {
                            _logger.LogWarning(
                                "Register index out of range. Tag={TagId}, Address={Address}, Offset={Offset}, Length={Length}",
                                tag.Id, tag.Address, offset, regValues?.Length ?? 0);
                            continue;
                        }

                        var raw = regValues[offset];

                        if (tag.BitIndex.HasValue)
                        {
                            var bit = (raw & (1 << tag.BitIndex.Value)) != 0;
                            valueByTagId[tag.Id] = bit;
                        }
                        else
                        {
                            valueByTagId[tag.Id] = raw; // ushort 그대로
                        }
                        break;
                }
            }

            // 4) 도메인 서비스에 전달해서 설비 상태 반영
            if (valueByTagId.Count > 0)
            {
                var asObjectDict = valueByTagId.ToDictionary(
                        kv => kv.Key,
                        kv => (object?)kv.Value);

                // 인터페이스 시그니처와 맞춤
                await _equipmentStatus.UpdateFromFieldAsync(deviceId, asObjectDict, ct);
            }

            // 5) (선택) 로그로 현재 값 확인
            if (valueByTagId.Count > 0)
            {
                var refPrefix = dataType switch
                {
                    IoDataType.Coil           => "0",
                    IoDataType.DiscreteInput  => "1",
                    IoDataType.InputRegister  => "3",
                    IoDataType.HoldingRegister=> "4",
                    _                         => ""
                };

                var logText = string.Join(", ",
                    tags.Select(t =>
                    {
                        valueByTagId.TryGetValue(t.Id, out var v);
                        var refAddr = t.Address + 1; // 0 -> 00001
                        return $"{t.Id}({refPrefix}{refAddr:D4})={v}";
                    }));

                _logger.LogInformation("[POLL][{Device}] {Values}", deviceId, logText);
            }
        }
    }
}
