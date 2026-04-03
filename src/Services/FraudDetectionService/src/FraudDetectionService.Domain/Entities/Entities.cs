using FraudDetectionService.Domain.Enums;

namespace FraudDetectionService.Domain.Entities;

public class FraudAlert
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public string RuleTriggered { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public AlertStatus Status { get; set; } = AlertStatus.Open;
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class FraudRuleEvaluation
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Details { get; set; }
}

public class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
