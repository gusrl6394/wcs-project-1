using System.Threading;
using System.Threading.Tasks;
using EquipmentEntity = Wcs.Domain.Equipment.Equipment;

/*
 - 설비 엔티티를 조회/저장하는 도메인 리포지토리 인터페이스
 - 실제 구현(EF Core 등)은 Infrastructure에 들어가게 됨
*/
namespace Wcs.Domain.Equipment
{
    public interface IEquipmentRepository
    {
        Task<Equipment?> GetByIdAsync(string equipmentId, CancellationToken ct = default);
        Task SaveAsync(EquipmentEntity equipment, CancellationToken ct = default);
    }
}
