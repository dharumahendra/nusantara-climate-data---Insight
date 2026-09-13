using NADI.Core.Models;

namespace NADI.Core.Interfaces;

public interface IClimateService
{
    Task<IEnumerable<ClimateData>> GetRegionalClimate(int regionId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ClimateTrend> CalculateTrend(int regionId, string indicator);
    Task<RegionComparison> CompareRegion(int regionAId, int regionBId, string indicator);
}
