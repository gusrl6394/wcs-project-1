using Microsoft.EntityFrameworkCore;
using Wcs.Domain;
using Wcs.Infrastructure.Persistence;

namespace Wcs.Infrastructure;

// 도메인에서 정의한 의도를 코드로 노출하는 리포지토리 패턴
// 컨트롤러(API)와 워커가 도메인 의미에 맞게 데이터를 가져오고 저장하도록 돕습니다
// 실제 저장/조회 로직은 EF Core가 수행하므로, 상위 계층은 EF 구문/추적을 직접 몰라도 됩니다


public interface IJobRepository
{
    /*
      FindScheduledByPalletAsync: “지정 팔레트의 대기중(Scheduled) 작업 한 건 찾기”라는 업무 의미를 표현
      AddAsync: “새 작업 추가”라는 업무 의미를 표현
      SaveChangesAsync: “변경사항 저장”이라는 업무 의미를 표현
    */
    Task<Job?> FindScheduledByPalletAsync(string palletId, CancellationToken ct);
    Task AddAsync(Job job, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public class JobRepository(WcsDbContext db) : IJobRepository
{
    /*
        FindScheduledByPalletAsync: 지정 팔레트의 대기중(Scheduled) 작업 한 건 찾기
        Reference 참고하기 때문에 JobState enum 사용가능
        FirstOrDefaultAsync: 조건에 맞는 첫 번째 항목을 비동기로 조회, 없으면 null 반환
    */
    public Task<Job?> FindScheduledByPalletAsync(string palletId, CancellationToken ct)
        => db.Jobs.FirstOrDefaultAsync(j => j.PalletId == palletId && j.State == JobState.Scheduled, ct);

    public Task AddAsync(Job job, CancellationToken ct) { db.Jobs.Add(job); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

// Command Interface
public interface ICommandRepository
{
    IQueryable<Command> QueryPending();
    Task AddAsync(Command cmd, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

// Command Repository Implementation
public class CommandRepository(WcsDbContext db) : ICommandRepository
{
    /*
     QueryPending(): 아직 처리되지 않은 명령 스트림을 상위가 조합 가능한 LINQ로 가져가도록 IQueryable로 노출
     정렬 기준: CreatedAt 오름차순으로 가져오면 오래된 것부터 처리
     IQueryable 반환 이유
      ㄴ 장점: 상위에서 추가 필터/페이징/개수 제한을 유연하게 조합.
      ㄴ 주의: DbContext 수명(scope) 내에서만 열거해야 함. (스코프 밖에서는 ObjectDisposedException 위험)
    */
    public IQueryable<Command> QueryPending()
        => db.Commands.Where(c => c.State == CommandState.Pending).OrderBy(c => c.CreatedAt);

    public Task AddAsync(Command cmd, CancellationToken ct) { db.Commands.Add(cmd); return Task.CompletedTask; }
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
