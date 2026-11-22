namespace NADI.Core.Models;

public class ClimateActionRecommendation
{
    public int RecommendationId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int RegionId { get; set; }
    public Region? Region { get; set; }

    public static ClimateActionRecommendation GenerateRecommendation(
        string category, string title, string description, int regionId)
    {
        return new ClimateActionRecommendation
        {
            Category = category,
            Title = title,
            Description = description,
            RegionId = regionId
        };
    }
}
