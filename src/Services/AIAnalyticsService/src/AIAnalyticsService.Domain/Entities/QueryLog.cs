namespace AIAnalyticsService.Domain.Entities;

/// <summary>
/// Tracks every AI query for monitoring, cost analysis, and auditing.
/// Stored in the QueryLog table.
/// </summary>
public class QueryLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string? GeneratedSql { get; set; }
    public bool WasSqlValid { get; set; }
    public int? RowsReturned { get; set; }
    public int? GeminiTokensUsed { get; set; }
    public int TotalMs { get; set; }
    public bool WasSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
