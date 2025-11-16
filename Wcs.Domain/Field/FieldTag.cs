namespace Wcs.Domain.Field
{
    public enum IoDataType
    {
        Coil,
        DiscreteInput,
        HoldingRegister,
        InputRegister
    }

    public enum IoDirection
    {
        Input,   // 설비 -> WCS
        Output   // WCS -> 설비
    }

    // 태그/IO 포인트 (어떤 값이 어느 주소인지)
    public class FieldTag
    {
        public string Id { get; init; } = default!;        // "CV01_RUN_FB" 등
        public string DeviceId { get; init; } = default!;  // FieldDevice.Id 참조
        public IoDataType DataType { get; init; }
        public IoDirection Direction { get; init; }

        public ushort Address { get; init; }               // Modbus address
        public ushort? BitIndex { get; init; }             // 워드 안에서 몇 비트인지 필요하면 사용

        // 의미적인 정보들
        public string Description { get; init; } = string.Empty;
        public string? EquipmentId { get; init; }          // 도메인 설비랑 연결하고 싶으면
        public string? PropertyName { get; init; }         // "IsRunning", "HasError" 등
    }
}
