using Wcs.Domain;

namespace Wcs.Infrastructure;

/*
    목적: WCS가 특정 장비(실제 PLC, OPC UA, Modbus, 벤더 SDK, 테스트용 시뮬레이터 등)와 구체 구현에 독립적으로 통신할 수 있게 하는 추상화
    메서드
     ㄴ GetStatusAsync(ct): 장비 상태(예: RUN/STOP, Fault 등)를 읽어 도메인 모델인 DeviceStatus로 반환
     ㄴ ExecuteAsync(command, args, requestId, ct): "START", "STOP" 같은 명령을 전송하고 결과를 도메인 모델 CommandResult로 반환
         ㄴ requestId: 상위(WCS)에서 발급한 요청 추적용 식별자(멱등·로그 연계·리트라이 추적에 유용)
         ㄴ args: 명령 인자(속도, 방향, 타임아웃 등) 확장을 위한 자리
*/
public interface IDeviceAdapter
{
    Task<DeviceStatus> GetStatusAsync(CancellationToken ct);
    Task<CommandResult> ExecuteAsync(string command, object? args, string requestId, CancellationToken ct);
}

public record DeviceStatus(string Mode, bool Fault);
public record CommandResult(bool Ok, string RequestId, string? Message = null)
{
    public static CommandResult Success(string id) => new(true, id);
    public static CommandResult Fail(string id, string? msg = null) => new(false, id, msg);
}
