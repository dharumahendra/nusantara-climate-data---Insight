using NADI.Core.Models;

namespace NADI.Core.Interfaces;

public interface IAiService
{
    Task<string> ExplainClimateData(IEnumerable<ClimateData> climateData, Region region);
    Task<string> AnswerQuestion(string question, IEnumerable<ClimateData>? context = null);
    Task<IEnumerable<ClimateActionRecommendation>> GenerateRecommendation(
        IEnumerable<ClimateData> climateData, Region region);
}
