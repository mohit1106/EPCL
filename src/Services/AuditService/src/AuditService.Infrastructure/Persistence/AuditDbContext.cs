using Microsoft.EntityFrameworkCore;
using AuditService.Domain.Entities;

namespace AuditService.Infrastructure.Persistence;

/// <summary>
/// EPCL_Audit DbContext — APPEND-ONLY design.
/// The AuditLogs table only supports INSERT + SELECT.
/// No SaveChanges override for update/delete interception is needed
/// because the repository only exposes AppendAsync.
/// </summary>
public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            b.Property(a => a.EventId).IsRequired();
            b.HasIndex(a => a.EventId).IsUnique();
            b.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            b.Property(a => a.EntityId).IsRequired();
            b.Property(a => a.Operation).IsRequired().HasMaxLength(10);
            b.Property(a => a.ChangedByRole).HasMaxLength(20);
            b.Property(a => a.IpAddress).HasMaxLength(45);
            b.Property(a => a.CorrelationId).HasMaxLength(50);
            b.Property(a => a.ServiceName).IsRequired().HasMaxLength(50);
            b.Property(a => a.Timestamp).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            // Indexes for common query patterns
            b.HasIndex(a => a.EntityType);
            b.HasIndex(a => a.EntityId);
            b.HasIndex(a => a.ChangedByUserId);
            b.HasIndex(a => a.Timestamp);
            b.HasIndex(a => a.ServiceName);
        });
    }
}
