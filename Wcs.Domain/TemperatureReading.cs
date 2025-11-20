namespace Wcs.Domain;

public class TemperatureReading
{
    public Guid Id { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
