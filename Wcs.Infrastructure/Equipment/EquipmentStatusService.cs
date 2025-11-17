using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wcs.Domain.Field;
using Wcs.Domain.Equipment;
using EquipmentEntity = Wcs.Domain.Equipment.Equipment;


/*
 - IEquipmentStatusService 구현체
 - IFieldTagRepository로 태그 메타데이터를 읽고
   DeviceId + Direction(Input) + EquipmentId + PropertyName 에 맞는 태그만 필터
 - IEquipmentRepository로 설비 엔티티를 가져와서
   PropertyName에 맞는 프로퍼티에 태그 값 반영 (리플렉션 사용)
 - 값이 바뀌면 Save
*/
namespace Wcs.Infrastructure.Services
{
    /// <summary>
    /// FieldTag + 태그 값들을 도메인 설비 상태에 반영하는 기본 구현.
    /// </summary>
    public class EquipmentStatusService : IEquipmentStatusService
    {
        private readonly ILogger<EquipmentStatusService> _logger;
        private readonly IFieldTagRepository _fieldTagRepository;
        // private readonly IEquipmentRepository _equipmentRepository;
        /*
        public EquipmentStatusService(
            ILogger<EquipmentStatusService> logger,
            IFieldTagRepository fieldTagRepository,
            IEquipmentRepository equipmentRepository)
        {
            _logger = logger;
            _fieldTagRepository = fieldTagRepository;
            _equipmentRepository = equipmentRepository;
        }
        */
        public EquipmentStatusService(
            ILogger<EquipmentStatusService> logger,
            IFieldTagRepository fieldTagRepository)
        {
            _logger = logger;
            _fieldTagRepository = fieldTagRepository;
        }

        public Task UpdateFromFieldAsync(
            string deviceId,
            IReadOnlyDictionary<string, object?> tagValues,
            CancellationToken ct = default)
        {
            // if (tagValues == null || tagValues.Count == 0)
            // {
            //     _logger.LogDebug(
            //         "UpdateFromFieldAsync 호출됐지만 tagValues가 비어 있습니다. deviceId={DeviceId}", deviceId);
            //     return;
            // }

            foreach (var kv in tagValues)
            {
                var tagId = kv.Key;
                var value = kv.Value;

                _logger.LogInformation(
                    "[EQUIP][{DeviceId}] Tag {TagId} = {Value}",
                    deviceId,
                    tagId,
                    value);
            }

            return Task.CompletedTask;

            /*
            // 1) 해당 deviceId + Input + tagValues에 포함된 태그만 필터
            var allTags = await _fieldTagRepository.GetAllAsync(ct);

            var tags = allTags
                .Where(t =>
                    t.DeviceId == deviceId &&
                    t.Direction == IoDirection.Input &&
                    !string.IsNullOrWhiteSpace(t.EquipmentId) &&
                    !string.IsNullOrWhiteSpace(t.PropertyName) &&
                    tagValues.ContainsKey(t.Id))
                .ToList();

            if (tags.Count == 0)
            {
                _logger.LogDebug(
                    "deviceId={DeviceId} 에 대해 매핑 가능한 Input 태그가 없습니다. (tagValues count={Count})",
                    deviceId, tagValues.Count);
                return;
            }

            // 2) EquipmentId 별로 그룹
            var groupsByEquipment = tags
                .GroupBy(t => t.EquipmentId!) // 위에서 null 아닌 것만 필터 했음
                .ToList();

            foreach (var eqGroup in groupsByEquipment)
            {
                var equipmentId = eqGroup.Key;
                var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, ct);

                if (equipment == null)
                {
                    _logger.LogWarning(
                        "EquipmentId={EquipmentId} 를 찾을 수 없습니다. deviceId={DeviceId}",
                        equipmentId, deviceId);
                    continue;
                }

                bool changed = false;

                foreach (var tag in eqGroup)
                {
                    var rawValue = tagValues[tag.Id];
                    var propertyName = tag.PropertyName!;

                    if (!TryApplyProperty(equipment, propertyName, rawValue, out bool propertyChanged))
                    {
                        _logger.LogDebug(
                            "EquipmentId={EquipmentId}, TagId={TagId}, PropertyName={PropertyName} 매핑 실패 (타입 불일치 또는 프로퍼티 없음)",
                            equipmentId, tag.Id, propertyName);
                        continue;
                    }

                    if (propertyChanged)
                    {
                        changed = true;
                        _logger.LogDebug(
                            "EquipmentId={EquipmentId}, Property={PropertyName} 태그 {TagId} 로부터 업데이트. 값={Value}",
                            equipmentId, propertyName, tag.Id, rawValue);
                    }
                }

                if (changed)
                {
                    await _equipmentRepository.SaveAsync(equipment, ct);
                }
            }
            */
        }

        private bool TryApplyProperty(
            EquipmentEntity  equipment,
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
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(bool))
            {
                if (rawValue is bool b) return b;
                if (rawValue is ushort us) return us != 0;
                if (rawValue is short s) return s != 0;
                if (rawValue is int i) return i != 0;
                if (rawValue is string str)
                    return str == "1" || bool.Parse(str);
            }

            if (underlyingType.IsEnum)
            {
                if (rawValue is string es)
                    return Enum.Parse(underlyingType, es, ignoreCase: true);

                return Enum.ToObject(underlyingType, rawValue);
            }

            return Convert.ChangeType(rawValue, underlyingType);
        }
    }
}
