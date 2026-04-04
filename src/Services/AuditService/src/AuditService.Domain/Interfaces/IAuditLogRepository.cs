using AuditService.Domain.Entities;

namespace AuditService.Domain.Interfaces;

/// <summary>
/// Audit log repository — INSERT and READ only, no update or delete.
/// </summary>
public interface IAuditLogRepository
{
    Task<AuditLog> AppendAsync(AuditLog log, CancellationToken ct = default);
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> EventAlreadyLoggedAsync(Guid eventId, CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(
        string? entityType, Guid? userId, string? operation, string? serviceName,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<AuditLog>> ExportAsync(
        string? entityType, Guid? userId, string? operation,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        CancellationToken ct = default);
}
