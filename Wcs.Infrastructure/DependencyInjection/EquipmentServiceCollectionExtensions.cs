using Microsoft.Extensions.DependencyInjection;
using Wcs.Domain.Equipment;
using Wcs.Infrastructure.Services;

/*
 - IEquipmentStatusService를 DI 컨테이너에 등록하는 확장 메서드
 - Program.cs에서 services.AddEquipmentServices(); 한 줄로 사용
*/
namespace Wcs.Infrastructure.DependencyInjection
{
    public static class EquipmentServiceCollectionExtensions
    {
        public static IServiceCollection AddEquipmentServices(this IServiceCollection services)
        {
            // 설비 리포지토리
            services.AddScoped<IEquipmentRepository, EfEquipmentRepository>();

            // 설비 상태 도메인 서비스
            services.AddScoped<IEquipmentStatusService, EquipmentStatusService>();
            return services;
        }
    }
}
