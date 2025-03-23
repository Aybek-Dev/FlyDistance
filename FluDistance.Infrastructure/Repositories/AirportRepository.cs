using FluDistance.Domain.Interfaces;
using FluDistance.Domain.Models;
using FluDistance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FluDistance.Infrastructure.Repositories;
public class AirportRepository : IAirportRepository
{
    private readonly AirportDbContext _dbContext;
    private readonly ILogger<AirportRepository> _logger;

    public AirportRepository(AirportDbContext dbContext, ILogger<AirportRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Airport> GetAirportByCodeAsync(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Airport code cannot be null or empty", nameof(code));
        }

        try
        {
            return await _dbContext.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Code == code.ToUpperInvariant());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while trying to get airport with code {Code} from database", code);
            throw;
        }
    }

    public async Task SaveAirportAsync(Airport airport)
    {
        if (airport == null)
        {
            throw new ArgumentNullException(nameof(airport));
        }

        try
        {
            var existingAirport = await _dbContext.Airports.FindAsync(airport.Code);
                
            if (existingAirport == null)
            {
                await _dbContext.Airports.AddAsync(airport);
            }
            else
            {
                _dbContext.Entry(existingAirport).CurrentValues.SetValues(airport);
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while trying to save airport with code {Code} to database", airport.Code);
            throw;
        }
    }
    
    public async Task SaveAirportsAsync(IEnumerable<Airport> airports)
    {
        if (airports == null)
        {
            throw new ArgumentNullException(nameof(airports));
        }

        try
        {
            foreach (var airport in airports)
            {
                // Проверяем, существует ли уже аэропорт с таким кодом
                var existingAirport = await _dbContext.Airports.FindAsync(airport.Code);
                
                if (existingAirport == null)
                {
                    // Если нет, добавляем новый
                    await _dbContext.Airports.AddAsync(airport);
                }
                else
                {
                    _dbContext.Entry(existingAirport).CurrentValues.SetValues(airport);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully saved batch of airports to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while trying to save multiple airports to database");
            throw;
        }
    }
}