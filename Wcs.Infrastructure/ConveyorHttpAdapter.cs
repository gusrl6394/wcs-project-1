using System.Net.Http.Json;
using Wcs.Domain;

namespace Wcs.Infrastructure;

public sealed class ConveyorHttpAdapter(HttpClient http) : IDeviceAdapter // HttpClient는 멀티스레드 안전하게 사용 가능
{
    // 상태 조회: GET plc/conveyor/status 호출 → 응답 JSON을 ConveyorStatus로 역직렬화 → 도메인 DeviceStatus 변환 후 반환.
    public async Task<DeviceStatus> GetStatusAsync(CancellationToken ct)
    {
        var res = await http.GetFromJsonAsync<ConveyorStatus>("plc/conveyor/status", ct);
        return new DeviceStatus(res?.Run == true ? "RUN" : "STOP", res?.Fault == true);
    }

    // 명령 전송: POST plc/conveyor/command 로 { cmd = "START" | "STOP" } 전송 → HTTP 2xx면 CommandResult.Success, 아니면 Fail.
    public async Task<CommandResult> ExecuteAsync(string command, object? args, string requestId, CancellationToken ct)
    {
        var resp = await http.PostAsJsonAsync("plc/conveyor/command", new { cmd = command }, ct);
        return resp.IsSuccessStatusCode ? CommandResult.Success(requestId)
                                        : CommandResult.Fail(requestId, $"HTTP {(int)resp.StatusCode}");
    }

    private sealed record ConveyorStatus(bool Run, bool Fault);
}
