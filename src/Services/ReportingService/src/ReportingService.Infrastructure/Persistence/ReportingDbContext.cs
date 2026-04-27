using Microsoft.EntityFrameworkCore;
using ReportingService.Domain.Entities;

namespace ReportingService.Infrastructure.Persistence;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<DailySalesSummary> DailySalesSummaries => Set<DailySalesSummary>();
    public DbSet<MonthlyStationReport> MonthlyStationReports => Set<MonthlyStationReport>();
    public DbSet<GeneratedReport> GeneratedReports => Set<GeneratedReport>();
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<StockPrediction> StockPredictions => Set<StockPrediction>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<DailySalesSummary>(b =>
        {
            b.ToTable("DailySalesSummaries"); b.HasKey(d => d.Id);
            b.Property(d => d.Id).HasDefaultValueSql("NEWID()");
            b.Property(d => d.TotalLitresSold).HasColumnType("decimal(12,3)");
            b.Property(d => d.TotalRevenue).HasColumnType("decimal(15,2)");
            b.HasIndex(d => d.StationId);
            b.HasIndex(d => new { d.StationId, d.FuelTypeId, d.Date }).IsUnique();
        });

        mb.Entity<MonthlyStationReport>(b =>
        {
            b.ToTable("MonthlyStationReports"); b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasDefaultValueSql("NEWID()");
            b.Property(m => m.TotalLitresSold).HasColumnType("decimal(14,3)");
            b.Property(m => m.TotalRevenue).HasColumnType("decimal(16,2)");
            b.Property(m => m.PetrolLitres).HasColumnType("decimal(14,3)");
            b.Property(m => m.DieselLitres).HasColumnType("decimal(14,3)");
            b.Property(m => m.CngLitres).HasColumnType("decimal(14,3)");
            b.HasIndex(m => m.StationId);
        });

        mb.Entity<GeneratedReport>(b =>
        {
            b.ToTable("GeneratedReports"); b.HasKey(g => g.Id);
            b.Property(g => g.Id).HasDefaultValueSql("NEWID()");
            b.Property(g => g.ReportType).IsRequired().HasMaxLength(30);
            b.Property(g => g.Format).IsRequired().HasMaxLength(5);
            b.Property(g => g.FilePath).IsRequired().HasMaxLength(500);
            b.Property(g => g.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
            b.Property(g => g.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        mb.Entity<ScheduledReport>(b =>
        {
            b.ToTable("ScheduledReports"); b.HasKey(s => s.Id);
            b.Property(s => s.ReportType).IsRequired().HasMaxLength(30);
            b.Property(s => s.CronExpression).IsRequired().HasMaxLength(50);
            b.Property(s => s.Format).IsRequired().HasMaxLength(5);
        });

        mb.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("ProcessedEvents"); b.HasKey(e => e.Id);
            b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            b.HasIndex(e => e.EventId).IsUnique();
        });

        mb.Entity<StockPrediction>(b =>
        {
            b.ToTable("StockPredictions");
            b.HasKey(s => s.Id);
            b.Property(s => s.CurrentStockLitres).HasColumnType("decimal(10,2)");
            b.Property(s => s.AvgDailyConsumptionL).HasColumnType("decimal(10,3)");
            b.Property(s => s.DaysUntilEmpty).HasColumnType("decimal(6,1)");
            b.HasIndex(s => s.StationId);
            b.HasIndex(s => s.TankId);
            b.HasIndex(s => s.DaysUntilEmpty);
        });
    }
}
