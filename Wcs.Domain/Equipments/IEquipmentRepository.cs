using System.Threading;
using System.Threading.Tasks;

namespace Wcs.Domain.Equipment
{
    public interface IEquipmentRepository
    {
        Task<Equipment?> GetByIdAsync(string equipmentId, CancellationToken ct = default);
        Task SaveAsync(Equipment equipment, CancellationToken ct = default);
    }
}
