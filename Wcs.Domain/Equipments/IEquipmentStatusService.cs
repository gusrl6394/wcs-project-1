using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wcs.Domain.Equipment
{
    /// <summary>
    /// 현장 IO(태그) 값들을 도메인 설비 상태(Equipment)에 반영하는 서비스.
    /// </summary>
    public interface IEquipmentStatusService
    {
        /// <summary>
        /// 특정 필드 디바이스(deviceId)에서 읽어온 태그 값들을
        /// 설비 상태에 반영한다.
        /// </summary>
        /// <param name="deviceId">
        /// FieldTag.DeviceId 에 대응되는 값 (예: "PLC01", "CV01_PLC")
        /// </param>
        /// <param name="tagValues">
        /// TagId → 값 (bool, ushort, int, string 등) 매핑
        /// </param>
        Task UpdateFromFieldAsync(
            string deviceId,
            IReadOnlyDictionary<string, object?> tagValues,
            CancellationToken ct = default);
    }
}
