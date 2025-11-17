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
namespace Wcs.Infrastructure.Services
{
    /// <summary>
    /// FieldTag + 태그 값들을 도메인 설비 상태에 반영하는 기본 구현.
    /// </summary>
    public class EquipmentStatusService : IEquipmentStatusService
    {
        private readonly ILogger<EquipmentStatusService> _logger;
        private readonly IFieldTagRepository _fieldTagRepository;
        private readonly IEquipmentRepository _equipmentRepository;

        public EquipmentStatusService(
            ILogger<EquipmentStatusService> logger,
            IFieldTagRepository fieldTagRepository,
            IEquipmentRepository equipmentRepository)
        {
            _logger = logger;
            _fieldTagRepository = fieldTagRepository;
            _equipmentRepository = equipmentRepository;
        }

        public async Task UpdateFromFieldAsync(
            string deviceId,
            IReadOnlyDictionary<string, object?> _fieldTags,
            CancellationToken ct = default)
        {
            if (_fieldTags == null || _fieldTags.Count == 0)
            {
                _logger.LogDebug(
                    "UpdateFromFieldAsync 호출됐지만 tagValues가 비어 있습니다. deviceId={DeviceId}", deviceId);
                return;
            }

            // 1) 모든 태그 메타데이터 조회 후, Id 기준으로 Dictionary화
            var allTags = await _fieldTags.GetAllAsync(ct);
            var tagById = allTags.ToDictionary(t => t.Id);

            // 2) 이번에 들어온 태그들 중, EquipmentId + PropertyName 이 있는 것만 필터
            //    설비별로 묶기 위해 equipmentId -> Equipment 캐시 사용
            var equipmentCache = new Dictionary<string, Equipment>();
        }

        private bool TryApplyProperty(
            Equipment equipment,
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
