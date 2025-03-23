using FluDistance.Domain.Models;

namespace FluDistance.Domain.Interfaces;

public interface IAirportRepository
{
    Task<Airport> GetAirportByCodeAsync(string code);
    Task SaveAirportAsync(Airport airport);
    Task SaveAirportsAsync(IEnumerable<Airport> airports);
}