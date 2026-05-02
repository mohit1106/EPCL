using InventoryService.Domain.Enums;

namespace InventoryService.Domain.Entities;

public class Tank
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
    public string TankSerialNumber { get; set; } = string.Empty;
    public decimal CapacityLitres { get; set; }
    public decimal CurrentStockLitres { get; set; }
    public decimal ReservedLitres { get; set; }
    public decimal MinThresholdLitres { get; set; }
    public TankStatus Status { get; set; } = TankStatus.Available;
    public DateTimeOffset? LastReplenishedAt { get; set; }
    public DateTimeOffset? LastDipReadingAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<StockLoading> StockLoadings { get; set; } = new List<StockLoading>();
    public ICollection<DipReading> DipReadings { get; set; } = new List<DipReading>();
    public ICollection<ReplenishmentRequest> ReplenishmentRequests { get; set; } = new List<ReplenishmentRequest>();

    /// <summary>Available stock = CurrentStock - Reserved.</summary>
    public decimal AvailableStock => CurrentStockLitres - ReservedLitres;
}

public class StockLoading
{
    public Guid Id { get; set; }
    public Guid TankId { get; set; }
    public decimal QuantityLoadedLitres { get; set; }
    public Guid LoadedByUserId { get; set; }
    public string TankerNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public Tank Tank { get; set; } = null!;
}

public class DipReading
{
    public Guid Id { get; set; }
    public Guid TankId { get; set; }
    public decimal DipValueLitres { get; set; }
    public decimal SystemStockLitres { get; set; }
    public decimal VarianceLitres { get; set; }
    public decimal VariancePercent { get; set; }
    public bool IsFraudFlagged { get; set; }
    public Guid RecordedByUserId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public Tank Tank { get; set; } = null!;
}

public class ReplenishmentRequest
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public decimal RequestedQuantityLitres { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Normal;
    public ReplenishmentStatus Status { get; set; } = ReplenishmentStatus.Submitted;
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }

    // ── Extended fields ────────────────────────────────────────────
    /// <summary>Auto-generated order number (e.g., ORD-A1B2C3).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Target pump name for display purposes.</summary>
    public string? TargetPumpName { get; set; }

    /// <summary>Fuel type name for display purposes.</summary>
    public string? FuelTypeName { get; set; }

    /// <summary>Priority label (Standard, High, Urgent).</summary>
    public string Priority { get; set; } = "Standard";

    /// <summary>Requested delivery window.</summary>
    public string? RequestedWindow { get; set; }

    // ── Driver assignment ──────────────────────────────────────────
    public Guid? AssignedDriverId { get; set; }
    public string? AssignedDriverName { get; set; }
    public string? AssignedDriverPhone { get; set; }
    public string? AssignedDriverCode { get; set; }

    // ── Dealer offloading verification ─────────────────────────────
    public DateTimeOffset? DealerVerifiedAt { get; set; }
    public string? DealerVerifiedDriverCode { get; set; }

    // Navigation
    public Tank Tank { get; set; } = null!;
}

public class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
