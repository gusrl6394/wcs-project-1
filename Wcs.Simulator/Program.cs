var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

/*
    상태 저장용 간단한 인메모리 객체
    메모리 상에 런/정지 상태와 고장(Fault) 여부를 들고 있습니다.
*/
var state = new ConveyorState();

app.MapGet("/", () => "Wcs.Simulator running");
// 컨베이어 상태 조회 및 명령 수신 엔드포인트
// 현재 상태를 JSON으로 응답, 현재는 고정된 응답(테스트 용도)
app.MapGet("/plc/conveyor/status", () => new { Run = state.Run, Fault = state.Fault });
app.MapPost("/plc/conveyor/command", (CommandReq req) =>
{
    if ((req.cmd ?? "").ToUpperInvariant() == "START") state.Run = true;
    else if ((req.cmd ?? "").ToUpperInvariant() == "STOP") state.Run = false;
    return Results.Ok(new { ok = true });
});

app.Run("http://localhost:5088");

record CommandReq(string cmd);
class ConveyorState {
     public bool Run { get; set; } = false; 
     public bool Fault { get; set; } = false; 
}
