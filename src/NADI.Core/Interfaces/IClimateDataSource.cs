using NADI.Core.Models;

namespace NADI.Core.Interfaces;

public interface IClimateDataSource
{
    Task<IEnumerable<ClimateData>> FetchData(Region region, DateTime startDate, DateTime endDate);
    Task SyncData(Region region);
}
