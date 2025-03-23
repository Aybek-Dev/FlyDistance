namespace FluDistance.Domain.Models;
public class AirportDistance
{
    public int Id { get; set; }
    public string SourceAirportCode { get; set; }
    public string DestinationAirportCode { get; set; }
    public double DistanceMiles { get; set; }
    public DateTime CalculatedAt { get; set; }
} 