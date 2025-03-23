using FluDistance.Domain.Models;

namespace FluDistance.Domain.Interfaces;

public interface IDistanceCalculator
{
    Task<DistanceResult> CalculateDistanceAsync(string sourceAirportCode, string destinationAirportCode);
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
}