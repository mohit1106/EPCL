using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Interfaces;
using InventoryService.Infrastructure.Persistence;

namespace InventoryService.Infrastructure.Repositories;

public class TankRepository(InventoryDbContext ctx) : ITankRepository
{
    public async Task<Tank?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.Tanks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Tank>> GetByStationIdAsync(Guid stationId, CancellationToken ct)
        => await ctx.Tanks.Where(t => t.StationId == stationId).OrderBy(t => t.TankSerialNumber).ToListAsync(ct);

    public async Task<Tank> AddAsync(Tank tank, CancellationToken ct)
    { await ctx.Tanks.AddAsync(tank, ct); await ctx.SaveChangesAsync(ct); return tank; }

    public async Task UpdateAsync(Tank tank, CancellationToken ct)
    { ctx.Tanks.Update(tank); await ctx.SaveChangesAsync(ct); }

    public async Task<bool> ExistsBySerialAsync(string serial, CancellationToken ct)
        => await ctx.Tanks.AnyAsync(t => t.TankSerialNumber == serial, ct);

    public async Task<IReadOnlyList<Tank>> GetLowStockTanksAsync(CancellationToken ct)
        => await ctx.Tanks.Where(t =>
            t.Status == TankStatus.Low || t.Status == TankStatus.Critical || t.Status == TankStatus.OutOfStock)
            .OrderBy(t => t.CurrentStockLitres).ToListAsync(ct);

    public async Task<IReadOnlyList<Tank>> GetAllAsync(CancellationToken ct)
        => await ctx.Tanks.ToListAsync(ct);
}

public class StockLoadingRepository(InventoryDbContext ctx) : IStockLoadingRepository
{
    public async Task<StockLoading> AddAsync(StockLoading loading, CancellationToken ct)
    { await ctx.StockLoadings.AddAsync(loading, ct); await ctx.SaveChangesAsync(ct); return loading; }

    public async Task<IReadOnlyList<StockLoading>> GetByTankIdAsync(Guid tankId, int page, int pageSize, CancellationToken ct)
        => await ctx.StockLoadings.Where(s => s.TankId == tankId)
            .OrderByDescending(s => s.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
}

public class DipReadingRepository(InventoryDbContext ctx) : IDipReadingRepository
{
    public async Task<DipReading> AddAsync(DipReading reading, CancellationToken ct)
    { await ctx.DipReadings.AddAsync(reading, ct); await ctx.SaveChangesAsync(ct); return reading; }

    public async Task<IReadOnlyList<DipReading>> GetByTankIdAsync(Guid tankId, int page, int pageSize, CancellationToken ct)
        => await ctx.DipReadings.Where(d => d.TankId == tankId)
            .OrderByDescending(d => d.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
}

public class ReplenishmentRequestRepository(InventoryDbContext ctx) : IReplenishmentRequestRepository
{
    public async Task<ReplenishmentRequest?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.ReplenishmentRequests.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<ReplenishmentRequest> AddAsync(ReplenishmentRequest req, CancellationToken ct)
    { await ctx.ReplenishmentRequests.AddAsync(req, ct); await ctx.SaveChangesAsync(ct); return req; }

    public async Task UpdateAsync(ReplenishmentRequest req, CancellationToken ct)
    { ctx.ReplenishmentRequests.Update(req); await ctx.SaveChangesAsync(ct); }

    public async Task<(IReadOnlyList<ReplenishmentRequest> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, ReplenishmentStatus? status, Guid? stationId, CancellationToken ct)
    {
        var q = ctx.ReplenishmentRequests.AsQueryable();
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);
        if (stationId.HasValue) q = q.Where(r => r.StationId == stationId.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}

public class ProcessedEventRepository(InventoryDbContext ctx) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => await ctx.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    {
        await ctx.ProcessedEvents.AddAsync(new ProcessedEvent
        { Id = Guid.NewGuid(), EventId = eventId, EventType = eventType }, ct);
        await ctx.SaveChangesAsync(ct);
    }
}
