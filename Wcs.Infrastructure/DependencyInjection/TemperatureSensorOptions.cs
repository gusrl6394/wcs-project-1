namespace Wcs.Infrastructure.DependencyInjection
{
    public class TemperatureSensorOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:5000";
        public int PollingIntervalSeconds { get; set; } = 5;
    }
}
