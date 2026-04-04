namespace AuditService.Application.DTOs;

public record AuditLogDto(
    Guid Id, Guid EventId, string EntityType, Guid EntityId, string Operation,
    string? OldValues, string? NewValues, Guid? ChangedByUserId, string? ChangedByRole,
    string? IpAddress, string? CorrelationId, string ServiceName, DateTimeOffset Timestamp);

public record MessageResponseDto(string Message);

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
