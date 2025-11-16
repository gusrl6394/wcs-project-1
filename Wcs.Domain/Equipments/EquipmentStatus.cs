namespace Wcs.Domain.Equipments
{
    // 도메인 서비스: “태그 <-> 설비 상태” 매핑 담당
    public class EquipmentStatus
    {
        public string EquipmentId { get; init; } = default!;
        public bool IsRunning { get; set; }
        public bool HasError { get; set; }
        public int? ErrorCode { get; set; }
        // 필요하면 더...
    }

    /*
     IEquipmentStatusService 구현체는:
     IFieldBusChannel + FieldTag 조회를 사용해서:
      ㄴ 설비 상태 읽기 (Read)
      ㄴ 명령 쓰기 (Write)
    */
    // Domain에는 설비/태그/상태의 의미를 정의하고, 외부 통신은 인터페이스로만 바라보자.
    public interface IEquipmentStatusRepository
    {
        Task<EquipmentStatus?> GetAsync(string equipmentId);
        Task SaveAsync(EquipmentStatus status);
    }

    public interface IEquipmentStatusService
    {
        Task UpdateFromFieldAsync(CancellationToken ct = default);
        Task CommandStartAsync(string equipmentId, CancellationToken ct = default);
        Task CommandStopAsync(string equipmentId, CancellationToken ct = default);
    }
}
