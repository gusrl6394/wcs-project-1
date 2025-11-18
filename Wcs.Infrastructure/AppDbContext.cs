using Microsoft.EntityFrameworkCore;
using Wcs.Domain;

namespace Wcs.Infrastructure;

// EF Core의 DbContext 구현
// Job과 Command 테이블에 해당하는 DbSet을 노출하고, 모델 구성(OnModelCreating) 에서 키/인덱스/제약을 정의

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Entity sets
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Command> Commands => Set<Command>();

    /*
        * Model configuration
         ㄴ e.HasKey : 기본키 설정
         ㄴ e.Property : 속성 설정
         ㄴ e.HasIndex : 인덱스 설정
         HasIndex(x => new { x.PalletId, x.State }): 팔레트별 작업 상태 조회
         HasIndex(State, CreatedAt): 워커가 Pending → 오래된 것 순으로 가져올 때 유리

    */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PalletId).IsRequired();
            e.HasIndex(x => new { x.PalletId, x.State });
        });

        modelBuilder.Entity<Command>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.State, x.CreatedAt });
        });
    }
}
