namespace Wcs.Domain.Field
{
    public interface IFieldBusChannel : IAsyncDisposable
    {
        // --- READ ---
        Task<bool[]> ReadCoilsAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default);

        Task<bool[]> ReadDiscreteInputsAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default);

        Task<ushort[]> ReadHoldingRegistersAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default);

        Task<ushort[]> ReadInputRegistersAsync(
            byte slaveId,
            ushort startAddress,
            ushort numberOfPoints,
            CancellationToken ct = default);

        // --- WRITE ---
        Task WriteSingleCoilAsync(
            byte slaveId,
            ushort address,
            bool value,
            CancellationToken ct = default);

        Task WriteSingleRegisterAsync(
            byte slaveId,
            ushort address,
            ushort value,
            CancellationToken ct = default);

        // 필요하면 나중에: 여러 개 쓰기 (WriteMultipleCoils / Registers)도 추가
    }
}
