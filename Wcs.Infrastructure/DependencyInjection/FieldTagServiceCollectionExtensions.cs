using Microsoft.Extensions.DependencyInjection;
using Wcs.Domain.Field;
using Wcs.Infrastructure.Field;

namespace Wcs.Infrastructure.DependencyInjection
{
    public static class FieldTagServiceCollectionExtensions
    {
        /// <summary>
        /// FieldTag 관련 서비스 등록.
        /// - IFieldTagRepository -> EfFieldTagRepository
        /// </summary>
        public static IServiceCollection AddFieldTags(this IServiceCollection services)
        {
            services.AddScoped<IFieldTagRepository, EfFieldTagRepository>();
            return services;
        }
    }
}
