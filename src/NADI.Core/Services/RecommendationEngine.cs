using NADI.Core.Interfaces;
using NADI.Core.Models;

namespace NADI.Core.Services;

public class RecommendationEngine : IRecommendationEngine
{
    public Task<IEnumerable<ClimateActionRecommendation>> GenerateRecommendation(
        IEnumerable<ClimateData> climateData, IEnumerable<ClimateTrend> trends, Region region)
    {
        var recommendations = new List<ClimateActionRecommendation>();
        return Task.FromResult<IEnumerable<ClimateActionRecommendation>>(recommendations);
    }
}
