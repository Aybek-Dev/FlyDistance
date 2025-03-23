using FluDistance.Domain.Interfaces;
using FluDistance.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FluDistance.Infrastructure.Services;
public class CachedAirportService : IAirportService
{
        private readonly IAirportRepository _airportRepository;
        private readonly ILogger<CachedAirportService> _logger;

        public CachedAirportService(
            IAirportRepository airportRepository,
            ILogger<CachedAirportService> logger)
        {
            _airportRepository = airportRepository ?? throw new ArgumentNullException(nameof(airportRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Airport> GetAirportInfoAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Airport code cannot be null or empty", nameof(code));
            }

            code = code.ToUpperInvariant();
            
            try
            {
                var airport = await _airportRepository.GetAirportByCodeAsync(code);
                
                if (airport != null)
                {
                    _logger.LogInformation("Airport {Code} found in cache", code);
                    return airport;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting airport info for {Code}", code);
                throw;
            }
        }
}