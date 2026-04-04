using MediatR;
using AuditService.Application.DTOs;
using AuditService.Domain.Entities;
using AuditService.Domain.Exceptions;
using AuditService.Domain.Interfaces;

namespace AuditService.Application.Queries;

// ══════════════════════════════════════════════════════════════════
// GetAuditLogs — paginated + filtered
// ══════════════════════════════════════════════════════════════════
public record GetAuditLogsQuery(
    string? EntityType = null, Guid? UserId = null, string? Operation = null,
    string? ServiceName = null,
    DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<AuditLogDto>>;

public class GetAuditLogsHandler(IAuditLogRepository repo)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            q.EntityType, q.UserId, q.Operation, q.ServiceName,
            q.DateFrom, q.DateTo, q.Page, q.PageSize, ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }
    private static AuditLogDto MapToDto(AuditLog l) => new(
        l.Id, l.EventId, l.EntityType, l.EntityId, l.Operation,
        l.OldValues, l.NewValues, l.ChangedByUserId, l.ChangedByRole,
        l.IpAddress, l.CorrelationId, l.ServiceName, l.Timestamp);
}

// ══════════════════════════════════════════════════════════════════
// GetAuditLogById
// ══════════════════════════════════════════════════════════════════
public record GetAuditLogByIdQuery(Guid Id) : IRequest<AuditLogDto>;

public class GetAuditLogByIdHandler(IAuditLogRepository repo)
    : IRequestHandler<GetAuditLogByIdQuery, AuditLogDto>
{
    public async Task<AuditLogDto> Handle(GetAuditLogByIdQuery q, CancellationToken ct)
    {
        var log = await repo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException("AuditLog", q.Id);
        return new AuditLogDto(log.Id, log.EventId, log.EntityType, log.EntityId, log.Operation,
            log.OldValues, log.NewValues, log.ChangedByUserId, log.ChangedByRole,
            log.IpAddress, log.CorrelationId, log.ServiceName, log.Timestamp);
    }
}

// ══════════════════════════════════════════════════════════════════
// ExportAuditLog — returns all matching records (no pagination)
// ══════════════════════════════════════════════════════════════════
public record ExportAuditLogQuery(
    string? EntityType = null, Guid? UserId = null, string? Operation = null,
    DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null)
    : IRequest<IReadOnlyList<AuditLogDto>>;

public class ExportAuditLogHandler(IAuditLogRepository repo)
    : IRequestHandler<ExportAuditLogQuery, IReadOnlyList<AuditLogDto>>
{
    public async Task<IReadOnlyList<AuditLogDto>> Handle(ExportAuditLogQuery q, CancellationToken ct)
    {
        var items = await repo.ExportAsync(q.EntityType, q.UserId, q.Operation, q.DateFrom, q.DateTo, ct);
        return items.Select(l => new AuditLogDto(
            l.Id, l.EventId, l.EntityType, l.EntityId, l.Operation,
            l.OldValues, l.NewValues, l.ChangedByUserId, l.ChangedByRole,
            l.IpAddress, l.CorrelationId, l.ServiceName, l.Timestamp)).ToList();
    }
}
