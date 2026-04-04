namespace AuditService.Domain.Events;

/// <summary>
/// Integration event consumed from any service via audit.# routing key.
/// All services publish audit events when they modify entities.
/// </summary>
public class AuditEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? ChangedByRole { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
