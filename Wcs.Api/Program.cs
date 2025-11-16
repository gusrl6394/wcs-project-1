using Microsoft.EntityFrameworkCore;
using Wcs.Domain;
using Wcs.Infrastructure;

// 서비스 등록 (의존성 주입 ,DI)
// Minimal API 용 엔드포인트 탐색기 등록
// Swagger (자동문서) 등록
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQLite DB 파일 경로 설정 및 DbContext 등록
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("db") ?? "Data Source=app.db"));

// Repository 를 Scoped(HTTP 요청당 1개 인스턴스 생성) 라이프사이클로 등록
// 컨트롤러 없이도 Minimal API 핸드러 파라미터로 인터페이스를 받으면 자동 주입
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ICommandRepository, CommandRepository>();

// 애플리케이션 빌드
// Swagger 문서 & Swagger 테스트 UI 미들웨어 등록
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// DB 파일 준비 * 실무에서는 Migration 사용 권장
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 1) 작업생성 (Create Job)
app.MapPost("/api/jobs", async (string palletId, string station, IJobRepository jobs, CancellationToken ct) =>
{
    var job = new Job { Id = Guid.NewGuid(), PalletId = palletId, Station = station };
    await jobs.AddAsync(job, ct);
    await jobs.SaveChangesAsync(ct);
    return Results.Created($"/api/jobs/{job.Id}", new { job.Id, job.PalletId, job.State });
});

// 2) 스캐너 읽기 처리 (Scanner Read) : 스캐너 읽은후 명령 적재
app.MapPost("/api/scanner/{id:int}/read", async (int id, string code, IJobRepository jobs, ICommandRepository cmds, CancellationToken ct) =>
{
    // code(= 바코드/팔레트ID)로 Scheduled 상태의 Job을 조회
    var job = await jobs.FindScheduledByPalletAsync(code, ct);
    if (job is null) return Results.NotFound(new { message = "No scheduled job for pallet" });

    // 상태머신: Scheduled → Dispatched → Running → Succeeded/Failed 흐름의 첫 전이를 담당
    job.Dispatch();
    var cmd = new Command { JobId = job.Id, DeviceCode = "CONV1", Name = "START" };

    await cmds.AddAsync(cmd, ct);
    await cmds.SaveAsync(ct);
    await jobs.SaveChangesAsync(ct);

    return Results.Accepted($"/api/commands/{cmd.Id}", new { CommandId = cmd.Id, JobId = job.Id, Code = code });
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
