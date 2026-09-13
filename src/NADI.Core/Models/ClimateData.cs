namespace NADI.Core.Models;

public class ClimateData
{
    public int DataId { get; set; }
    public DateTime Date { get; set; }
    public double Temperature { get; set; }
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public double Rainfall { get; set; }
    public double Humidity { get; set; }
    public double WindSpeed { get; set; }

    public int RegionId { get; set; }
    public Region? Region { get; set; }

    public double CalculateAverage()
    {
        return (MinTemperature + MaxTemperature) / 2.0;
    }
}
