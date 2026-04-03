namespace FraudDetectionService.Application.DTOs;

public record FraudAlertDto(
    Guid Id, Guid TransactionId, Guid StationId, string RuleTriggered,
    string Severity, string Description, string Status,
    Guid? ReviewedByUserId, DateTimeOffset? ReviewedAt, string? ReviewNotes,
    DateTimeOffset CreatedAt);

public record FraudStatsDto(int TotalAlerts, int OpenAlerts, int UnderReview, int Dismissed, int Escalated,
    int HighSeverity, int MediumSeverity, int LowSeverity);

public record MessageResponseDto(string Message);

public record DismissAlertRequest(string? Notes);
public record EscalateAlertRequest(string? Notes);
public record BulkDismissRequest(Guid[] AlertIds, string? Notes);

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
