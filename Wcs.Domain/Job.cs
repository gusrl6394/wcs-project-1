namespace Wcs.Domain;

public enum JobState { Scheduled, Dispatched, Running, Succeeded, Failed }

public class Job
{
    /*
        * Properties
        Guid Id : 작업 식별자
        PalletId : 팔레트/박스 바코드, 스캐너 이벤트의 Code와 매칭
        Station : 작업이 할당된 스테이션/라인
        JobState State : 작업 상태 (Scheduled, Dispatched, Running, Succeeded, Failed)
         ㄴ Scheduled : 작업이 생성되고 대기 중인 상태
         ㄴ Dispatched : 작업이 스캐너에서 읽혀지고 처리 대기 중인 상태
         ㄴ Running : 작업이 현재 진행 중인 상태
         ㄴ Succeeded : 작업이 성공적으로 완료된 상태
         ㄴ Failed : 작업이 실패한 상태
        DateTime CreatedAt : 작업 생성 시각
        DateTime UpdatedAt : 작업 상태가 마지막으로 변경된 시각
        string? CallbackUrl : 작업 상태 변경 시 알림을 받을 콜백 URL (선택 사항)
    */

    public Guid Id { get; init; }
    public string PalletId { get; init; } = "";
    public string Station  { get; init; } = "";
    public JobState State  { get; private set; } = JobState.Scheduled;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public string? CallbackUrl { get; init; }

    /*
        * Methods
        void Dispatch() : 작업을 Scheduled -> Dispatched 상태로 전환
         ㄴ 스캐너가 PalletId를 읽었을 때 호출
        void Start()    : 작업을 Dispatched -> Running 상태로 전환
         ㄴ 장비 명령이 실제 전송/수행되기 시작할 때
        void Succeed()  : 작업을 Running -> Succeeded 상태로 전환
         ㄴ 정상 완료 시
        void Fail()     : 작업을 Failed 상태로 전환
         ㄴ 오류/타임아웃 등 실패 시
    */
    public void Dispatch() { if (State != JobState.Scheduled) throw new InvalidOperationException(); State = JobState.Dispatched; Touch(); }
    public void Start()    { if (State != JobState.Dispatched) throw new InvalidOperationException(); State = JobState.Running;    Touch(); }
    public void Succeed()  { if (State != JobState.Running)    throw new InvalidOperationException(); State = JobState.Succeeded;  Touch(); }
    public void Fail()     { if (State == JobState.Succeeded)  throw new InvalidOperationException(); State = JobState.Failed;     Touch(); }

    void Touch() => UpdatedAt = DateTime.UtcNow;
}
