using Microsoft.EntityFrameworkCore;
using ReportingService.Domain.Entities;
using ReportingService.Domain.Interfaces;
using ReportingService.Infrastructure.Persistence;

namespace ReportingService.Infrastructure.Repositories;

public class DailySalesSummaryRepository(ReportingDbContext ctx) : IDailySalesSummaryRepository
{
    public async Task UpsertAsync(Guid stationId, Guid fuelTypeId, DateOnly date,
        decimal litres, decimal revenue, CancellationToken ct)
    {
        var existing = await ctx.DailySalesSummaries
            .FirstOrDefaultAsync(d => d.StationId == stationId && d.FuelTypeId == fuelTypeId && d.Date == date, ct);

        if (existing != null)
        {
            existing.TotalTransactions++;
            existing.TotalLitresSold += litres;
            existing.TotalRevenue += revenue;
            existing.LastUpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            await ctx.DailySalesSummaries.AddAsync(new DailySalesSummary
            {
                Id = Guid.NewGuid(), StationId = stationId, FuelTypeId = fuelTypeId,
                Date = date, TotalTransactions = 1, TotalLitresSold = litres,
                TotalRevenue = revenue, LastUpdatedAt = DateTimeOffset.UtcNow
            }, ct);
        }
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DailySalesSummary>> GetAsync(
        Guid? stationId, Guid? fuelTypeId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct)
    {
        IQueryable<DailySalesSummary> q = ctx.DailySalesSummaries;
        if (stationId.HasValue) q = q.Where(d => d.StationId == stationId);
        if (fuelTypeId.HasValue) q = q.Where(d => d.FuelTypeId == fuelTypeId);
        if (dateFrom.HasValue) q = q.Where(d => d.Date >= dateFrom);
        if (dateTo.HasValue) q = q.Where(d => d.Date <= dateTo);
        return await q.OrderByDescending(d => d.Date).AsNoTracking().ToListAsync(ct);
    }
}

public class MonthlyStationReportRepository(ReportingDbContext ctx) : IMonthlyStationReportRepository
{
    public async Task<IReadOnlyList<MonthlyStationReport>> GetAsync(
        Guid? stationId, int? year, int? month, CancellationToken ct)
    {
        IQueryable<MonthlyStationReport> q = ctx.MonthlyStationReports;
        if (stationId.HasValue) q = q.Where(m => m.StationId == stationId);
        if (year.HasValue) q = q.Where(m => m.Year == year);
        if (month.HasValue) q = q.Where(m => m.Month == month);
        return await q.OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).AsNoTracking().ToListAsync(ct);
    }
}

public class GeneratedReportRepository(ReportingDbContext ctx) : IGeneratedReportRepository
{
    public async Task<GeneratedReport> AddAsync(GeneratedReport report, CancellationToken ct)
    { await ctx.GeneratedReports.AddAsync(report, ct); await ctx.SaveChangesAsync(ct); return report; }

    public async Task UpdateAsync(GeneratedReport report, CancellationToken ct)
    { ctx.GeneratedReports.Update(report); await ctx.SaveChangesAsync(ct); }

    public async Task<GeneratedReport?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.GeneratedReports.FirstOrDefaultAsync(r => r.Id == id, ct);
}

public class ScheduledReportRepository(ReportingDbContext ctx) : IScheduledReportRepository
{
    public async Task<ScheduledReport> AddAsync(ScheduledReport sched, CancellationToken ct)
    { await ctx.ScheduledReports.AddAsync(sched, ct); await ctx.SaveChangesAsync(ct); return sched; }

    public async Task<IReadOnlyList<ScheduledReport>> GetAllActiveAsync(CancellationToken ct)
        => await ctx.ScheduledReports.Where(s => s.IsActive).AsNoTracking().ToListAsync(ct);

    public async Task<ScheduledReport?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.ScheduledReports.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task RemoveAsync(ScheduledReport sched, CancellationToken ct)
    { ctx.ScheduledReports.Remove(sched); await ctx.SaveChangesAsync(ct); }
}

public class ProcessedEventRepository(ReportingDbContext ctx) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => await ctx.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    { await ctx.ProcessedEvents.AddAsync(new ProcessedEvent { Id = Guid.NewGuid(), EventId = eventId, EventType = eventType }, ct); await ctx.SaveChangesAsync(ct); }
}

public class StockPredictionRepository(ReportingDbContext ctx) : IStockPredictionRepository
{
    public async Task UpsertAsync(StockPrediction prediction, CancellationToken ct = default)
    {
        var existing = await ctx.StockPredictions.FirstOrDefaultAsync(s => s.TankId == prediction.TankId, ct);
        if (existing != null)
        {
            existing.AvgDailyConsumptionL = prediction.AvgDailyConsumptionL;
            existing.CurrentStockLitres = prediction.CurrentStockLitres;
            existing.PredictedEmptyAt = prediction.PredictedEmptyAt;
            existing.DaysUntilEmpty = prediction.DaysUntilEmpty;
            existing.CalculatedAt = prediction.CalculatedAt;
            existing.DataPointsUsed = prediction.DataPointsUsed;
        }
        else
        {
            await ctx.StockPredictions.AddAsync(prediction, ct);
        }
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<StockPrediction?> GetByTankIdAsync(Guid tankId, CancellationToken ct = default)
    {
        return await ctx.StockPredictions.FirstOrDefaultAsync(s => s.TankId == tankId, ct);
    }

    public async Task<IReadOnlyList<StockPrediction>> GetAllAsync(Guid? stationId = null, CancellationToken ct = default)
    {
        IQueryable<StockPrediction> q = ctx.StockPredictions;
        if (stationId.HasValue) q = q.Where(s => s.StationId == stationId.Value);
        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockPrediction>> GetAtRiskAsync(int daysThreshold = 7, Guid? stationId = null, CancellationToken ct = default)
    {
        IQueryable<StockPrediction> q = ctx.StockPredictions.Where(s => s.DaysUntilEmpty <= daysThreshold);
        if (stationId.HasValue) q = q.Where(s => s.StationId == stationId.Value);
        return await q.OrderBy(s => s.DaysUntilEmpty).AsNoTracking().ToListAsync(ct);
    }

    public async Task MarkAlertSentAsync(Guid tankId, CancellationToken ct = default)
    {
        var existing = await ctx.StockPredictions.FirstOrDefaultAsync(s => s.TankId == tankId, ct);
        if (existing != null)
        {
            existing.AlertSentAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync(ct);
        }
    }
}
