using FluDistance.Domain.Models;

namespace FluDistance.Domain.Interfaces;
public interface IAirportDistanceRepository
{
    Task<AirportDistance> GetDistanceAsync(string sourceAirportCode, string destinationAirportCode);
    Task SaveDistanceAsync(AirportDistance airportDistance);
} 