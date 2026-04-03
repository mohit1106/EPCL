namespace FraudDetectionService.Domain.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
}

/// <summary>Consumed from Sales Service.</summary>
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

/// <summary>Consumed from Inventory Service.</summary>
public class DipVarianceDetectedEvent : IntegrationEvent
{
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal PhysicalDipLitres { get; set; }
    public decimal SystemStockLitres { get; set; }
    public decimal VariancePercent { get; set; }
}

/// <summary>Published by Fraud Service.</summary>
public class FraudAlertTriggeredEvent : IntegrationEvent
{
    public Guid AlertId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public string RuleTriggered { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
