namespace AuditService.Domain.Entities;

/// <summary>
/// Append-only audit log entry. NO UPDATE OR DELETE operations are allowed on this entity — ever.
/// Every state change across the platform is recorded here for compliance and traceability.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>RabbitMQ EventId for idempotent deduplication.</summary>
    public Guid EventId { get; set; }

    /// <summary>Entity type that was changed (e.g. Transaction, Tank, User, Station).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID of the changed entity.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Operation performed: Create, Update, Delete.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>JSON snapshot of entity BEFORE the change (null for Create).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of entity AFTER the change (null for Delete).</summary>
    public string? NewValues { get; set; }

    /// <summary>User who triggered the change.</summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>Role of the user who triggered the change.</summary>
    public string? ChangedByRole { get; set; }

    /// <summary>IP address of the requestor.</summary>
    public string? IpAddress { get; set; }

    /// <summary>X-Correlation-ID for request tracing.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Name of the publishing microservice.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Exact UTC timestamp of the change.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
