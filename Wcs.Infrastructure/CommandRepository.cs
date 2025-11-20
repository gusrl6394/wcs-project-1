using System;
using System.Collections.Generic;
using System.Text;
using Wcs.Domain;
using Wcs.Infrastructure.Persistence;

namespace Wcs.Infrastructure
{
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
}
