using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Wcs.Domain.Field;
using NModbus;

namespace Wcs.Workers.Workers
{
    /// <summary>
    /// Modbus TCP 코일 쓰기 테스트용 Worker.
    /// - slaveId = 1, address = 0 으로 가정
    /// - 2초마다 true/false 토글해서 WriteSingleCoilAsync 호출
    /// - 바로 ReadCoilsAsync(1개)로 읽어서 값 확인
    /// </summary>
    public class ModbusWriteTestWorker : BackgroundService
    {
        private readonly ILogger<ModbusWriteTestWorker> _logger;
        private readonly IFieldBusChannel _channel;

        public ModbusWriteTestWorker(
            ILogger<ModbusWriteTestWorker> logger,
            IFieldBusChannel channel)
        {
            _logger = logger;
            _channel = channel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const byte slaveId = 1;
            const ushort address = 0;

            bool current = false;

            _logger.LogInformation(
                "Modbus write test worker started. Slave={Slave}, Address={Address}",
                slaveId, address);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    current = !current; // true / false 토글

                    // 1) 코일 쓰기
                    await _channel.WriteSingleCoilAsync(slaveId, address, current, stoppingToken);

                    _logger.LogInformation(
                        "[WRITE] Coil(slave:{Slave}, addr:{Addr}) = {Value}",
                        slaveId, address, current);

                    // 2) 바로 읽어서 확인 (선택)
                    var coils = await _channel.ReadCoilsAsync(slaveId, address, 1, stoppingToken);
                    var readValue = coils.FirstOrDefault();

                    _logger.LogInformation(
                        "[READ ] Coil(slave:{Slave}, addr:{Addr}) = {Value}",
                        slaveId, address, readValue);
                }
                catch (SocketException)
                {
                    _logger.LogWarning(
                        "Modbus 서버에 연결할 수 없습니다. (슬레이브 서버가 꺼져 있거나, 포트 502가 닫혀 있음). 계속 재시도합니다.");
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
                    _logger.LogError(ex, "Error while writing coil.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
