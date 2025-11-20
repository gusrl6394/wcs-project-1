using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Wcs.Domain;
using Wcs.Infrastructure;
using Wcs.Infrastructure.DependencyInjection;
using Wcs.Infrastructure.Persistence;
using Wcs.Workers.Workers; 

var host = new HostBuilder()
    .ConfigureAppConfiguration(cfg => { /* default */ })
    .ConfigureLogging((context, logging) =>
    {
        // ★ EF Core SQL 로그를 Warning 이상만 찍도록 강제
        logging.AddFilter(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogLevel.Warning
        );
    })
    // DI 컨테이너에 서비스 등록
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<WcsDbContext>(opt =>
            opt.UseSqlite(context.Configuration.GetConnectionString("db")));

        // 리포지토리: ICommandRepository를 워커에서 사용(대기 중 명령 조회/저장).
        services.AddScoped<ICommandRepository, CommandRepository>();

        // HttpClient + 어댑터:
        //  ㄴ IDeviceAdapter 구현으로 ConveyorHttpAdapter를 등록.
        //  ㄴ HttpClient 기본 주소: 설정파일의 Simulator:BaseAddress (기본값: http://localhost:5088/).
        //  ㄴ Polly 재시도 정책: 일시적 실패 시 최대 3회, 지수 백오프(200ms, 400ms, 600ms).
        services.AddHttpClient<IDeviceAdapter, ConveyorHttpAdapter>(client =>
        {
            var baseAddress = context.Configuration["Simulator:BaseAddress"] ?? "http://localhost:5088/";
            client.BaseAddress = new Uri(baseAddress);
        })
        .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(200 * i)));

        services.AddHostedService<CommandProcessor>();

        services.AddModbusTcp(context.Configuration);  // Modbus TCP 서비스 등록
        services.AddEquipmentServices();
        services.AddHostedService<ModbusPollingWorker>(); // 코일 읽기 Worker
        services.AddHostedService<ModbusWriteTestWorker>(); // 코일 쓰기 Worker
        services.AddFieldTags(); // FieldTag 관련 서비스 등록
        services.AddTemperatureServices(); // TemperatureRepository 관련 서비스 등록
        services.AddHostedService<TemperaturePollingWorker>(); // 온도 센서 폴링 워커

        // Temperature Sensor 통신을 위한 HttpClient 등록
        services.AddHttpClient("TemperatureSensorClient", client =>
        {
            client.BaseAddress = new Uri(context.Configuration["TemperatureSensor:BaseUrl"] ?? "http://localhost:5000");
        });

        // Temperature Sensor 옵션 바인딩
        services.AddOptions<TemperatureSensorOptions>()
                .Bind(context.Configuration.GetSection("TemperatureSensor"));
    })
    .ConfigureLogging(lb => lb.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss ")) // 콘솔 로그 포맷
    .UseConsoleLifetime() // Ctrl+C 종료 처리
    .Build();

await host.RunAsync();

/*
동작 흐름
 1) 스코프 생성: DbContext/리포지토리 수명 관리를 위해 루프마다 DI 스코프를 열어요.
 2) 대기 명령 조회: cmds.QueryPending().Take(10) → FIFO에 가깝게 최대 10건(각 구현에서 OrderBy(CreatedAt)).
 3) 상태 전이 1: Sent
     ㄴ 장비로 명령 보내기 직전에 Sent로 바꾸고 저장(장비 호출 중 장애가 나도 “보냈다 시도했다” 흔적 남김).
 4) 장비 호출: device.ExecuteAsync
     ㄴ 여기서 HTTP(시뮬레이터) 호출. Polly에 의해 일시 오류는 재시도.
 5) 성공
     ㄴ Acked 설정, 완료 시간 기록 → 저장
     ㄴ JobId가 연결된 경우, 현재 Job이 Dispatched면 Start()→Succeed()로 완료 처리 후 저장
 6) 실패
     ㄴ Failed 설정 + 실패 메시지 기록 → 저장
 7) 예외
     ㄴ 모든 예외를 캐치해서 Failed 처리 + 로그 남기고 계속
 8) 폴링 간격: 300ms 딜레이

왜 이렇게 나눴나?
 1) 내결함성: 장비 호출 도중 장애가 나도 커맨드 상태가 남아 원인 추적 가능.
 2) 비동기/백그라운드: API 요청 처리와 분리해서 장비 제어를 독립된 워커가 담당.
 3) 상태머신의 응집: Job/Command의 전이가 한 곳(워커)에서 일관되게 이뤄짐.
*/
public sealed class CommandProcessor(IServiceProvider sp, IDeviceAdapter device, ILogger<CommandProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = sp.CreateScope();
            var cmds = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
            var db = scope.ServiceProvider.GetRequiredService<WcsDbContext>();

            var pending = cmds.QueryPending().Take(10).ToList();
            foreach (var c in pending)
            {
                try
                {
                    c.State = CommandState.Sent;
                    await cmds.SaveAsync(stoppingToken);

                    var res = await device.ExecuteAsync(c.Name, c.Args, c.RequestId, stoppingToken);
                    if (res.Ok)
                    {
                        c.State = CommandState.Acked;
                        c.CompletedAt = DateTime.UtcNow;
                        await cmds.SaveAsync(stoppingToken);

                        if (c.JobId is Guid jid)
                        {
                            var job = await db.Jobs.FirstAsync(j => j.Id == jid, stoppingToken);
                            if (job.State == JobState.Dispatched) { job.Start(); job.Succeed(); }
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                    else { c.State = CommandState.Failed; c.Note = res.Message; await cmds.SaveAsync(stoppingToken); }
                }
                catch (Exception ex)
                {
                    c.State = CommandState.Failed; c.Note = ex.Message;
                    await cmds.SaveAsync(stoppingToken);
                    logger.LogError(ex, "Command processing failed for {CommandId}", c.Id);
                }
            }
            await Task.Delay(300, stoppingToken);
        }
    }
}
