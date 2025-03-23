using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluDistance.Domain.Interfaces;
using FluDistance.Domain.Models;
using FluDistance.Infrastructure.Data;
using FluDistance.Infrastructure.Repositories;
using FluDistance.Infrastructure.Services;
using FlyDistance.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AirportDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AirportDatabase")));

builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IAirportDistanceRepository, AirportDistanceRepository>();
builder.Services.AddScoped<IAirportService, CachedAirportService>();
builder.Services.AddScoped<IDistanceCalculator, DistanceCalculator>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Airport Distance API, for Shadow mood", Version = "v1.0.0" });
});

var app = builder.Build();

// Настройка HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Airport Distance API, for Shadow mood v1.0.0"));
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AirportDbContext>();
        
            app.Logger.LogInformation("Database created ");
            dbContext.Database.EnsureCreated();
            app.Logger.LogInformation("Database successful created ");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database");
    }
}

// Определение API маршрутов (Minimal API)

// GET /api/airports/{code}
app.MapGet("/api/airports/{code}", async (string code, IAirportService airportService, ILogger<Program> logger) =>
    {
        try
        {
            var airport = await airportService.GetAirportInfoAsync(code);

            if (airport == null)
            {
                return Results.NotFound($"Airport with code {code} not found");
            }

            return Results.Ok(airport);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while getting airport with code {Code}", code);
            return Results.Problem("An error occurred while processing your request");
        }
    })
    .WithName("GetAirport")
    .Produces<Airport>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status500InternalServerError);

// GET /api/distance?from={fromCode}&to={toCode}
app.MapGet("/api/distance",
        async (string from, string to, IDistanceCalculator distanceCalculator, ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                return Results.BadRequest("Both 'from' and 'to' airport codes are required");
            }

            try
            {
                var result = await distanceCalculator.CalculateDistanceAsync(from, to);

                if (result == null)
                {
                    logger.LogWarning("Unable to calculate distance between {From} and {To}, result is null", from, to);
                    return Results.NotFound("One or both airports not found. Please check airport codes and try again.");
                }

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while calculating distance between {From} and {To}", from, to);
                return Results.Problem("An error occurred while processing your request");
            }
        })
    .WithName("GetDistance")
    .Produces<DistanceResult>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status500InternalServerError);

// POST /api/import/csv
app.MapPost("/api/import/csv", async (IFormFile file, ICsvImportService csvImportService, ILogger<Program> logger) =>
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("No CSV file uploaded or file is empty.");
            }

            if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".csv")
            {
                return Results.BadRequest("Only CSV files are supported.");
            }

            await using var stream = file.OpenReadStream();
            var result = await csvImportService.ImportAirportsFromCsvAsync(stream);
            return Results.Ok(new
            {
                Message = "CSV file processed successfully",
                TotalEntries = result.total,
                ImportedEntries = result.imported,
                SkippedEntries = result.total - result.imported
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing CSV import");
            return Results.Problem("An error occurred while processing the CSV file.");
        }
    })
    .DisableAntiforgery()
    .WithName("ImportCsv")
    .Accepts<IFormFile>("multipart/form-data")
    .Produces<object>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError);

app.Run();