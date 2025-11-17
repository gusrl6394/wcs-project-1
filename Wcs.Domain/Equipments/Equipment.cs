using System;

/*
 - 실제 WCS 도메인이 보는 “설비 상태”를 표현
 - 태그 값이 이 엔티티의 프로퍼티로 매핑됨
   (예: RUN_FB → IsRunning, FAULT → HasFault 등)
*/
namespace Wcs.Domain.Equipment
{
    /// <summary>
    /// 도메인 설비(컨베이어, 리프트 등)의 상태를 표현하는 엔티티 예시.
    /// </summary>
    public class Equipment
    {
        public string Id { get; private set; } = default!;
        public string Name { get; private set; } = default!;

        // 상태 값 예시들
        public bool IsRunning { get; private set; }
        public bool HasFault { get; private set; }
        public bool IsBlocked { get; private set; }

        public DateTime LastStatusChangedAt { get; private set; }

        // 생성자/팩토리 메서드는 필요에 따라…
        public Equipment(string id, string name)
        {
            Id = id;
            Name = name;
            LastStatusChangedAt = DateTime.UtcNow;
        }

        public void SetIsRunning(bool value)
        {
            if (IsRunning != value)
            {
                IsRunning = value;
                LastStatusChangedAt = DateTime.UtcNow;
            }
        }

        public void SetHasFault(bool value)
        {
            if (HasFault != value)
            {
                HasFault = value;
                LastStatusChangedAt = DateTime.UtcNow;
            }
        }

        public void SetIsBlocked(bool value)
        {
            if (IsBlocked != value)
            {
                IsBlocked = value;
                LastStatusChangedAt = DateTime.UtcNow;
            }
        }
    }
}
