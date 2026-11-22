using NADI.Core.Models;

namespace NADI.Core.Interfaces;

public interface IAiExplainerService
{
    Task<string> GenerateExplanation(IEnumerable<ClimateData> climateData, Region region);
}
