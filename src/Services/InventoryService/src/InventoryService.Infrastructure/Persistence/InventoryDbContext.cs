using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;

namespace InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Tank> Tanks => Set<Tank>();
    public DbSet<StockLoading> StockLoadings => Set<StockLoading>();
    public DbSet<DipReading> DipReadings => Set<DipReading>();
    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Tank>(b =>
        {
            b.ToTable("Tanks");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasDefaultValueSql("NEWID()");
            b.Property(t => t.StationId).IsRequired();
            b.Property(t => t.FuelTypeId).IsRequired();
            b.Property(t => t.TankSerialNumber).IsRequired().HasMaxLength(50).HasColumnType("VARCHAR(50)");
            b.Property(t => t.CapacityLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(t => t.CurrentStockLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(t => t.ReservedLitres).IsRequired().HasColumnType("DECIMAL(10,2)").HasDefaultValue(0m);
            b.Property(t => t.MinThresholdLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(t => t.Status).IsRequired().HasMaxLength(20)
                .HasConversion<string>().HasDefaultValue(TankStatus.Available);
            b.Property(t => t.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(t => t.StationId);
            b.HasIndex(t => t.TankSerialNumber).IsUnique();
            b.Ignore(t => t.AvailableStock);
        });

        mb.Entity<StockLoading>(b =>
        {
            b.ToTable("StockLoadings");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasDefaultValueSql("NEWID()");
            b.Property(s => s.QuantityLoadedLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(s => s.LoadedByUserId).IsRequired();
            b.Property(s => s.TankerNumber).IsRequired().HasMaxLength(30).HasColumnType("VARCHAR(30)");
            b.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(50).HasColumnType("VARCHAR(50)");
            b.Property(s => s.SupplierName).HasMaxLength(150);
            b.Property(s => s.StockBefore).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(s => s.StockAfter).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(s => s.Timestamp).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(s => s.Notes).HasMaxLength(500);
            b.HasOne(s => s.Tank).WithMany(t => t.StockLoadings).HasForeignKey(s => s.TankId);
        });

        mb.Entity<DipReading>(b =>
        {
            b.ToTable("DipReadings");
            b.HasKey(d => d.Id);
            b.Property(d => d.Id).HasDefaultValueSql("NEWID()");
            b.Property(d => d.DipValueLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(d => d.SystemStockLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(d => d.VarianceLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(d => d.VariancePercent).IsRequired().HasColumnType("DECIMAL(5,2)");
            b.Property(d => d.IsFraudFlagged).IsRequired().HasDefaultValue(false);
            b.Property(d => d.RecordedByUserId).IsRequired();
            b.Property(d => d.Timestamp).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(d => d.Notes).HasMaxLength(500);
            b.HasOne(d => d.Tank).WithMany(t => t.DipReadings).HasForeignKey(d => d.TankId);
        });

        mb.Entity<ReplenishmentRequest>(b =>
        {
            b.ToTable("ReplenishmentRequests");
            b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasDefaultValueSql("NEWID()");
            b.Property(r => r.StationId).IsRequired();
            b.Property(r => r.RequestedByUserId).IsRequired();
            b.Property(r => r.RequestedQuantityLitres).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(r => r.UrgencyLevel).IsRequired().HasMaxLength(10)
                .HasConversion<string>().HasDefaultValue(UrgencyLevel.Normal);
            b.Property(r => r.Status).IsRequired().HasMaxLength(20)
                .HasConversion<string>().HasDefaultValue(ReplenishmentStatus.Submitted);
            b.Property(r => r.RequestedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(r => r.RejectionReason).HasMaxLength(500);
            b.Property(r => r.Notes).HasMaxLength(500);
            b.HasIndex(r => r.StationId);
            b.HasOne(r => r.Tank).WithMany(t => t.ReplenishmentRequests).HasForeignKey(r => r.TankId);
        });

        mb.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("ProcessedEvents");
            b.HasKey(e => e.Id);
            b.Property(e => e.EventId).IsRequired();
            b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            b.Property(e => e.ProcessedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(e => e.EventId).IsUnique();
        });
    }
}
