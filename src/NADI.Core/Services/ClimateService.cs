using NADI.Core.Interfaces;
using NADI.Core.Models;

namespace NADI.Core.Services;

public class ClimateService : IClimateService
{
    private readonly IClimateRepository _repository;

    public ClimateService(IClimateRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ClimateData>> GetRegionalClimate(
        int regionId, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return await _repository.GetByPeriod(regionId, startDate.Value, endDate.Value);
        }

        return await _repository.GetByRegion(regionId);
    }

    public async Task<ClimateTrend> CalculateTrend(int regionId, string indicator)
    {
        var data = await _repository.GetByRegion(regionId);

        var trend = new ClimateTrend
        {
            RegionId = regionId,
            Indicator = indicator
        };

        trend.CalculateTrend(data);

        return trend;
    }

    public async Task<RegionComparison> CompareRegion(int regionAId, int regionBId, string indicator)
    {
        var dataA = await _repository.GetByRegion(regionAId);
        var dataB = await _repository.GetByRegion(regionBId);

        var comparison = new RegionComparison
        {
            RegionAId = regionAId,
            RegionBId = regionBId,
            Indicator = indicator,
            ComparisonDate = DateTime.UtcNow
        };

        comparison.Compare(dataA, dataB);

        return comparison;
    }
}
