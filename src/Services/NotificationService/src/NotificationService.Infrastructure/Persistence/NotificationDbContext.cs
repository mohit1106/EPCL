using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<PriceAlertSubscription> PriceAlertSubscriptions => Set<PriceAlertSubscription>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<NotificationLog>(b =>
        {
            b.ToTable("NotificationLogs"); b.HasKey(n => n.Id);
            b.Property(n => n.Id).HasDefaultValueSql("NEWID()");
            b.Property(n => n.RecipientEmail).HasMaxLength(256);
            b.Property(n => n.RecipientPhone).HasMaxLength(15);
            b.Property(n => n.Channel).IsRequired().HasMaxLength(10).HasConversion<string>();
            b.Property(n => n.Subject).HasMaxLength(200);
            b.Property(n => n.Body).IsRequired();
            b.Property(n => n.Status).IsRequired().HasMaxLength(10).HasConversion<string>()
                .HasDefaultValue(NotificationStatus.Pending);
            b.Property(n => n.TriggerEvent).IsRequired().HasMaxLength(100);
            b.Property(n => n.FailureReason).HasMaxLength(500);
            b.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(n => n.RecipientUserId);
        });

        mb.Entity<PriceAlertSubscription>(b =>
        {
            b.ToTable("PriceAlertSubscriptions"); b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasDefaultValueSql("NEWID()");
            b.Property(p => p.UserId).IsRequired();
            b.Property(p => p.FuelTypeId).IsRequired();
            b.Property(p => p.AlertType).IsRequired().HasMaxLength(20).HasConversion<string>();
            b.Property(p => p.ThresholdPrice).HasColumnType("decimal(8,3)");
            b.Property(p => p.Channel).IsRequired().HasMaxLength(10).HasConversion<string>();
            b.Property(p => p.IsActive).HasDefaultValue(true);
            b.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(p => p.UserId);
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
