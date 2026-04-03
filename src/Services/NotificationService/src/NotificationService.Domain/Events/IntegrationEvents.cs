namespace NotificationService.Domain.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
}

public class SaleCompletedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid PumpId { get; set; }
    public Guid FuelTypeId { get; set; }
    public Guid DealerUserId { get; set; }
    public Guid? CustomerUserId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public decimal QuantityLitres { get; set; }
    public decimal PricePerLitre { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class FraudAlertTriggeredEvent : IntegrationEvent
{
    public Guid AlertId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public string RuleTriggered { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class FuelPriceUpdatedEvent : IntegrationEvent
{
    public Guid FuelTypeId { get; set; }
    public decimal NewPricePerLitre { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public Guid UpdatedByUserId { get; set; }
}

public class StockLevelLowEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public string TankName { get; set; } = string.Empty;
    public decimal CurrentStockLitres { get; set; }
    public decimal ThresholdLitres { get; set; }
    public decimal CapacityLitres { get; set; }
    public Guid DealerUserId { get; set; }
}

public class StockLevelCriticalEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public string TankName { get; set; } = string.Empty;
    public decimal CurrentStockLitres { get; set; }
    public decimal ThresholdLitres { get; set; }
    public Guid DealerUserId { get; set; }
}

public class ReplenishmentRequestedEvent : IntegrationEvent
{
    public Guid RequestId { get; set; }
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal RequestedQuantityLitres { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty;
}

public class ReplenishmentApprovedEvent : IntegrationEvent
{
    public Guid RequestId { get; set; }
    public Guid StationId { get; set; }
    public Guid DealerUserId { get; set; }
    public decimal ApprovedQuantityLitres { get; set; }
    public string TankName { get; set; } = string.Empty;
}

public class DipVarianceDetectedEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal PhysicalDipLitres { get; set; }
    public decimal SystemStockLitres { get; set; }
}

public class UserAccountLockedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class TransactionVoidedEvent : IntegrationEvent
{
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
    public decimal QuantityLitres { get; set; }
    public Guid VoidedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
