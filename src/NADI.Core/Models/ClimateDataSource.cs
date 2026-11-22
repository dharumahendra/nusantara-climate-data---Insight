namespace NADI.Core.Models;

public class ClimateDataSource
{
    public int SourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }

    public ICollection<ClimateData> ClimateDataRecords { get; set; } = new List<ClimateData>();

    public void FetchData()
    {
    }

    public void SyncData()
    {
    }
}
