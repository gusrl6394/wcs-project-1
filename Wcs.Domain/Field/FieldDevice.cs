namespace Wcs.Domain.Field
{
    // 설비 자체 (Device/Equipment)
    public class FieldDevice
    {
        public string Id { get; init; } = default!;       // "CV01", "LIFT01" 같은 키
        public string Name { get; init; } = default!;     // 화면에 보여줄 이름
        public string Protocol { get; init; } = "ModbusTcp";
        public string Address { get; init; } = default!;  // "192.168.0.10:502" 또는 "COM3" 등
        public byte SlaveId { get; init; }                // Modbus slave id (TCP에서도 종종 씀)
    }
}
