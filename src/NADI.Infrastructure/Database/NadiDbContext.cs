using Microsoft.EntityFrameworkCore;

namespace NADI.Infrastructure.Database;

// Skema database (PRD §9): Regions, ClimateDailyRecords, ClimateYearlySummaries,
// ChatMessages, AiResponseCache, Recommendations.
public class NadiDbContext : DbContext
{
    public NadiDbContext(DbContextOptions<NadiDbContext> options) : base(options)
    {
    }
}
