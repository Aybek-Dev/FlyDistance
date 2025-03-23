using FluDistance.Domain.Interfaces;
using FluDistance.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FlyDistance.Application.Services;

public class DistanceCalculator : IDistanceCalculator
{
    private readonly IAirportDistanceRepository _distanceRepository;
    private readonly IAirportRepository _airportRepository;
    private readonly ILogger<DistanceCalculator> _logger;

    public DistanceCalculator(
        IAirportDistanceRepository distanceRepository,
        IAirportRepository airportRepository,
        ILogger<DistanceCalculator> logger)
    {
        _distanceRepository = distanceRepository ?? throw new ArgumentNullException(nameof(distanceRepository));
        _airportRepository = airportRepository ?? throw new ArgumentNullException(nameof(airportRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DistanceResult> CalculateDistanceAsync(string sourceAirportCode, string destinationAirportCode)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(sourceAirportCode, nameof(sourceAirportCode));
            ArgumentNullException.ThrowIfNull(destinationAirportCode, nameof(destinationAirportCode));
            
            var sourceAirport = await _airportRepository.GetAirportByCodeAsync(sourceAirportCode);
            var destinationAirport = await _airportRepository.GetAirportByCodeAsync(destinationAirportCode);
            if (sourceAirport == null || destinationAirport== null)
            { 
                throw new NullReferenceException("Airport not found");
            }
            
            // 1. Проверяем кэш расстояний
            var cachedDistance = await _distanceRepository.GetDistanceAsync(sourceAirportCode, destinationAirportCode);
            
            if (cachedDistance != null)
            {
                // Возвращаем закэшированное расстояние
                _logger.LogInformation(
                    "Found cached distance between {Source} and {Destination}: {Distance} miles",
                    sourceAirportCode, destinationAirportCode, cachedDistance.DistanceMiles);

                return new DistanceResult
                {
                    SourceAirport = sourceAirport,
                    DestinationAirport = destinationAirport,
                    DistanceMiles = Math.Round(cachedDistance.DistanceMiles, 2),
                    FromCache = true
                };
            }

            // 2. Вычисляем расстояние
            var distanceMiles = CalculateDistance(
                sourceAirport.Latitude, sourceAirport.Longitude,
                destinationAirport.Latitude, destinationAirport.Longitude);

            _logger.LogInformation(
                "Calculated new distance between airports {Source} and {Destination}: {Distance} miles",
                sourceAirportCode, destinationAirportCode, distanceMiles);

            // 3. Сохраняем расстояние в кэш
                await _distanceRepository.SaveDistanceAsync(new AirportDistance
                {
                    SourceAirportCode = sourceAirportCode,
                    DestinationAirportCode = destinationAirportCode,
                    DistanceMiles = distanceMiles,
                    CalculatedAt = DateTime.UtcNow
                });
            

            return new DistanceResult
            {
                SourceAirport = sourceAirport,
                DestinationAirport = destinationAirport,
                DistanceMiles = Math.Round(distanceMiles, 2),
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calculating distance between {Source} and {Destination}",
                sourceAirportCode, destinationAirportCode);
            throw;
        }
    }

    // Формула гаверсинусов для расчета расстояния между двумя точками на сфере
    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double EarthRadiusMiles = 3958.8; // Радиус Земли в милях

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMiles * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}