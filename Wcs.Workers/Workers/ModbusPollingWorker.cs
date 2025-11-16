using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using Wcs.Domain.Field; // IFieldBusChannel 등

namespace Wcs.Workers.Workers
{
    /// <summary>
    /// Modbus TCP를 통해 주기적으로 태그 값을 읽어와
    /// 도메인 설비 상태(IEquipmentStatusService)에 반영하는 Worker.
    /// 
    /// 가장 단순한 버전:
    /// - slaveId = 1
    /// - startAddress = 0
    /// - numberOfPoints = 10
    /// </summary>
    public class ModbusPollingWorker : BackgroundService
    {
        private readonly ILogger<ModbusPollingWorker> _logger;
        private readonly IFieldBusChannel _channel;
        // private readonly IFieldTagRepository _tagRepository;            // 태그 메타데이터
        // private readonly IEquipmentStatusService _equipmentStatus;      // 도메인 서비스

        public ModbusPollingWorker(
            ILogger<ModbusPollingWorker> logger,
            IFieldBusChannel channel)
            // IFieldTagRepository tagRepository,
            // IEquipmentStatusService equipmentStatus)
        {
            _logger = logger;
            _channel = channel;
            // _tagRepository = tagRepository;
            // _equipmentStatus = equipmentStatus;
        }

        /// <summary>
        /// 메인 폴링 루프
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Modbus polling worker started.");

            const byte slaveId = 1;
            const ushort startAddress = 0;
            const ushort points = 10;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ★ 여기 하드 코딩 부분
                    var coils = await _channel.ReadCoilsAsync(slaveId, startAddress, points, stoppingToken);

                    // bool[] 그대로 찍으면 "True,False..." 이런식이라, 1/0으로 바꿔서 찍음
                    var coilsText = string.Join(",", coils.Select(c => c ? "1" : "0"));

                    _logger.LogInformation("Coils (slave:{Slave}, addr:{Addr}, count:{Count}) = {Coils}",
                        slaveId, startAddress, points, coilsText);

                    /*
                    // 1) Tag 목록 (예: CV01 관련 태그들) 가져오기
                    var tags = await _tagRepository.GetAllAsync(stoppingToken);

                    if (tags == null || tags.Count == 0)
                    {
                        _logger.LogDebug("No field tags configured. Skipping polling.");
                    }

                    else
                    {
                        // 2) DeviceId + SlaveId + DataType 별로 그룹핑해서 효율적으로 읽기
                        //    (여기서는 FieldTag에 SlaveId 속성이 있다고 가정)
                        var groups = tags
                            .GroupBy(t => new { t.DeviceId, t.SlaveId, t.DataType });

                        foreach (var group in groups)
                        {
                            await PollGroupAsync(group.Key.DeviceId,
                                                 group.Key.SlaveId,
                                                 group.Key.DataType,
                                                 group.ToList(),
                                                 stoppingToken);
                        }
                    }
                    */
                }
                catch (SocketException)
                {
                    // 여기서는 스택트레이스 남기지 않고, "서버 안 켜져 있음" 정도만 경고로 기록
                    _logger.LogWarning(
                        "Modbus 서버에 연결할 수 없습니다. (슬레이브 서버가 꺼져 있거나, 포트 502가 닫혀 있음). 계속 재시도합니다.");

                    // 바로 다시 예외를 던지지 않고, 지정된 주기 뒤에 재시도
                }
                catch (Exception ex)
                {
                    // 그 외 예외는 한 번만 전체 스택 로그
                    _logger.LogError(ex, "Unexpected error while polling Modbus.");
                }

                // 폴링 주기 (필요에 따라 조정)
                await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
            }
        }

        /// <summary>
        /// 같은 Device + SlaveId + DataType에 속한 태그들을 한 번에 읽고
        /// IEquipmentStatusService에 반영하는 함수.
        /// </summary>
        /*
        private async Task PollGroupAsync(
            string deviceId,
            byte slaveId,
            IoDataType dataType,
            List<FieldTag> tags,
            CancellationToken ct)
        {
            if (tags.Count == 0)
                return;

            // Modbus 주소 범위 계산
            var minAddress = tags.Min(t => t.Address);
            var maxAddress = tags.Max(t => t.Address);
            var numberOfPoints = (ushort)(maxAddress - minAddress + 1);

            _logger.LogDebug(
                "Polling Device={DeviceId}, SlaveId={SlaveId}, Type={Type}, AddressRange={Start}-{End}",
                deviceId, slaveId, dataType, minAddress, maxAddress);

            // 1) Modbus 값 읽기
            bool[]? boolValues = null;
            ushort[]? regValues = null;

            switch (dataType)
            {
                case IoDataType.Coil:
                    boolValues = await _channel.ReadCoilsAsync(slaveId, minAddress, numberOfPoints, ct);
                    break;

                case IoDataType.DiscreteInput:
                    boolValues = await _channel.ReadDiscreteInputsAsync(slaveId, minAddress, numberOfPoints, ct);
                    break;

                case IoDataType.HoldingRegister:
                    regValues = await _channel.ReadHoldingRegistersAsync(slaveId, minAddress, numberOfPoints, ct);
                    break;

                case IoDataType.InputRegister:
                    regValues = await _channel.ReadInputRegistersAsync(slaveId, minAddress, numberOfPoints, ct);
                    break;

                default:
                    _logger.LogWarning("Unsupported IoDataType: {Type}", dataType);
                    return;
            }

            // 2) 태그별로 값을 해석 (비트 인덱스 반영 포함)
            var valueByTagId = new Dictionary<string, object?>();

            foreach (var tag in tags)
            {
                var offset = tag.Address - minAddress;

                object? value = null;

                if (boolValues != null)
                {
                    // Coil / DiscreteInput
                    if (offset < 0 || offset >= boolValues.Length)
                    {
                        _logger.LogWarning(
                            "Bool index out of range. Tag={TagId}, Address={Address}, Offset={Offset}",
                            tag.Id, tag.Address, offset);
                        continue;
                    }

                    var raw = boolValues[offset];

                    // BitIndex가 있으면 워드의 특정 비트로 해석하는 경우도 있지만
                    // bool 배열에서는 이미 비트 단위이므로 BitIndex는 무시하거나, 필요시 다른 의미로 사용
                    value = raw;
                }
                else if (regValues != null)
                {
                    // Holding/Input Register
                    if (offset < 0 || offset >= regValues.Length)
                    {
                        _logger.LogWarning(
                            "Register index out of range. Tag={TagId}, Address={Address}, Offset={Offset}",
                            tag.Id, tag.Address, offset);
                        continue;
                    }

                    var raw = regValues[offset];

                    if (tag.BitIndex.HasValue)
                    {
                        // 레지스터 안의 특정 비트를 태그로 사용하는 경우
                        var bit = (raw & (1 << tag.BitIndex.Value)) != 0;
                        value = bit;
                    }
                    else
                    {
                        // 그냥 레지스터 값 자체 사용 (ushort)
                        value = raw;
                    }
                }

                valueByTagId[tag.Id] = value;
            }

            // 3) 도메인 서비스에 설비 상태 반영
            if (valueByTagId.Count > 0)
            {
                await _equipmentStatus.UpdateFromFieldAsync(deviceId, valueByTagId, ct);
            }
        }
        */
    }
}
