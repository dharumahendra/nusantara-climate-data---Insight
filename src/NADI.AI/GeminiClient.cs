using NADI.Core.Interfaces;
using NADI.Core.Models;

namespace NADI.AI;

public class GeminiClient : IAiService
{
    private readonly HttpClient _httpClient;

    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-pro";

    public GeminiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ExplainClimateData(IEnumerable<ClimateData> climateData, Region region)
    {
        return await Task.FromResult(string.Empty);
    }

    public async Task<string> AnswerQuestion(string question, IEnumerable<ClimateData>? context = null)
    {
        return await Task.FromResult(string.Empty);
    }

    public async Task<IEnumerable<ClimateActionRecommendation>> GenerateRecommendation(
        IEnumerable<ClimateData> climateData, Region region)
    {
        return await Task.FromResult<IEnumerable<ClimateActionRecommendation>>(
            new List<ClimateActionRecommendation>());
    }
}
