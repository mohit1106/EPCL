using SalesService.Domain.Enums;

namespace SalesService.Domain.Entities;

// ── Pump ───────────────────────────────────────────────────────────
public class Pump
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
    public string PumpName { get; set; } = string.Empty;
    public int NozzleCount { get; set; } = 1;
    public PumpStatus Status { get; set; } = PumpStatus.Active;
    public DateTimeOffset? LastServiced { get; set; }
    public DateTimeOffset? NextServiceDue { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

// ── FuelPrice ──────────────────────────────────────────────────────
public class FuelPrice
{
    public Guid Id { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal PricePerLitre { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid SetByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ── Transaction ────────────────────────────────────────────────────
public class Transaction
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid StationId { get; set; }
    public Guid PumpId { get; set; }
    public Guid TankId { get; set; }
    public Guid FuelTypeId { get; set; }
    public Guid DealerUserId { get; set; }
    public Guid? CustomerUserId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public decimal QuantityLitres { get; set; }
    public decimal PricePerLitre { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? PaymentReferenceId { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Initiated;
    public FraudCheckStatus FraudCheckStatus { get; set; } = FraudCheckStatus.Pending;
    public int LoyaltyPointsEarned { get; set; }
    public int LoyaltyPointsRedeemed { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public bool IsVoided { get; set; }

    // Navigation
    public Pump Pump { get; set; } = null!;
    public VoidedTransaction? VoidedTransaction { get; set; }
}

// ── VoidedTransaction ──────────────────────────────────────────────
public class VoidedTransaction
{
    public Guid Id { get; set; }
    public Guid OriginalTransactionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid VoidedByUserId { get; set; }
    public DateTimeOffset VoidedAt { get; set; } = DateTimeOffset.UtcNow;

    public Transaction OriginalTransaction { get; set; } = null!;
}

// ── Shift ──────────────────────────────────────────────────────────
public class Shift
{
    public Guid Id { get; set; }
    public Guid DealerUserId { get; set; }
    public Guid StationId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string OpeningStockJson { get; set; } = "{}";
    public string? ClosingStockJson { get; set; }
    public decimal TotalLitresSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public bool DiscrepancyFlagged { get; set; }
}

// ── RegisteredVehicle ──────────────────────────────────────────────
public class RegisteredVehicle
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public Guid? FuelTypePreference { get; set; }
    public VehicleType VehicleType { get; set; }
    public string? Nickname { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<FleetVehicle> FleetVehicles { get; set; } = new List<FleetVehicle>();
}

// ── FleetAccount ───────────────────────────────────────────────────
public class FleetAccount
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid ContactUserId { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<FleetVehicle> FleetVehicles { get; set; } = new List<FleetVehicle>();
}

// ── FleetVehicle ───────────────────────────────────────────────────
public class FleetVehicle
{
    public Guid Id { get; set; }
    public Guid FleetAccountId { get; set; }
    public Guid VehicleId { get; set; }
    public decimal? DailyLimitLitres { get; set; }
    public decimal? MonthlyLimitAmount { get; set; }
    public bool IsActive { get; set; } = true;

    public FleetAccount FleetAccount { get; set; } = null!;
    public RegisteredVehicle Vehicle { get; set; } = null!;
}

// ── CustomerWallet ─────────────────────────────────────────────────
public class CustomerWallet
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalLoaded { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}

// ── WalletTransaction ──────────────────────────────────────────────
public class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Pending;
    public Guid? SaleTransactionId { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CustomerWallet Wallet { get; set; } = null!;
}

// ── ProcessedEvent ─────────────────────────────────────────────────
public class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ── ParkingSlot ────────────────────────────────────────────────────
public class ParkingSlot
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public ParkingSlotType SlotType { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ── ParkingBooking ─────────────────────────────────────────────────
public class ParkingBooking
{
    public Guid Id { get; set; }
    public Guid ParkingSlotId { get; set; }
    public Guid StationId { get; set; }
    public Guid CustomerId { get; set; }
    public ParkingSlotType SlotType { get; set; }
    public int DurationHours { get; set; }
    public decimal Amount { get; set; }
    public ParkingBookingStatus Status { get; set; } = ParkingBookingStatus.Initiated;
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public DateTimeOffset BookedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }

    public ParkingSlot ParkingSlot { get; set; } = null!;
}

// ── WalletPaymentRequest ──────────────────────────────────────────
public class WalletPaymentRequest
{
    public Guid Id { get; set; }
    public Guid SaleTransactionId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid DealerUserId { get; set; }
    public Guid StationId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Expired
    public string Description { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
    public string? FuelTypeName { get; set; }
    public decimal? QuantityLitres { get; set; }
    public string PaymentMethod { get; set; } = "Wallet"; // Wallet, UPI, Bank
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}
