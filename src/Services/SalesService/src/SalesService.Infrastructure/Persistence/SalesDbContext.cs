using Microsoft.EntityFrameworkCore;
using SalesService.Domain.Entities;
using SalesService.Domain.Enums;

namespace SalesService.Infrastructure.Persistence;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<Pump> Pumps => Set<Pump>();
    public DbSet<FuelPrice> FuelPrices => Set<FuelPrice>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<VoidedTransaction> VoidedTransactions => Set<VoidedTransaction>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<RegisteredVehicle> RegisteredVehicles => Set<RegisteredVehicle>();
    public DbSet<FleetAccount> FleetAccounts => Set<FleetAccount>();
    public DbSet<FleetVehicle> FleetVehicles => Set<FleetVehicle>();
    public DbSet<CustomerWallet> CustomerWallets => Set<CustomerWallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<FuelPreAuthorization> FuelPreAuthorizations => Set<FuelPreAuthorization>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Pump ─────────────────────────────────────────────
        mb.Entity<Pump>(b =>
        {
            b.ToTable("Pumps"); b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasDefaultValueSql("NEWID()");
            b.Property(p => p.StationId).IsRequired();
            b.Property(p => p.FuelTypeId).IsRequired();
            b.Property(p => p.PumpName).IsRequired().HasMaxLength(50);
            b.Property(p => p.NozzleCount).IsRequired().HasDefaultValue(1);
            b.Property(p => p.Status).IsRequired().HasMaxLength(20).HasConversion<string>().HasDefaultValue(PumpStatus.Active);
            b.Property(p => p.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(p => p.StationId);
        });

        // ── FuelPrice ────────────────────────────────────────
        mb.Entity<FuelPrice>(b =>
        {
            b.ToTable("FuelPrices"); b.HasKey(p => p.Id);
            b.Property(p => p.Id).HasDefaultValueSql("NEWID()");
            b.Property(p => p.FuelTypeId).IsRequired();
            b.Property(p => p.PricePerLitre).IsRequired().HasColumnType("DECIMAL(8,3)");
            b.Property(p => p.EffectiveFrom).IsRequired();
            b.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(p => p.SetByUserId).IsRequired();
            b.Property(p => p.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(p => p.FuelTypeId);
        });

        // ── Transaction ──────────────────────────────────────
        mb.Entity<Transaction>(b =>
        {
            b.ToTable("Transactions"); b.HasKey(t => t.Id);
            b.Property(t => t.Id).HasDefaultValueSql("NEWID()");
            b.Property(t => t.ReceiptNumber).IsRequired().HasMaxLength(25).HasColumnType("VARCHAR(25)");
            b.Property(t => t.StationId).IsRequired();
            b.Property(t => t.FuelTypeId).IsRequired();
            b.Property(t => t.DealerUserId).IsRequired();
            b.Property(t => t.VehicleNumber).IsRequired().HasMaxLength(15).HasColumnType("VARCHAR(15)");
            b.Property(t => t.QuantityLitres).IsRequired().HasColumnType("DECIMAL(10,3)");
            b.Property(t => t.PricePerLitre).IsRequired().HasColumnType("DECIMAL(8,3)");
            b.Property(t => t.TotalAmount).IsRequired().HasColumnType("DECIMAL(12,2)");
            b.Property(t => t.PaymentMethod).IsRequired().HasMaxLength(20).HasConversion<string>();
            b.Property(t => t.PaymentReferenceId).HasMaxLength(100);
            b.Property(t => t.Status).IsRequired().HasMaxLength(20).HasConversion<string>().HasDefaultValue(TransactionStatus.Initiated);
            b.Property(t => t.FraudCheckStatus).IsRequired().HasMaxLength(20).HasConversion<string>().HasDefaultValue(FraudCheckStatus.Pending);
            b.Property(t => t.Timestamp).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.Property(t => t.IsVoided).IsRequired().HasDefaultValue(false);
            b.HasIndex(t => t.ReceiptNumber).IsUnique();
            b.HasIndex(t => t.StationId);
            b.HasOne(t => t.Pump).WithMany(p => p.Transactions).HasForeignKey(t => t.PumpId);
        });

        // ── VoidedTransaction ────────────────────────────────
        mb.Entity<VoidedTransaction>(b =>
        {
            b.ToTable("VoidedTransactions"); b.HasKey(v => v.Id);
            b.Property(v => v.Id).HasDefaultValueSql("NEWID()");
            b.Property(v => v.Reason).IsRequired().HasMaxLength(500);
            b.Property(v => v.VoidedByUserId).IsRequired();
            b.Property(v => v.VoidedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(v => v.OriginalTransactionId).IsUnique();
            b.HasOne(v => v.OriginalTransaction).WithOne(t => t.VoidedTransaction)
                .HasForeignKey<VoidedTransaction>(v => v.OriginalTransactionId);
        });

        // ── Shift ────────────────────────────────────────────
        mb.Entity<Shift>(b =>
        {
            b.ToTable("Shifts"); b.HasKey(s => s.Id);
            b.Property(s => s.Id).HasDefaultValueSql("NEWID()");
            b.Property(s => s.DealerUserId).IsRequired();
            b.Property(s => s.StationId).IsRequired();
            b.Property(s => s.StartedAt).IsRequired();
            b.Property(s => s.OpeningStockJson).IsRequired();
            b.Property(s => s.TotalLitresSold).HasColumnType("DECIMAL(10,3)").HasDefaultValue(0m);
            b.Property(s => s.TotalRevenue).HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            b.Property(s => s.TotalTransactions).HasDefaultValue(0);
            b.Property(s => s.DiscrepancyFlagged).HasDefaultValue(false);
        });

        // ── RegisteredVehicle ────────────────────────────────
        mb.Entity<RegisteredVehicle>(b =>
        {
            b.ToTable("RegisteredVehicles"); b.HasKey(v => v.Id);
            b.Property(v => v.Id).HasDefaultValueSql("NEWID()");
            b.Property(v => v.CustomerId).IsRequired();
            b.Property(v => v.RegistrationNumber).IsRequired().HasMaxLength(15).HasColumnType("VARCHAR(15)");
            b.Property(v => v.VehicleType).IsRequired().HasMaxLength(20).HasConversion<string>();
            b.Property(v => v.Nickname).HasMaxLength(50);
            b.Property(v => v.IsActive).HasDefaultValue(true);
            b.Property(v => v.RegisteredAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(v => v.RegistrationNumber).IsUnique();
            b.HasIndex(v => v.CustomerId);
        });

        // ── FleetAccount ─────────────────────────────────────
        mb.Entity<FleetAccount>(b =>
        {
            b.ToTable("FleetAccounts"); b.HasKey(f => f.Id);
            b.Property(f => f.Id).HasDefaultValueSql("NEWID()");
            b.Property(f => f.CompanyName).IsRequired().HasMaxLength(200);
            b.Property(f => f.ContactUserId).IsRequired();
            b.Property(f => f.CreditLimit).HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            b.Property(f => f.CurrentBalance).HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            b.Property(f => f.IsActive).HasDefaultValue(true);
            b.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── FleetVehicle ─────────────────────────────────────
        mb.Entity<FleetVehicle>(b =>
        {
            b.ToTable("FleetVehicles"); b.HasKey(f => f.Id);
            b.Property(f => f.Id).HasDefaultValueSql("NEWID()");
            b.Property(f => f.DailyLimitLitres).HasColumnType("DECIMAL(8,2)");
            b.Property(f => f.MonthlyLimitAmount).HasColumnType("DECIMAL(10,2)");
            b.Property(f => f.IsActive).HasDefaultValue(true);
            b.HasOne(f => f.FleetAccount).WithMany(a => a.FleetVehicles).HasForeignKey(f => f.FleetAccountId);
            b.HasOne(f => f.Vehicle).WithMany(v => v.FleetVehicles).HasForeignKey(f => f.VehicleId);
        });

        // ── CustomerWallet ───────────────────────────────────
        mb.Entity<CustomerWallet>(b =>
        {
            b.ToTable("CustomerWallets"); b.HasKey(w => w.Id);
            b.Property(w => w.Id).HasDefaultValueSql("NEWID()");
            b.Property(w => w.CustomerId).IsRequired();
            b.Property(w => w.Balance).HasColumnType("DECIMAL(12,2)").HasDefaultValue(0m);
            b.Property(w => w.TotalLoaded).HasColumnType("DECIMAL(14,2)").HasDefaultValue(0m);
            b.Property(w => w.IsActive).HasDefaultValue(true);
            b.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(w => w.CustomerId).IsUnique();
        });

        // ── WalletTransaction ────────────────────────────────
        mb.Entity<WalletTransaction>(b =>
        {
            b.ToTable("WalletTransactions"); b.HasKey(w => w.Id);
            b.Property(w => w.Id).HasDefaultValueSql("NEWID()");
            b.Property(w => w.Type).IsRequired().HasMaxLength(10).HasConversion<string>();
            b.Property(w => w.Amount).IsRequired().HasColumnType("DECIMAL(10,2)");
            b.Property(w => w.BalanceAfter).IsRequired().HasColumnType("DECIMAL(12,2)");
            b.Property(w => w.RazorpayOrderId).HasMaxLength(100);
            b.Property(w => w.RazorpayPaymentId).HasMaxLength(100);
            b.Property(w => w.Status).IsRequired().HasMaxLength(20).HasConversion<string>().HasDefaultValue(WalletTransactionStatus.Pending);
            b.Property(w => w.Description).HasMaxLength(200);
            b.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasOne(w => w.Wallet).WithMany(c => c.WalletTransactions).HasForeignKey(w => w.WalletId);
        });

        // ── ProcessedEvent ───────────────────────────────────
        mb.Entity<ProcessedEvent>(b =>
        {
            b.ToTable("ProcessedEvents"); b.HasKey(e => e.Id);
            b.Property(e => e.EventId).IsRequired();
            b.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            b.Property(e => e.ProcessedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(e => e.EventId).IsUnique();
        });

        // ── FuelPreAuthorization ─────────────────────────────
        mb.Entity<FuelPreAuthorization>(b =>
        {
            b.ToTable("FuelPreAuthorizations"); b.HasKey(f => f.Id);
            b.Property(f => f.Id).HasDefaultValueSql("NEWID()");
            b.Property(f => f.AuthorizedAmountINR).HasColumnType("DECIMAL(10,2)");
            b.Property(f => f.AuthorizedLitres).HasColumnType("DECIMAL(10,3)");
            b.Property(f => f.AuthCode).IsRequired().HasMaxLength(10);
            b.Property(f => f.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            b.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            b.HasIndex(f => f.AuthCode).IsUnique();
        });
    }
}
