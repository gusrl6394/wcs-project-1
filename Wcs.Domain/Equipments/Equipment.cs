using System;

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
