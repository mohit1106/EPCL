namespace StationService.Domain.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class StationCreatedEvent : IntegrationEvent
{
    public Guid StationId { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string StationName { get; set; } = string.Empty;
    public Guid DealerUserId { get; set; }
}

public class StationUpdatedEvent : IntegrationEvent
{
    public Guid StationId { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public Guid? ChangedByUserId { get; set; }
}

public class StationDeactivatedEvent : IntegrationEvent
{
    public Guid StationId { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public Guid DeactivatedByUserId { get; set; }
}

public class AuditEvent : IntegrationEvent
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByRole { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string ServiceName { get; set; } = "StationService";
}
