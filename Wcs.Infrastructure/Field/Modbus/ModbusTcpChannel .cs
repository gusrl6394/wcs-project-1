using System.Net.Sockets;
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

        public Task<bool[]> ReadCoilsAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                EnsureConnected();
                return _master.ReadCoils(slaveId, startAddress, numberOfPoints);
            }, ct);
        }

        public Task<bool[]> ReadDiscreteInputsAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                EnsureConnected();
                // NModbus에서 Discrete Inputs는 ReadInputs
                return _master.ReadInputs(slaveId, startAddress, numberOfPoints);
            }, ct);
        }

        public Task<ushort[]> ReadHoldingRegistersAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                EnsureConnected();
                return _master.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
            }, ct);
        }

        public Task<ushort[]> ReadInputRegistersAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                EnsureConnected();
                return _master.ReadInputRegisters(slaveId, startAddress, numberOfPoints);
            }, ct);
        }

        // ---------------- WRITE ----------------

        public Task WriteSingleCoilAsync(
            byte slaveId,
            ushort address,
            bool value,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                EnsureConnected();
                _master.WriteSingleCoil(slaveId, address, value);
            }, ct);
        }

        public Task WriteSingleRegisterAsync(
            byte slaveId,
            ushort address,
            ushort value,
            CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                EnsureConnected();
                _master.WriteSingleRegister(slaveId, address, value);
            }, ct);
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
