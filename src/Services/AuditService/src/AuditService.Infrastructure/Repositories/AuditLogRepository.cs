using Microsoft.EntityFrameworkCore;
using AuditService.Domain.Entities;
using AuditService.Domain.Interfaces;
using AuditService.Infrastructure.Persistence;

namespace AuditService.Infrastructure.Repositories;

/// <summary>
/// Audit log repository — INSERT + READ only. No update or delete methods exist by design.
/// </summary>
public class AuditLogRepository(AuditDbContext ctx) : IAuditLogRepository
{
    public async Task<AuditLog> AppendAsync(AuditLog log, CancellationToken ct)
    {
        await ctx.AuditLogs.AddAsync(log, ct);
        await ctx.SaveChangesAsync(ct);
        return log;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> EventAlreadyLoggedAsync(Guid eventId, CancellationToken ct)
        => await ctx.AuditLogs.AnyAsync(a => a.EventId == eventId, ct);

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(
        string? entityType, Guid? userId, string? operation, string? serviceName,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo,
        int page, int pageSize, CancellationToken ct)
    {
        var q = BuildQuery(entityType, userId, operation, serviceName, dateFrom, dateTo);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .AsNoTracking().ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<AuditLog>> ExportAsync(
        string? entityType, Guid? userId, string? operation,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo, CancellationToken ct)
    {
        return await BuildQuery(entityType, userId, operation, null, dateFrom, dateTo)
            .OrderByDescending(a => a.Timestamp)
            .AsNoTracking().ToListAsync(ct);
    }

    private IQueryable<AuditLog> BuildQuery(
        string? entityType, Guid? userId, string? operation, string? serviceName,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo)
    {
        IQueryable<AuditLog> q = ctx.AuditLogs;
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (userId.HasValue) q = q.Where(a => a.ChangedByUserId == userId);
        if (!string.IsNullOrWhiteSpace(operation)) q = q.Where(a => a.Operation == operation);
        if (!string.IsNullOrWhiteSpace(serviceName)) q = q.Where(a => a.ServiceName == serviceName);
        if (dateFrom.HasValue) q = q.Where(a => a.Timestamp >= dateFrom);
        if (dateTo.HasValue) q = q.Where(a => a.Timestamp <= dateTo);
        return q;
    }
}
