using NADI.Core.Models;

namespace NADI.Core.Interfaces;

public interface IRecommendationEngine
{
    Task<IEnumerable<ClimateActionRecommendation>> GenerateRecommendation(
        IEnumerable<ClimateData> climateData, IEnumerable<ClimateTrend> trends, Region region);
}
