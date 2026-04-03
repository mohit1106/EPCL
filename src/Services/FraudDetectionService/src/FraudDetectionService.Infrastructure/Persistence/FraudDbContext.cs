using Microsoft.EntityFrameworkCore;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;

namespace FraudDetectionService.Infrastructure.Persistence;

public class FraudDbContext : DbContext
{
    public FraudDbContext(DbContextOptions<FraudDbContext> options) : base(options) { }

    public DbSet<FraudAlert> FraudAlerts => Set<FraudAlert>();
    public DbSet<FraudRuleEvaluation> FraudRuleEvaluations => Set<FraudRuleEvaluation>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<FraudAlert>(b =>
        {
            b.ToTable("FraudAlerts"); b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            b.Property(a => a.TransactionId).IsRequired();
            b.Property(a => a.StationId).IsRequired();
            b.Property(a => a.RuleTriggered).IsRequired().HasMaxLength(50);
            b.Property(a => a.Severity).IsRequired().HasMaxLength(10).HasConversion<string>();
            b.Property(a => a.Description).IsRequired().HasMaxLength(1000);
            b.Property(a => a.Status).IsRequired().HasMaxLength(20).HasConversion<string>().HasDefaultValue(AlertStatus.Open);
            b.Property(a => a.ReviewNotes).HasMaxLength(500);
            b.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(a => a.TransactionId);
            b.HasIndex(a => a.StationId);
            b.HasIndex(a => a.Status);
        });

        mb.Entity<FraudRuleEvaluation>(b =>
        {
            b.ToTable("FraudRuleEvaluations"); b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            b.Property(e => e.TransactionId).IsRequired();
            b.Property(e => e.RuleName).IsRequired().HasMaxLength(50);
            b.Property(e => e.Passed).IsRequired();
            b.Property(e => e.EvaluatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.Property(e => e.Details).HasMaxLength(500);
            b.HasIndex(e => e.TransactionId);
        });

        mb.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("ProcessedEvents"); b.HasKey(e => e.Id);
            b.Property(e => e.EventId).IsRequired();
            b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            b.Property(e => e.ProcessedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(e => e.EventId).IsUnique();
        });
    }
}
