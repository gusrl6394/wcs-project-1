using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wcs.Domain.Field;
using Wcs.Domain.Equipment;


/*
 - IEquipmentStatusService 구현체
 - IFieldTagRepository로 태그 메타데이터를 읽고
   DeviceId + Direction(Input) + EquipmentId + PropertyName 에 맞는 태그만 필터
 - IEquipmentRepository로 설비 엔티티를 가져와서
   PropertyName에 맞는 프로퍼티에 태그 값 반영 (리플렉션 사용)
 - 값이 바뀌면 Save
*/
namespace Wcs.Infrastructure.Equipment
{
    /// <summary>
    /// FieldTag + 태그 값들을 도메인 설비 상태에 반영하는 기본 구현.
    /// </summary>
    public class EquipmentStatusService : IEquipmentStatusService
    {
        private readonly ILogger<EquipmentStatusService> _logger;
        private readonly IFieldTagRepository _fieldTags;
        private readonly IEquipmentRepository _equipments ;

        public EquipmentStatusService(
            ILogger<EquipmentStatusService> logger,
            IFieldTagRepository fieldTagRepository,
            IEquipmentRepository equipmentRepository)
        {
            _logger = logger;
            _fieldTags = fieldTagRepository;
            _equipments  = equipmentRepository;
        }

        public async Task UpdateFromFieldAsync(
            string deviceId,
            IReadOnlyDictionary<string, object?> boolTags,
            CancellationToken ct = default)
        {
            if (boolTags.Count == 0)
                return;

            // 1) 태그 메타데이터 읽기
            var allTags = await _fieldTags.GetAllAsync(ct);
            var tagById = allTags.ToDictionary(t => t.Id);

            // 2) 설비별 캐시
            var equipmentCache = new Dictionary<string, EquipmentEntity>();

            foreach (var kv in boolTags)
            {
                var tagId = kv.Key;
                var value = kv.Value;

                if (!tagById.TryGetValue(tagId, out var tag))
                {
                    _logger.LogWarning("FieldTag 메타데이터를 찾을 수 없습니다. TagId={TagId}", tagId);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(tag.EquipmentId) ||
                    string.IsNullOrWhiteSpace(tag.PropertyName))
                    continue; // 설비와 연결되지 않은 태그는 스킵

                var equipmentId = tag.EquipmentId!;

                if (!equipmentCache.TryGetValue(equipmentId, out var equipment))
                {
                    equipment = await _equipments.GetByIdAsync(equipmentId, ct)
                        ?? new EquipmentEntity(
                            id: equipmentId,
                            name: equipmentId,
                            deviceId: deviceId
                        );

                    equipmentCache[equipmentId] = equipment;
                }
            }

            // 4) 변경된 설비들 저장
            foreach (var equipment in equipmentCache.Values)
            {
                await _equipments.SaveAsync(equipment, ct);

                _logger.LogInformation(
                    "[EQUIP][{DeviceId}] {EquipmentId} 상태 업데이트: Run={IsRunning}, Fault={HasFault}, Blocked={IsBlocked}",
                    deviceId,
                    equipment.Id,
                    equipment.IsRunning,
                    equipment.HasFault,
                    equipment.IsBlocked);
            }
        }

        private bool TryApplyProperty(
            EquipmentEntity equipment,
            string propertyName,
            object? rawValue,
            out bool changed)
        {
            changed = false;

            var type = equipment.GetType();
            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop == null || !prop.CanWrite)
                return false;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var currentValue = prop.GetValue(equipment);

            if (rawValue == null)
                return false;

            object? convertedValue;

            try
            {
                convertedValue = ConvertToPropertyType(rawValue, targetType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "태그 값을 Equipment 프로퍼티 타입으로 변환 실패. Property={PropertyName}, TargetType={TargetType}, RawType={RawType}, RawValue={RawValue}",
                    propertyName, targetType.Name, rawValue.GetType().Name, rawValue);
                return false;
            }

            if (Equals(currentValue, convertedValue))
                return true;

            prop.SetValue(equipment, convertedValue);
            changed = true;
            return true;
        }

        private object? ConvertToPropertyType(object rawValue, Type targetType)
        {
            if (targetType == typeof(bool))
                return Convert.ToBoolean(rawValue);

            if (targetType == typeof(int))
                return Convert.ToInt32(rawValue);

            if (targetType == typeof(double))
                return Convert.ToDouble(rawValue);

            if (targetType == typeof(ushort))
                return Convert.ToUInt16(rawValue);

            return Convert.ChangeType(rawValue, targetType);
        }
    }
}
