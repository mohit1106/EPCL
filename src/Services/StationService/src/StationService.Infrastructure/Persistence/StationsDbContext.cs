using Microsoft.EntityFrameworkCore;
using StationService.Domain.Entities;

namespace StationService.Infrastructure.Persistence;

public class StationsDbContext : DbContext
{
    public StationsDbContext(DbContextOptions<StationsDbContext> options) : base(options) { }

    public DbSet<Station> Stations => Set<Station>();
    public DbSet<FuelType> FuelTypes => Set<FuelType>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Station ──────────────────────────────────────────────
        modelBuilder.Entity<Station>(b =>
        {
            b.ToTable("Stations");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasDefaultValueSql("NEWID()");
            b.Property(s => s.StationCode).IsRequired().HasMaxLength(15).HasColumnType("VARCHAR(15)");
            b.Property(s => s.StationName).IsRequired().HasMaxLength(150);
            b.Property(s => s.DealerUserId).IsRequired();
            b.Property(s => s.AddressLine1).IsRequired().HasMaxLength(250);
            b.Property(s => s.City).IsRequired().HasMaxLength(100);
            b.Property(s => s.State).IsRequired().HasMaxLength(100);
            b.Property(s => s.PinCode).IsRequired().HasMaxLength(6).HasColumnType("VARCHAR(6)");
            b.Property(s => s.Latitude).IsRequired().HasColumnType("DECIMAL(9,6)");
            b.Property(s => s.Longitude).IsRequired().HasColumnType("DECIMAL(9,6)");
            b.Property(s => s.LicenseNumber).IsRequired().HasMaxLength(60).HasColumnType("VARCHAR(60)");
            b.Property(s => s.OperatingHoursStart).IsRequired();
            b.Property(s => s.OperatingHoursEnd).IsRequired();
            b.Property(s => s.Is24x7).IsRequired().HasDefaultValue(false);
            b.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(s => s.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(s => s.UpdatedAt);

            b.HasIndex(s => s.StationCode).IsUnique();
            b.HasIndex(s => s.LicenseNumber).IsUnique();
            b.HasIndex(s => s.DealerUserId);
            b.HasIndex(s => new { s.City, s.State });
        });

        // ── FuelType ─────────────────────────────────────────────
        modelBuilder.Entity<FuelType>(b =>
        {
            b.ToTable("FuelTypes");
            b.HasKey(f => f.Id);
            b.Property(f => f.Id).HasDefaultValueSql("NEWID()");
            b.Property(f => f.Name).IsRequired().HasMaxLength(50);
            b.Property(f => f.Description).HasMaxLength(200);
            b.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);

            b.HasIndex(f => f.Name).IsUnique();
        });

        // ── ProcessedEvent ───────────────────────────────────────
        modelBuilder.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("ProcessedEvents");
            b.HasKey(e => e.Id);
            b.Property(e => e.EventId).IsRequired();
            b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            b.Property(e => e.ProcessedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(e => e.EventId).IsUnique();
        });

        // ── Seed data: default fuel types ────────────────────────
        modelBuilder.Entity<FuelType>().HasData(
            new FuelType { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-000000000001"), Name = "Petrol", Description = "Regular unleaded petrol", IsActive = true },
            new FuelType { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-000000000002"), Name = "Diesel", Description = "High-speed diesel", IsActive = true },
            new FuelType { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-000000000003"), Name = "CNG", Description = "Compressed natural gas", IsActive = true },
            new FuelType { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-000000000004"), Name = "PremiumPetrol", Description = "Premium high-octane petrol", IsActive = true },
            new FuelType { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-000000000005"), Name = "PremiumDiesel", Description = "Premium diesel fuel", IsActive = true }
        );
    }
}
