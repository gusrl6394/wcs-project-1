using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wcs.Domain.Field;              // IFieldBusChannel
using Wcs.Infrastructure.Field.Modbus;

namespace Wcs.Infrastructure.DependencyInjection
{
    public static class FieldBusServiceCollectionExtensions
    {
        public static IServiceCollection AddModbusTcp(this IServiceCollection services, IConfiguration config)
        {
            // appsettings.json 의 "ModbusTcp" 섹션 가져오기
            var section = config.GetSection("ModbusTcp");

            // 1) Options 등록 + Bind (Configure<T>(IConfiguration) 대신 이 방식 사용)
            services.AddOptions<ModbusTcpOptions>()
                    .Bind(section);

            // 2) IFieldBusChannel 구현으로 ModbusTcpChannel 등록
            services.AddSingleton<IFieldBusChannel>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ModbusTcpOptions>>().Value;
                return new ModbusTcpChannel(options.IpAddress, options.Port);
            });

            return services;
        }
    }

    // ModbusTcp 설정 값 (IP / Port)
    public class ModbusTcpOptions
    {
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 502;
    }
}
