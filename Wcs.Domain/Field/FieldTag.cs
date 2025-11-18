/*
 - IO 태그 메타데이터 정의
 - 어떤 태그가 어느 디바이스(DeviceId), 어떤 타입(IoDataType), 방향(IoDirection), 주소(Address)에 있는지 표현
 - 나중에 이 태그를 설비(EquipmentId) + PropertyName에 연결해서 도메인과 매핑
*/

namespace Wcs.Domain.Field
{
    public enum IoDataType
    {
        Coil,           // 0xxxx
        DiscreteInput,  // 1xxxx
        HoldingRegister,// 4xxxx
        InputRegister   // 3xxxx
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

    public interface IFieldTagRepository
    {
        Task<IReadOnlyList<FieldTag>> GetAllAsync(CancellationToken ct = default);
    }
}
