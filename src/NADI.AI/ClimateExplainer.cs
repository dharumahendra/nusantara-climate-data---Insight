using NADI.Core.Interfaces;
using NADI.Core.Models;

namespace NADI.AI;

public class ClimateExplainer : IAiExplainerService
{
    private readonly IAiService _aiService;

    public string Explanation { get; private set; } = string.Empty;

    public ClimateExplainer(IAiService aiService)
    {
        _aiService = aiService;
    }

    public async Task<string> GenerateExplanation(IEnumerable<ClimateData> climateData, Region region)
    {
        Explanation = await _aiService.ExplainClimateData(climateData, region);
        return Explanation;
    }
}
