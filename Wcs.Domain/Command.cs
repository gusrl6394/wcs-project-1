namespace Wcs.Domain;

public enum CommandState { Pending, Sent, Acked, Failed }

public class Command
{
    /*
        Guid Id : 명령 식별자
        Guid? JobId : 연관된 작업 식별자 (없을 수도 있음)
        string DeviceCode : 명령이 전송될 장비/컨트롤러 코드 (예: "CONV1")
        string Name : 명령 이름 (예: "START", "STOP")
        string? Args : 명령에 대한 추가 인자 (선택 사항)
        string RequestId : 명령 요청 식별자 (중복 방지용)
        CommandState State : 명령 상태 (Pending -> Sent -> Acked / Failed)
         ㄴ Pending : 명령이 생성되고 전송 대기 중인 상태
         ㄴ Sent : 명령이 장비/컨트롤러에 전송된 상태
         ㄴ Acked : 장비/컨트롤러가 명령을 수신하고 확인 응답을 보낸 상태
         ㄴ Failed : 명령 전송 또는 처리에 실패한 상태
        DateTime CreatedAt : 명령 생성 시각
        DateTime? CompletedAt : 명령이 완료된 시각 (성공 또는 실패 시)
        string? Note : 명령 처리에 대한 추가 정보 또는 오류 메시지 (선택 사항) 
    */
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? JobId { get; set; }
    public string DeviceCode { get; set; } = "CONV1";
    public string Name { get; set; } = "START"; // or "STOP"
    public string? Args { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public CommandState State { get; set; } = CommandState.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }
}
