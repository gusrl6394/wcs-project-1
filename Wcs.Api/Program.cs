using Microsoft.EntityFrameworkCore;
using Wcs.Domain;
using Wcs.Infrastructure;
using Wcs.Infrastructure.Persistence;

// 서비스 등록 (의존성 주입 ,DI)
// Minimal API 용 엔드포인트 탐색기 등록
// Swagger (자동문서) 등록
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQLite DB 파일 경로 설정 및 DbContext 등록
builder.Services.AddDbContext<WcsDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("db") ?? "Data Source=app.db"));

// Repository 를 Scoped(HTTP 요청당 1개 인스턴스 생성) 라이프사이클로 등록
// 컨트롤러 없이도 Minimal API 핸드러 파라미터로 인터페이스를 받으면 자동 주입
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<ICommandRepository, CommandRepository>();
builder.Services.AddScoped<ITemperatureRepository, EfTemperatureRepository>();

// 애플리케이션 빌드
var app = builder.Build();

// Swagger 문서 & Swagger 테스트 UI 미들웨어 등록
app.UseSwagger();
app.UseSwaggerUI();

// DB 파일 준비 * 실무에서는 Migration 사용 권장
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WcsDbContext>();
    db.Database.EnsureCreated();
}

// Minimal API 엔드포인트 정의
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
    var job = await jobs.FindScheduledByPalletAsync(code, ct);
    if (job is null) return Results.NotFound(new { message = "No scheduled job for pallet" });

    job.Dispatch();
    var cmd = new Command { JobId = job.Id, DeviceCode = "CONV1", Name = "START" };

    await cmds.AddAsync(cmd, ct);
    await cmds.SaveAsync(ct);
    await jobs.SaveChangesAsync(ct);

    return Results.Accepted($"/api/commands/{cmd.Id}", new { CommandId = cmd.Id, JobId = job.Id, Code = code });
});

// 3) 온도 데이터 조회 (Get Temperature Readings)
app.MapGet("/api/temperature", async (ITemperatureRepository repo, int count = 10, CancellationToken ct = default) =>
{
    if (count <= 0) return Results.BadRequest("Count must be greater than 0.");
    var readings = await repo.GetRecentAsync(count, ct);
    return Results.Ok(readings);
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
