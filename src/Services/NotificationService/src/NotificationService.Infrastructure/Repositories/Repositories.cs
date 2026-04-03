using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Persistence;

namespace NotificationService.Infrastructure.Repositories;

public class NotificationLogRepository(NotificationDbContext ctx) : INotificationLogRepository
{
    public async Task<NotificationLog> AddAsync(NotificationLog log, CancellationToken ct)
    { await ctx.NotificationLogs.AddAsync(log, ct); await ctx.SaveChangesAsync(ct); return log; }

    public async Task UpdateAsync(NotificationLog log, CancellationToken ct)
    { ctx.NotificationLogs.Update(log); await ctx.SaveChangesAsync(ct); }

    public async Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.NotificationLogs.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<(IReadOnlyList<NotificationLog> Items, int Total)> GetByUserAsync(
        Guid userId, bool? isRead, int page, int pageSize, CancellationToken ct)
    {
        var q = ctx.NotificationLogs.Where(n => n.RecipientUserId == userId && n.Channel == NotificationChannel.InApp);
        if (isRead.HasValue) q = q.Where(n => n.IsRead == isRead.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<NotificationLog> Items, int Total)> GetLogsPagedAsync(int page, int pageSize, CancellationToken ct)
    {
        var total = await ctx.NotificationLogs.CountAsync(ct);
        var items = await ctx.NotificationLogs.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        await ctx.NotificationLogs.Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}

public class PriceAlertSubscriptionRepository(NotificationDbContext ctx) : IPriceAlertSubscriptionRepository
{
    public async Task<PriceAlertSubscription> AddAsync(PriceAlertSubscription sub, CancellationToken ct)
    { await ctx.PriceAlertSubscriptions.AddAsync(sub, ct); await ctx.SaveChangesAsync(ct); return sub; }

    public async Task<PriceAlertSubscription?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.PriceAlertSubscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<PriceAlertSubscription>> GetByUserAsync(Guid userId, CancellationToken ct)
        => await ctx.PriceAlertSubscriptions.Where(s => s.UserId == userId && s.IsActive).ToListAsync(ct);

    public async Task<IReadOnlyList<PriceAlertSubscription>> GetActiveByFuelTypeAsync(Guid fuelTypeId, CancellationToken ct)
        => await ctx.PriceAlertSubscriptions.Where(s => s.FuelTypeId == fuelTypeId && s.IsActive).ToListAsync(ct);

    public async Task RemoveAsync(PriceAlertSubscription sub, CancellationToken ct)
    { ctx.PriceAlertSubscriptions.Remove(sub); await ctx.SaveChangesAsync(ct); }
}

public class ProcessedEventRepository(NotificationDbContext ctx) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => await ctx.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    { await ctx.ProcessedEvents.AddAsync(new ProcessedEvent { Id = Guid.NewGuid(), EventId = eventId, EventType = eventType }, ct); await ctx.SaveChangesAsync(ct); }
}
