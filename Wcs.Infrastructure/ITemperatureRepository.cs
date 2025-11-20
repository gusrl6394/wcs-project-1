using Wcs.Domain;

namespace Wcs.Infrastructure;

public interface ITemperatureRepository
{
    Task AddAsync(TemperatureReading reading, CancellationToken ct);
    Task<List<TemperatureReading>> GetRecentAsync(int count, CancellationToken ct);
}
