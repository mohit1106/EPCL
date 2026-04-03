namespace FraudDetectionService.Domain.Enums;

public enum AlertSeverity
{
    Low,
    Medium,
    High
}

public enum AlertStatus
{
    Open,
    UnderReview,
    Dismissed,
    Escalated
}
