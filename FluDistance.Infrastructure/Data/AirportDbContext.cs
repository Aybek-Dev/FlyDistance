using FluDistance.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FluDistance.Infrastructure.Data;

public class AirportDbContext : DbContext
{
    public AirportDbContext(DbContextOptions<AirportDbContext> options) : base(options)
    {
    }

    public DbSet<Airport> Airports { get; set; }
    public DbSet<AirportDistance> AirportDistances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Airport>(entity =>
        {
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
        });
        
        modelBuilder.Entity<AirportDistance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceAirportCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.DestinationAirportCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.DistanceMiles).IsRequired();
            entity.Property(e => e.CalculatedAt).IsRequired();
            
            // уникальный индекс для пары аэропортов в обоих направлениях
            entity.HasIndex(e => new { e.SourceAirportCode, e.DestinationAirportCode }).IsUnique();
        });
    }
}