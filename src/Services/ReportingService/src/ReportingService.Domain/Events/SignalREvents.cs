namespace ReportingService.Domain.Events;

/// <summary>Fraud alert triggered — pushed to AdminHub.</summary>
public class FraudAlertTriggeredEvent
{
    public Guid EventId { get; set; }
    public Guid AlertId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>Stock level critical — pushed to AdminHub + DealerHub station group.</summary>
public class StockLevelCriticalEvent
{
    public Guid EventId { get; set; }
    public Guid TankId { get; set; }
    public Guid StationId { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal CapacityLitres { get; set; }
    public Guid FuelTypeId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>Replenishment requested — pushed to AdminHub.</summary>
public class ReplenishmentRequestedEvent
{
    public Guid EventId { get; set; }
    public Guid RequestId { get; set; }
    public Guid StationId { get; set; }
    public Guid TankId { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>Fuel price updated — pushed to all DealerHub groups.</summary>
public class FuelPriceUpdatedEvent
{
    public Guid EventId { get; set; }
    public Guid FuelTypeId { get; set; }
    public string FuelTypeName { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
