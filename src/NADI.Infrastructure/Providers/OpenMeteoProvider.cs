using NADI.Core.Interfaces;
using NADI.Core.Models;

namespace NADI.Infrastructure.Providers;

public class OpenMeteoProvider : IClimateDataSource
{
    private readonly HttpClient _httpClient;
    private readonly IClimateRepository _repository;

    public int SourceId { get; set; }
    public string Name { get; set; } = "Open-Meteo";
    public string ApiUrl { get; set; } = "https://archive-api.open-meteo.com/v1/archive";
    public DateTime LastUpdated { get; set; }

    public OpenMeteoProvider(HttpClient httpClient, IClimateRepository repository)
    {
        _httpClient = httpClient;
        _repository = repository;
    }

    public async Task<IEnumerable<ClimateData>> FetchData(Region region, DateTime startDate, DateTime endDate)
    {
        var results = new List<ClimateData>();
        LastUpdated = DateTime.UtcNow;
        return await Task.FromResult(results);
    }

    public async Task SyncData(Region region)
    {
        var existingData = await _repository.GetByRegion(region.RegionId);
        var lastDate = existingData.Any()
            ? existingData.Max(d => d.Date)
            : new DateTime(1994, 1, 1); 

        var newData = await FetchData(region, lastDate.AddDays(1), DateTime.UtcNow.Date);

        foreach (var data in newData)
        {
            await _repository.SaveClimateData(data);
        }

        LastUpdated = DateTime.UtcNow;
    }
}
