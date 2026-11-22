namespace NADI.Core.Models;

public class RegionComparison
{
    public int ComparisonId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public DateTime ComparisonDate { get; set; }

    public int RegionAId { get; set; }
    public Region? RegionA { get; set; }

    public int RegionBId { get; set; }
    public Region? RegionB { get; set; }

    public void Compare(IEnumerable<ClimateData> dataA, IEnumerable<ClimateData> dataB)
    {
    }

    public string GenerateResult()
    {
        return $"Perbandingan {Indicator} antara Region {RegionAId} dan Region {RegionBId} " +
               $"pada {ComparisonDate:yyyy-MM-dd}";
    }
}
