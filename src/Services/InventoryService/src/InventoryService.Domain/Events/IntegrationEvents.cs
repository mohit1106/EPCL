namespace InventoryService.Domain.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
}

// ── Published BY Inventory Service ──────────────────────────────────

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

public class FuelStockLoadedEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal QuantityLoaded { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}

public class StockLevelLowEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal Threshold { get; set; }
    public Guid FuelTypeId { get; set; }
}

public class StockLevelCriticalEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal CapacityLitres { get; set; }
}

public class StockOutOfFuelEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
}

public class DipVarianceDetectedEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal DipValueLitres { get; set; }
    public decimal SystemStockLitres { get; set; }
}

public class ReplenishmentRequestedEvent : IntegrationEvent
{
    public Guid RequestId { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
}

public class ReplenishmentApprovedEvent : IntegrationEvent
{
    public Guid RequestId { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public Guid ApprovedByUserId { get; set; }
}

// ── Consumed BY Inventory Service ───────────────────────────────────

public class SaleInitiatedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal QuantityLitres { get; set; }
    public Guid DealerUserId { get; set; }
}

public class SaleCompletedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal QuantityLitres { get; set; }
}

public class SaleCancelledEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid TankId { get; set; }
    public decimal ReservedLitres { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AuditEvent : IntegrationEvent
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string ServiceName { get; set; } = "InventoryService";
}
