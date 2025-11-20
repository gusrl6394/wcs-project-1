using System;

namespace Wcs.Monitor.Models
{
    public class TemperatureReadingDto
    {
        public Guid Id { get; set; }
        public string SensorId { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
