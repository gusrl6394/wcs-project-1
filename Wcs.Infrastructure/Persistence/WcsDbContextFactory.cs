using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Wcs.Infrastructure.Persistence
{
    public class WcsDbContextFactory : IDesignTimeDbContextFactory<WcsDbContext>
    {
        public WcsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WcsDbContext>();

            // ✅ 디자인 타임용 간단 연결 문자열 (SQLite 예시)
            // 실제 런타임에서 쓰는 DB와 다르면 나중에 여기만 맞춰주면 된다.
            optionsBuilder.UseSqlite("Data Source=wcs.db");

            // 만약 실제로 SqlServer / PostgreSQL 등을 쓴다면,
            // 여기만 바꿔주면 됨:
            // optionsBuilder.UseSqlServer("Server=...;Database=...;Trusted_Connection=True;TrustServerCertificate=True;");
            // optionsBuilder.UseNpgsql("Host=...;Database=...;Username=...;Password=...;");

            return new WcsDbContext(optionsBuilder.Options);
        }
    }
}
