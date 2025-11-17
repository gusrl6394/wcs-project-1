using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wcs.Domain.Field;
using Wcs.Infrastructure.Persistence;

namespace Wcs.Infrastructure.Field
{
    /// <summary>
    /// EF Core 기반 FieldTag 리포지토리 구현.
    /// WcsDbContext의 FieldTags 테이블에서 태그 메타데이터를 조회한다.
    /// </summary>
    public class EfFieldTagRepository : IFieldTagRepository
    {
        private readonly WcsDbContext _db;

        public EfFieldTagRepository(WcsDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<FieldTag>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.FieldTags
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
