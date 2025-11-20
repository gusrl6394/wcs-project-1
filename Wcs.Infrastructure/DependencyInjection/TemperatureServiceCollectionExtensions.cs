using Microsoft.Extensions.DependencyInjection;

namespace Wcs.Infrastructure.DependencyInjection
{
    public static class TemperatureServiceCollectionExtensions
    {
        public static IServiceCollection AddTemperatureServices(this IServiceCollection services)
        {
            services.AddScoped<ITemperatureRepository, EfTemperatureRepository>();
            return services;
        }
    }
}
