using System.Text.Json.Serialization;

namespace FluDistance.Domain.Models;

public class Airport
{
    public string Code { get; set; }
    
    public string Name { get; set; }
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public string City { get; set; }
    
    public string Country { get; set; }
}