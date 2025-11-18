using System.Net.Sockets;
using System.Threading;
using NModbus;
using Wcs.Domain.Field;

namespace Wcs.Infrastructure.Field.Modbus
{
    // Modbus TCP 구현 (Domain 인터페이스 실제화)
    public sealed class ModbusTcpChannel : IFieldBusChannel
    {
        private readonly string _ip;
        private readonly int _port;
        private TcpClient _tcpClient = null!;
        private IModbusMaster _master = null!;

        // ★ 추가: 동시 접근 막기 위한 세마포어
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);


        public ModbusTcpChannel(string ip, int port = 502)
        {
            _ip = ip;
            _port = port;
        }

        /// <summary>
        /// 연결이 끊어졌으면 재연결.
        /// </summary>
        private void EnsureConnected()
        {
            // 이미 연결되어 있으면 재연결 안 함
            if (_tcpClient != null && _tcpClient.Connected && _master != null)
                return;

            // 기존 연결 정리
            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch
            {
                // 무시
            }
            
            // 새 연결 시도
            _tcpClient = new TcpClient();
            _tcpClient.Connect(_ip, _port);  // 여기서 SocketException 발생 가능 (Worker에서 처리)

            var factory = new ModbusFactory();
            _master = factory.CreateMaster(_tcpClient);
        }

        // ---------------- READ ----------------

        public async Task<bool[]> ReadCoilsAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                EnsureConnected();
                return _master!.ReadCoils(slaveId, startAddress, numberOfPoints);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<bool[]> ReadDiscreteInputsAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                EnsureConnected();
                return _master!.ReadInputs(slaveId, startAddress, numberOfPoints);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                EnsureConnected();
                return _master!.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task<ushort[]> ReadInputRegistersAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                EnsureConnected();
                return _master!.ReadInputRegisters(slaveId, startAddress, numberOfPoints);
            }
            finally
            {
                _sync.Release();
            }
        }

        // ---------------- WRITE ----------------

        public async Task WriteSingleCoilAsync(
            byte slaveId,
            ushort address,
            bool value,
            CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                EnsureConnected();
                _master!.WriteSingleCoil(slaveId, address, value);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task WriteSingleRegisterAsync(
            byte slaveId,
            ushort address,
            ushort value,
            CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                EnsureConnected();
                _master!.WriteSingleRegister(slaveId, address, value);
            }
            finally
            {
                _sync.Release();
            }
        }

        // ---------------- DISPOSE ----------------

        public async ValueTask DisposeAsync()
        {
            _tcpClient.Close();
            _tcpClient.Dispose();
            await Task.CompletedTask;
        }
    }
}
