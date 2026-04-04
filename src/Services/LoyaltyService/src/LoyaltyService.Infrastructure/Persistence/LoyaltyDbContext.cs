using Microsoft.EntityFrameworkCore;
using LoyaltyService.Domain.Entities;

namespace LoyaltyService.Infrastructure.Persistence;

public class LoyaltyDbContext : DbContext
{
    public LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options) : base(options) { }

    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<ReferralCode> ReferralCodes => Set<ReferralCode>();
    public DbSet<ReferralRedemption> ReferralRedemptions => Set<ReferralRedemption>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<LoyaltyAccount>(b =>
        {
            b.ToTable("LoyaltyAccounts"); b.HasKey(a => a.Id);
            b.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            b.Property(a => a.CustomerId).IsRequired();
            b.HasIndex(a => a.CustomerId).IsUnique();
            b.Property(a => a.PointsBalance).HasDefaultValue(0);
            b.Property(a => a.LifetimePoints).HasDefaultValue(0);
            b.Property(a => a.Tier).IsRequired().HasMaxLength(10).HasDefaultValue("Silver");
            b.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        mb.Entity<LoyaltyTransaction>(b =>
        {
            b.ToTable("LoyaltyTransactions"); b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasDefaultValueSql("NEWID()");
            b.Property(t => t.Type).IsRequired().HasMaxLength(10);
            b.Property(t => t.Description).HasMaxLength(200);
            b.Property(t => t.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            b.HasOne(t => t.LoyaltyAccount).WithMany().HasForeignKey(t => t.LoyaltyAccountId);
            b.HasIndex(t => t.LoyaltyAccountId);
            b.HasIndex(t => t.SaleTransactionId);
        });

        mb.Entity<ReferralCode>(b =>
        {
            b.ToTable("ReferralCodes"); b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasDefaultValueSql("NEWID()");
            b.Property(r => r.CustomerId).IsRequired();
            b.HasIndex(r => r.CustomerId).IsUnique();
            b.Property(r => r.Code).IsRequired().HasMaxLength(8);
            b.HasIndex(r => r.Code).IsUnique();
            b.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        mb.Entity<ReferralRedemption>(b =>
        {
            b.ToTable("ReferralRedemptions"); b.HasKey(r => r.Id);
            b.Property(r => r.Id).HasDefaultValueSql("NEWID()");
            b.HasOne(r => r.ReferralCode).WithMany().HasForeignKey(r => r.ReferralCodeId);
            b.HasIndex(r => r.RedeemedByCustomerId).IsUnique();
            b.Property(r => r.RedeemedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        mb.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("ProcessedEvents"); b.HasKey(e => e.Id);
            b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            b.HasIndex(e => e.EventId).IsUnique();
        });
    }
}
