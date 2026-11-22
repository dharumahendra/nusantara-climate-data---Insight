namespace NADI.Core.Models;

public class ClimateTrend
{
    public int TrendId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public double ChangeValue { get; set; }
    public string TrendType { get; set; } = string.Empty;

    public int RegionId { get; set; }
    public Region? Region { get; set; }

    public void CalculateTrend(IEnumerable<ClimateData> climateDataList)
    {
    }

    public string GetTrendSummary()
    {
        return $"{Indicator}: {(ChangeValue >= 0 ? "+" : "")}{ChangeValue:F2} ({TrendType})";
    }
}
