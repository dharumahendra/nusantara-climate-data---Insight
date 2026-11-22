namespace NADI.Core.Models;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public ICollection<ClimateChat> ClimateChats { get; set; } = new List<ClimateChat>();

    public Region? SelectRegion(IEnumerable<Region> availableRegions, int regionId)
    {
        return availableRegions.FirstOrDefault(r => r.RegionId == regionId);
    }

    public ClimateChat AskClimateChat(string question)
    {
        var chat = new ClimateChat
        {
            UserQuestion = question,
            Timestamp = DateTime.UtcNow,
            UserId = UserId
        };
        ClimateChats.Add(chat);
        return chat;
    }

    public IEnumerable<ClimateActionRecommendation> ViewRecommendation(
        IEnumerable<ClimateActionRecommendation> recommendations)
    {
        return recommendations;
    }
}
