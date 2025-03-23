using FluDistance.Domain.Interfaces;
using FluDistance.Domain.Models;
using FluDistance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FluDistance.Infrastructure.Repositories;
public class AirportDistanceRepository : IAirportDistanceRepository
{
    private readonly AirportDbContext _dbContext;
    private readonly ILogger<AirportDistanceRepository> _logger;

    public AirportDistanceRepository(AirportDbContext dbContext, ILogger<AirportDistanceRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public async Task<AirportDistance> GetDistanceAsync(string sourceAirportCode, string destinationAirportCode)
    {
        if (string.IsNullOrEmpty(sourceAirportCode))
        {
            throw new ArgumentException("Source airport code cannot be null or empty", nameof(sourceAirportCode));
        }

        if (string.IsNullOrEmpty(destinationAirportCode))
        {
            throw new ArgumentException("Destination airport code cannot be null or empty", nameof(destinationAirportCode));
        }

        sourceAirportCode = sourceAirportCode.ToUpperInvariant();
        destinationAirportCode = destinationAirportCode.ToUpperInvariant();

        try
        {
            // Поиск расстояния в обоих направлениях (A -> B и B -> A)
            var distance = await _dbContext.AirportDistances.AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    (d.SourceAirportCode == sourceAirportCode && d.DestinationAirportCode == destinationAirportCode) ||
                    (d.SourceAirportCode == destinationAirportCode && d.DestinationAirportCode == sourceAirportCode));

            if (distance != null)
            {
                _logger.LogInformation(
                    "Distance between airports {Source} and {Destination} found in cache",
                    sourceAirportCode, destinationAirportCode);
            }

            return distance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while trying to get distance between airports {Source} and {Destination}",
                sourceAirportCode, destinationAirportCode);
            throw;
        }
    }
    public async Task SaveDistanceAsync(AirportDistance airportDistance)
    {
        ArgumentNullException.ThrowIfNull(airportDistance, nameof(airportDistance));

        try
        {
            airportDistance.SourceAirportCode = airportDistance.SourceAirportCode.ToUpperInvariant();
            airportDistance.DestinationAirportCode = airportDistance.DestinationAirportCode.ToUpperInvariant();
            
            // Проверяем, существует ли уже запись о расстоянии
            var existingDistance = await _dbContext.AirportDistances
                .FirstOrDefaultAsync(d =>
                    (d.SourceAirportCode == airportDistance.SourceAirportCode && 
                     d.DestinationAirportCode == airportDistance.DestinationAirportCode) ||
                    (d.SourceAirportCode == airportDistance.DestinationAirportCode && 
                     d.DestinationAirportCode == airportDistance.SourceAirportCode));

            if (existingDistance == null)
            {
                // Если нет, добавляем новую запись
                airportDistance.CalculatedAt = DateTime.UtcNow;
                await _dbContext.AirportDistances.AddAsync(airportDistance);
                _logger.LogInformation(
                    "Adding new distance between airports {Source} and {Destination} to cache",
                    airportDistance.SourceAirportCode, airportDistance.DestinationAirportCode);
            }
            else
            {
                // Если существует, обновляем
                existingDistance.DistanceMiles = airportDistance.DistanceMiles;
                existingDistance.CalculatedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Updating existing distance between airports {Source} and {Destination} in cache",
                    airportDistance.SourceAirportCode, airportDistance.DestinationAirportCode);
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while trying to save distance between airports {Source} and {Destination}",
                airportDistance.SourceAirportCode, airportDistance.DestinationAirportCode);
            throw;
        }
    }
} 