using System.Globalization;
using FluDistance.Domain.Interfaces;
using FluDistance.Domain.Models;
using Microsoft.Extensions.Logging;

namespace FluDistance.Infrastructure.Services;
public class CsvImportService : ICsvImportService
{
    private readonly IAirportRepository _airportRepository;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(IAirportRepository airportRepository, ILogger<CsvImportService> logger)
    {
        _airportRepository = airportRepository ?? throw new ArgumentNullException(nameof(airportRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(int total, int imported)> ImportAirportsFromCsvAsync(Stream csvStream)
    {
        if (csvStream == null)
        {
            throw new ArgumentNullException(nameof(csvStream));
        }

        try
        {
            _logger.LogInformation("Starting import of airports from CSV");
            
            var airports = new List<Airport>();
            var totalCount = 0;
            var validCount = 0;
            
            using (var reader = new StreamReader(csvStream))
            {
                // Пропускаем заголовок
                var header = await reader.ReadLineAsync();
                _logger.LogInformation("CSV header: {Header}", header);

                while (await reader.ReadLineAsync() is { } line)
                {
                    totalCount++;
                    
                    try
                    {
                        var airport = ParseCsvLine(line);
                        if (airport != null)
                        {
                            airports.Add(airport);
                            validCount++;
                            
                            // сохронять данные пакетами для экономии
                            if (airports.Count >= 100)
                            {
                                await _airportRepository.SaveAirportsAsync(airports);
                                airports.Clear();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing CSV line: {Line}", line);
                    }
                }
                
                // Сохраняем оставшиеся аэропорты
                if (airports.Any())
                {
                    await _airportRepository.SaveAirportsAsync(airports);
                }
            }

            _logger.LogInformation("CSV import completed. Total entries: {Total}, Valid and imported: {Valid}", 
                totalCount, validCount);
            
            return (totalCount, validCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CSV import");
            throw;
        }
    }

    private Airport ParseCsvLine(string line)
    {
        // Разбиваем строку CSV, учитывая кавычки
        var parts = SplitCsvLine(line);
        if (parts.Length < 7)
        {
            _logger.LogWarning("Invalid CSV line format: {Line}", line);
            return null;
        }

        // Ожидаемый формат CSV:
        // country_code,region_name,iata,icao,airport,latitude,longitude
        var countryCode = parts[0];
        var regionName = parts[1];
        var iataCode = parts[2];
        var airportName = parts[4];
        
        // Проверяем наличие IATA кода (обязательное поле)
        if (string.IsNullOrWhiteSpace(iataCode))
        {
            _logger.LogWarning("Skipping entry without IATA code: {Line}", line);
            return null;
        }
        
        // Проверяем и парсим координаты
        if (!double.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var longitude))
        {
            _logger.LogWarning("Invalid coordinates in line: {Line}", line);
            return null;
        }

        return new Airport
        {
            Code = iataCode.ToUpperInvariant(),
            Name = airportName,
            City = regionName,
            Country = countryCode,
            Latitude = latitude,
            Longitude = longitude
        };
    }

    private string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var startPos = 0;
        var insideQuotes = false;
        
        for (var i = 0; i < line.Length; i++)
        {
            switch (line[i])
            {
                case '"':
                    insideQuotes = !insideQuotes;
                    break;
                case ',' when !insideQuotes:
                    result.Add(ExtractField(line.Substring(startPos, i - startPos)));
                    startPos = i + 1;
                    break;
            }
        }
        
        // Добавляем последнее поле
        result.Add(ExtractField(line.Substring(startPos)));
        
        return result.ToArray();
    }
    
    private string ExtractField(string field)
    {
        field = field.Trim();
        
        // Убираем кавычки, если они есть
        if (field.StartsWith("\"") && field.EndsWith("\""))
        {
            field = field.Substring(1, field.Length - 2);
        }
        
        return field;
    }
} 