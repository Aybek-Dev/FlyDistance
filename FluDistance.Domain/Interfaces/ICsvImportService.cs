namespace FluDistance.Domain.Interfaces;

public interface ICsvImportService
{
    Task<(int total, int imported)> ImportAirportsFromCsvAsync(Stream csvStream);
} 