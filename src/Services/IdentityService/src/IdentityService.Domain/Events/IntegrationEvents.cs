namespace IdentityService.Domain.Events;

public abstract class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserAccountLockedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset? LockoutEnd { get; set; }
    public int FailedAttempts { get; set; }
}

public class UserRoleChangedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string OldRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
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
    public string ServiceName { get; set; } = "IdentityService";
}
