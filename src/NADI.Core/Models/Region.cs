namespace NADI.Core.Models;

public class Region
{
    public int RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public ICollection<ClimateData> ClimateDataRecords { get; set; } = new List<ClimateData>();
    public ICollection<ClimateTrend> ClimateTrends { get; set; } = new List<ClimateTrend>();

    public IEnumerable<ClimateData> GetClimateData()
    {
        return ClimateDataRecords;
    }
}
