using Microsoft.EntityFrameworkCore;
using Wcs.Domain;
using Wcs.Infrastructure.Persistence;

namespace Wcs.Infrastructure;

public class EfTemperatureRepository(WcsDbContext db) : ITemperatureRepository
{
    public async Task AddAsync(TemperatureReading reading, CancellationToken ct)
    {
        await db.TemperatureReadings.AddAsync(reading, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<TemperatureReading>> GetRecentAsync(int count, CancellationToken ct)
    {
        return await db.TemperatureReadings
                       .OrderByDescending(r => r.Timestamp)
                       .Take(count)
                       .ToListAsync(ct);
    }
}
