using FluDistance.Domain.Models;

namespace FluDistance.Domain.Interfaces;

public interface IAirportService
{
    Task<Airport> GetAirportInfoAsync(string code);
}