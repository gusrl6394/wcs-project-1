using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Wcs.Domain;
using Wcs.Infrastructure.Persistence;

namespace Wcs.Infrastructure
{
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
}
