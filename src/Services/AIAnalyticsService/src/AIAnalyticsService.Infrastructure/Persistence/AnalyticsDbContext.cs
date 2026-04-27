using AIAnalyticsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIAnalyticsService.Infrastructure.Persistence;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options) { }

    public DbSet<ConversationMessage> ConversationHistory => Set<ConversationMessage>();
    public DbSet<QueryLog> QueryLogs => Set<QueryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ConversationMessage>(e =>
        {
            e.ToTable("ConversationHistory");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.SessionId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Role).HasMaxLength(10).IsRequired();
            e.Property(x => x.Content).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasIndex(x => x.UserId).HasDatabaseName("IX_ConvHistory_UserId");
            e.HasIndex(x => x.SessionId).HasDatabaseName("IX_ConvHistory_SessionId");
        });

        modelBuilder.Entity<QueryLog>(e =>
        {
            e.ToTable("QueryLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired();
            e.Property(x => x.UserRole).HasMaxLength(20).IsRequired();
            e.Property(x => x.Question).HasMaxLength(2000).IsRequired();
            e.Property(x => x.TotalMs).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
