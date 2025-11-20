using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // For IServiceScopeFactory
using Microsoft.Extensions.Options; // For IOptions
using Wcs.Domain;
using Wcs.Infrastructure; // For ITemperatureRepository
using Wcs.Infrastructure.DependencyInjection; // For TemperatureSensorOptions

namespace Wcs.Workers.Workers
{
    public class TemperaturePollingWorker : BackgroundService
    {
        private readonly ILogger<TemperaturePollingWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TemperatureSensorOptions _options; // Injected options

        public TemperaturePollingWorker(ILogger<TemperaturePollingWorker> logger,
                                        IHttpClientFactory httpClientFactory,
                                        IServiceScopeFactory scopeFactory,
                                        IOptions<TemperatureSensorOptions> options)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
            _options = options.Value; // Get the configured options
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Temperature Polling Worker running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var temperatureRepository = scope.ServiceProvider.GetRequiredService<ITemperatureRepository>();
                    // CreateClient with the named client configured in Program.cs
                    var httpClient = _httpClientFactory.CreateClient("TemperatureSensorClient");

                    try
                    {
                        var response = await httpClient.GetAsync($"{_options.BaseUrl}/temperature", stoppingToken);
                        response.EnsureSuccessStatusCode();

                        var content = await response.Content.ReadAsStringAsync(stoppingToken);
                        var reading = JsonSerializer.Deserialize<TemperatureReading>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (reading != null)
                        {
                            await temperatureRepository.AddAsync(reading, stoppingToken);
                            _logger.LogInformation("Recorded temperature: {Temperature} at {Timestamp}", reading.Value, reading.Timestamp);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "Error connecting to temperature sensor at {Url}: {Message}", $"{_options.BaseUrl}/temperature", ex.Message);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error parsing temperature sensor response: {Message}", ex.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An unexpected error occurred in Temperature Polling Worker: {Message}", ex.Message);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("Temperature Polling Worker stopped.");
        }
    }
}
