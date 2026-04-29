using Microsoft.EntityFrameworkCore;
using SalesService.Domain.Entities;
using SalesService.Domain.Enums;
using SalesService.Domain.Interfaces;
using SalesService.Infrastructure.Persistence;

namespace SalesService.Infrastructure.Repositories;

public class TransactionRepository(SalesDbContext ctx) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct)
        => await ctx.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Transaction> AddAsync(Transaction tx, CancellationToken ct)
    { await ctx.Transactions.AddAsync(tx, ct); await ctx.SaveChangesAsync(ct); return tx; }

    public async Task UpdateAsync(Transaction tx, CancellationToken ct)
    { ctx.Transactions.Update(tx); await ctx.SaveChangesAsync(ct); }

    public async Task<(IReadOnlyList<Transaction> Items, int Total)> GetPagedAsync(
        int page, int pageSize, Guid? stationId, Guid? dealerId, Guid? customerId,
        string? vehicleNumber, TransactionStatus? status, DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo, Guid? fuelTypeId, CancellationToken ct)
    {
        var q = ctx.Transactions.AsQueryable();
        if (stationId.HasValue) q = q.Where(t => t.StationId == stationId.Value);
        if (dealerId.HasValue) q = q.Where(t => t.DealerUserId == dealerId.Value);
        if (customerId.HasValue) q = q.Where(t => t.CustomerUserId == customerId.Value);
        if (!string.IsNullOrEmpty(vehicleNumber)) q = q.Where(t => t.VehicleNumber == vehicleNumber);
        if (status.HasValue) q = q.Where(t => t.Status == status.Value);
        if (dateFrom.HasValue) q = q.Where(t => t.Timestamp >= dateFrom.Value);
        if (dateTo.HasValue) q = q.Where(t => t.Timestamp <= dateTo.Value);
        if (fuelTypeId.HasValue) q = q.Where(t => t.FuelTypeId == fuelTypeId.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<int> GetDailySequenceAsync(Guid stationId, DateTimeOffset date, CancellationToken ct)
    {
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        return await ctx.Transactions.CountAsync(t => t.StationId == stationId && t.Timestamp >= dayStart && t.Timestamp < dayEnd, ct);
    }
}

public class PumpRepository(SalesDbContext ctx) : IPumpRepository
{
    public async Task<Pump?> GetByIdAsync(Guid id, CancellationToken ct) => await ctx.Pumps.FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task<IReadOnlyList<Pump>> GetByStationIdAsync(Guid stationId, CancellationToken ct)
        => await ctx.Pumps.Where(p => p.StationId == stationId).OrderBy(p => p.PumpName).ToListAsync(ct);
    public async Task<Pump> AddAsync(Pump pump, CancellationToken ct) { await ctx.Pumps.AddAsync(pump, ct); await ctx.SaveChangesAsync(ct); return pump; }
    public async Task UpdateAsync(Pump pump, CancellationToken ct) { ctx.Pumps.Update(pump); await ctx.SaveChangesAsync(ct); }
}

public class FuelPriceRepository(SalesDbContext ctx) : IFuelPriceRepository
{
    public async Task<FuelPrice?> GetActivePriceAsync(Guid fuelTypeId, CancellationToken ct)
        => await ctx.FuelPrices.Where(p => p.FuelTypeId == fuelTypeId && p.IsActive).OrderByDescending(p => p.EffectiveFrom).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<FuelPrice>> GetAllActiveAsync(CancellationToken ct)
        => await ctx.FuelPrices.Where(p => p.IsActive).ToListAsync(ct);
    public async Task<FuelPrice> AddAsync(FuelPrice price, CancellationToken ct) { await ctx.FuelPrices.AddAsync(price, ct); await ctx.SaveChangesAsync(ct); return price; }
    public async Task DeactivateAsync(Guid fuelTypeId, CancellationToken ct)
    {
        var actives = await ctx.FuelPrices.Where(p => p.FuelTypeId == fuelTypeId && p.IsActive).ToListAsync(ct);
        foreach (var p in actives) p.IsActive = false;
        await ctx.SaveChangesAsync(ct);
    }
}

public class ShiftRepository(SalesDbContext ctx) : IShiftRepository
{
    public async Task<Shift?> GetActiveShiftAsync(Guid dealerUserId, CancellationToken ct)
        => await ctx.Shifts.FirstOrDefaultAsync(s => s.DealerUserId == dealerUserId && s.EndedAt == null, ct);
    public async Task<Shift> AddAsync(Shift shift, CancellationToken ct) { await ctx.Shifts.AddAsync(shift, ct); await ctx.SaveChangesAsync(ct); return shift; }
    public async Task UpdateAsync(Shift shift, CancellationToken ct) { ctx.Shifts.Update(shift); await ctx.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<Shift>> GetByStationAsync(Guid stationId, int page, int pageSize, CancellationToken ct)
        => await ctx.Shifts.Where(s => s.StationId == stationId).OrderByDescending(s => s.StartedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
}

public class VoidedTransactionRepository(SalesDbContext ctx) : IVoidedTransactionRepository
{
    public async Task<VoidedTransaction> AddAsync(VoidedTransaction vt, CancellationToken ct) { await ctx.VoidedTransactions.AddAsync(vt, ct); await ctx.SaveChangesAsync(ct); return vt; }
}

public class RegisteredVehicleRepository(SalesDbContext ctx) : IRegisteredVehicleRepository
{
    public async Task<RegisteredVehicle?> GetByIdAsync(Guid id, CancellationToken ct) => await ctx.RegisteredVehicles.FirstOrDefaultAsync(v => v.Id == id, ct);
    public async Task<RegisteredVehicle?> GetByRegistrationAsync(string regNumber, CancellationToken ct)
        => await ctx.RegisteredVehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == regNumber, ct);
    public async Task<IReadOnlyList<RegisteredVehicle>> GetByCustomerAsync(Guid customerId, CancellationToken ct)
        => await ctx.RegisteredVehicles.Where(v => v.CustomerId == customerId && v.IsActive).ToListAsync(ct);
    public async Task<RegisteredVehicle> AddAsync(RegisteredVehicle v, CancellationToken ct) { await ctx.RegisteredVehicles.AddAsync(v, ct); await ctx.SaveChangesAsync(ct); return v; }
}

public class FleetAccountRepository(SalesDbContext ctx) : IFleetAccountRepository
{
    public async Task<FleetAccount?> GetByIdAsync(Guid id, CancellationToken ct) => await ctx.FleetAccounts.FirstOrDefaultAsync(f => f.Id == id, ct);
    public async Task<IReadOnlyList<FleetAccount>> GetAllAsync(CancellationToken ct) => await ctx.FleetAccounts.Where(f => f.IsActive).ToListAsync(ct);
    public async Task<FleetAccount> AddAsync(FleetAccount fa, CancellationToken ct) { await ctx.FleetAccounts.AddAsync(fa, ct); await ctx.SaveChangesAsync(ct); return fa; }
    public async Task UpdateAsync(FleetAccount fa, CancellationToken ct) { ctx.FleetAccounts.Update(fa); await ctx.SaveChangesAsync(ct); }
}

public class FleetVehicleRepository(SalesDbContext ctx) : IFleetVehicleRepository
{
    public async Task<FleetVehicle> AddAsync(FleetVehicle fv, CancellationToken ct) { await ctx.FleetVehicles.AddAsync(fv, ct); await ctx.SaveChangesAsync(ct); return fv; }
    public async Task<FleetVehicle?> GetByIdAsync(Guid id, CancellationToken ct) => await ctx.FleetVehicles.FirstOrDefaultAsync(f => f.Id == id, ct);
    public async Task RemoveAsync(FleetVehicle fv, CancellationToken ct) { ctx.FleetVehicles.Remove(fv); await ctx.SaveChangesAsync(ct); }
    public async Task<IReadOnlyList<FleetVehicle>> GetByAccountAsync(Guid accountId, CancellationToken ct)
        => await ctx.FleetVehicles.Where(f => f.FleetAccountId == accountId && f.IsActive).ToListAsync(ct);
}

public class CustomerWalletRepository(SalesDbContext ctx) : ICustomerWalletRepository
{
    public async Task<CustomerWallet?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct)
        => await ctx.CustomerWallets.FirstOrDefaultAsync(w => w.CustomerId == customerId, ct);
    public async Task<CustomerWallet> AddAsync(CustomerWallet w, CancellationToken ct) { await ctx.CustomerWallets.AddAsync(w, ct); await ctx.SaveChangesAsync(ct); return w; }
    public async Task UpdateAsync(CustomerWallet w, CancellationToken ct) { ctx.CustomerWallets.Update(w); await ctx.SaveChangesAsync(ct); }
}

public class WalletTransactionRepository(SalesDbContext ctx) : IWalletTransactionRepository
{
    public async Task<WalletTransaction> AddAsync(WalletTransaction wt, CancellationToken ct) { await ctx.WalletTransactions.AddAsync(wt, ct); await ctx.SaveChangesAsync(ct); return wt; }
    public async Task UpdateAsync(WalletTransaction wt, CancellationToken ct) { ctx.WalletTransactions.Update(wt); await ctx.SaveChangesAsync(ct); }
    public async Task<WalletTransaction?> GetByRazorpayOrderIdAsync(string orderId, CancellationToken ct)
        => await ctx.WalletTransactions.FirstOrDefaultAsync(w => w.RazorpayOrderId == orderId, ct);
    public async Task<IReadOnlyList<WalletTransaction>> GetByWalletIdAsync(Guid walletId, int page, int pageSize, CancellationToken ct)
        => await ctx.WalletTransactions.Where(w => w.WalletId == walletId).OrderByDescending(w => w.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
}

public class ProcessedEventRepository(SalesDbContext ctx) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => await ctx.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);
    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    { await ctx.ProcessedEvents.AddAsync(new ProcessedEvent { Id = Guid.NewGuid(), EventId = eventId, EventType = eventType }, ct); await ctx.SaveChangesAsync(ct); }
}
