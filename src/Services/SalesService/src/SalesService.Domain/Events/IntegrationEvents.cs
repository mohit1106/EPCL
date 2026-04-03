namespace SalesService.Domain.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
}

// ── Saga Step 1: Published by Sales ────────────────────────────────
public class SaleInitiatedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal QuantityLitres { get; set; }
    public Guid DealerUserId { get; set; }
}

// ── Saga Step 3a: Published after stock reserved ───────────────────
public class SaleCompletedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
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
    public string PaymentMethod { get; set; } = string.Empty;
}

// ── Saga Step 3b: Published on reservation failure ─────────────────
public class SaleCancelledEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid TankId { get; set; }
    public decimal ReservedLitres { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ── Voided Transaction ─────────────────────────────────────────────
public class TransactionVoidedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal QuantityLitres { get; set; }
    public Guid VoidedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ── Price Updated ──────────────────────────────────────────────────
public class FuelPriceUpdatedEvent : IntegrationEvent
{
    public Guid FuelTypeId { get; set; }
    public decimal NewPricePerLitre { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public Guid UpdatedByUserId { get; set; }
}

// ── Consumed BY Sales ──────────────────────────────────────────────
public class StockReservedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid TankId { get; set; }
    public decimal ReservedLitres { get; set; }
}

public class StockReservationFailedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid TankId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ── Audit ──────────────────────────────────────────────────────────
public class AuditEvent : IntegrationEvent
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string ServiceName { get; set; } = "SalesService";
}
