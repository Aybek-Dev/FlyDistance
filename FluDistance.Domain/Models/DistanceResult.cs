namespace FluDistance.Domain.Models;

public class DistanceResult
{
    public Airport SourceAirport { get; set; }
    public Airport DestinationAirport { get; set; }
    public double DistanceMiles { get; set; }
    public bool FromCache { get; set; }
}