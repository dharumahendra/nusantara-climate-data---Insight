using Microsoft.EntityFrameworkCore;
using NADI.Core.Interfaces;
using NADI.Core.Models;
using NADI.Infrastructure.Database;

namespace NADI.Infrastructure.Repositories;

public class ClimateRepository : IClimateRepository
{
    private readonly NadiDbContext _context;

    public ClimateRepository(NadiDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClimateData>> GetClimateData()
    {
        return await _context.ClimateDataRecords.ToListAsync();
    }

    public async Task SaveClimateData(ClimateData data)
    {
        _context.ClimateDataRecords.Add(data);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ClimateData>> GetByRegion(int regionId)
    {
        return await _context.ClimateDataRecords
            .Where(d => d.RegionId == regionId)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<ClimateData>> GetByPeriod(int regionId, DateTime startDate, DateTime endDate)
    {
        return await _context.ClimateDataRecords
            .Where(d => d.RegionId == regionId && d.Date >= startDate && d.Date <= endDate)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }
}
