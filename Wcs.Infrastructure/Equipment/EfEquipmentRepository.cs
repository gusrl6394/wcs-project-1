using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wcs.Domain.Equipment;
using Wcs.Infrastructure.Persistence;

namespace Wcs.Infrastructure.Equipment
{
    /// <summary>
    /// EF Core 기반 설비 리포지토리 구현.
    /// WcsDbContext.Equipments 테이블을 통해 설비 상태를 조회/저장한다.
    /// </summary>
    public class EfEquipmentRepository : IEquipmentRepository
    {
        private readonly WcsDbContext _db;

        public EfEquipmentRepository(WcsDbContext db)
        {
            _db = db;
        }

        public async Task<EquipmentEntity?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return await _db.Equipments
                .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task SaveAsync(EquipmentEntity equipment, CancellationToken ct = default)
        {
            // 이미 존재하는지 확인
            var exists = await _db.Equipments
                .AnyAsync(e => e.Id == equipment.Id, ct);

            if (exists)
            {
                _db.Equipments.Update(equipment);
            }
            else
            {
                await _db.Equipments.AddAsync(equipment, ct);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
