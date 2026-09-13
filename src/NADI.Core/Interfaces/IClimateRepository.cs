using NADI.Core.Models;

namespace NADI.Core.Interfaces;

public interface IClimateRepository
{
    Task<IEnumerable<ClimateData>> GetClimateData();
    Task SaveClimateData(ClimateData data);
    Task<IEnumerable<ClimateData>> GetByRegion(int regionId);
    Task<IEnumerable<ClimateData>> GetByPeriod(int regionId, DateTime startDate, DateTime endDate);
}
