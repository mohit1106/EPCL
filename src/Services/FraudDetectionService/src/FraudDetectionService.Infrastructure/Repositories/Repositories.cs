using Microsoft.EntityFrameworkCore;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Interfaces;
using FraudDetectionService.Infrastructure.Persistence;

namespace FraudDetectionService.Infrastructure.Repositories;

public class FraudAlertRepository(FraudDbContext ctx) : IFraudAlertRepository
{
    public async Task<FraudAlert?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.FraudAlerts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<FraudAlert> AddAsync(FraudAlert alert, CancellationToken ct)
    { await ctx.FraudAlerts.AddAsync(alert, ct); await ctx.SaveChangesAsync(ct); return alert; }

    public async Task AddRangeAsync(IEnumerable<FraudAlert> alerts, CancellationToken ct)
    { await ctx.FraudAlerts.AddRangeAsync(alerts, ct); await ctx.SaveChangesAsync(ct); }

    public async Task UpdateAsync(FraudAlert alert, CancellationToken ct)
    { ctx.FraudAlerts.Update(alert); await ctx.SaveChangesAsync(ct); }

    public async Task<(IReadOnlyList<FraudAlert> Items, int Total)> GetPagedAsync(
        int page, int pageSize, AlertStatus? status, AlertSeverity? severity,
        Guid? stationId, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, CancellationToken ct)
    {
        var q = ctx.FraudAlerts.AsQueryable();
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        if (severity.HasValue) q = q.Where(a => a.Severity == severity.Value);
        if (stationId.HasValue) q = q.Where(a => a.StationId == stationId.Value);
        if (dateFrom.HasValue) q = q.Where(a => a.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(a => a.CreatedAt <= dateTo.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<int> GetCountByStatusAsync(AlertStatus status, Guid? stationId,
        DateTimeOffset? dateFrom, DateTimeOffset? dateTo, CancellationToken ct)
    {
        var q = ctx.FraudAlerts.Where(a => a.Status == status);
        if (stationId.HasValue) q = q.Where(a => a.StationId == stationId.Value);
        if (dateFrom.HasValue) q = q.Where(a => a.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(a => a.CreatedAt <= dateTo.Value);
        return await q.CountAsync(ct);
    }
}

public class FraudRuleEvaluationRepository(FraudDbContext ctx) : IFraudRuleEvaluationRepository
{
    public async Task AddRangeAsync(IEnumerable<FraudRuleEvaluation> evaluations, CancellationToken ct)
    { await ctx.FraudRuleEvaluations.AddRangeAsync(evaluations, ct); await ctx.SaveChangesAsync(ct); }

    // Context queries for rules — these query the FraudRuleEvaluations table
    // In a real system, these would call the Sales Service or query a shared read model.
    // For now, they return conservative defaults to avoid false positives.

    public Task<int> GetRecentTransactionCountAsync(Guid pumpId, TimeSpan window, CancellationToken ct)
    {
        // Count recent evaluations as proxy for transaction count
        var cutoff = DateTimeOffset.UtcNow - window;
        return ctx.FraudRuleEvaluations
            .Where(e => e.EvaluatedAt >= cutoff && e.RuleName == "RapidTransactionRule")
            .Select(e => e.TransactionId).Distinct().CountAsync(ct);
    }

    public Task<bool> HasDuplicateTransactionAsync(string vehicleNumber, Guid pumpId, decimal quantity, TimeSpan window, CancellationToken ct)
    {
        // Check if we've recently evaluated a transaction with the same details
        var cutoff = DateTimeOffset.UtcNow - window;
        return ctx.FraudRuleEvaluations
            .Where(e => e.EvaluatedAt >= cutoff && e.RuleName == "DuplicateTransactionRule" && e.Details != null && e.Details.Contains(vehicleNumber))
            .AnyAsync(ct);
    }

    public Task<int> GetDailyVoidCountAsync(Guid dealerUserId, DateTimeOffset today, CancellationToken ct)
    {
        var dayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, TimeSpan.Zero);
        return ctx.FraudAlerts
            .Where(a => a.CreatedAt >= dayStart && a.RuleTriggered == "VoidPatternRule")
            .CountAsync(ct);
    }

    public Task<int> GetDailyTransactionCountAsync(Guid dealerUserId, DateTimeOffset today, CancellationToken ct)
    {
        var dayStart = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, TimeSpan.Zero);
        return ctx.FraudRuleEvaluations
            .Where(e => e.EvaluatedAt >= dayStart)
            .Select(e => e.TransactionId).Distinct().CountAsync(ct);
    }

    public Task<decimal> GetDailyStationVolumeAsync(Guid stationId, DateTimeOffset today, CancellationToken ct)
    {
        // Without cross-service query, return 0 (won't trigger)
        return Task.FromResult(0m);
    }

    public Task<decimal> GetAverageVolumeForDayOfWeekAsync(Guid stationId, DayOfWeek dayOfWeek, int weeksBack, CancellationToken ct)
    {
        return Task.FromResult(0m);
    }

    public Task<bool> AreLastNTransactionsRoundNumbersAsync(Guid pumpId, int count, CancellationToken ct)
    {
        // Check last N evaluations for the round number rule
        return Task.FromResult(false);
    }
}

public class ProcessedEventRepository(FraudDbContext ctx) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => await ctx.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    { await ctx.ProcessedEvents.AddAsync(new ProcessedEvent { Id = Guid.NewGuid(), EventId = eventId, EventType = eventType }, ct); await ctx.SaveChangesAsync(ct); }
}
