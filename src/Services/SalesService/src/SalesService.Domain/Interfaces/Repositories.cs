using SalesService.Domain.Entities;
using SalesService.Domain.Enums;

namespace SalesService.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Transaction> AddAsync(Transaction tx, CancellationToken ct = default);
    Task UpdateAsync(Transaction tx, CancellationToken ct = default);
    Task<(IReadOnlyList<Transaction> Items, int Total)> GetPagedAsync(
        int page, int pageSize, Guid? stationId = null, Guid? dealerId = null,
        Guid? customerId = null, string? vehicleNumber = null,
        TransactionStatus? status = null, DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null, Guid? fuelTypeId = null,
        CancellationToken ct = default);
    Task<int> GetDailySequenceAsync(Guid stationId, DateTimeOffset date, CancellationToken ct = default);
}

public interface IPumpRepository
{
    Task<Pump?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Pump>> GetByStationIdAsync(Guid stationId, CancellationToken ct = default);
    Task<Pump> AddAsync(Pump pump, CancellationToken ct = default);
    Task UpdateAsync(Pump pump, CancellationToken ct = default);
}

public interface IFuelPriceRepository
{
    Task<FuelPrice?> GetActivePriceAsync(Guid fuelTypeId, CancellationToken ct = default);
    Task<IReadOnlyList<FuelPrice>> GetAllActiveAsync(CancellationToken ct = default);
    Task<FuelPrice> AddAsync(FuelPrice price, CancellationToken ct = default);
    Task DeactivateAsync(Guid fuelTypeId, CancellationToken ct = default);
}

public interface IShiftRepository
{
    Task<Shift?> GetActiveShiftAsync(Guid dealerUserId, CancellationToken ct = default);
    Task<Shift> AddAsync(Shift shift, CancellationToken ct = default);
    Task UpdateAsync(Shift shift, CancellationToken ct = default);
    Task<IReadOnlyList<Shift>> GetByStationAsync(Guid stationId, int page, int pageSize, CancellationToken ct = default);
}

public interface IVoidedTransactionRepository
{
    Task<VoidedTransaction> AddAsync(VoidedTransaction vt, CancellationToken ct = default);
}

public interface IRegisteredVehicleRepository
{
    Task<RegisteredVehicle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RegisteredVehicle?> GetByRegistrationAsync(string regNumber, CancellationToken ct = default);
    Task<IReadOnlyList<RegisteredVehicle>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<RegisteredVehicle> AddAsync(RegisteredVehicle v, CancellationToken ct = default);
}

public interface IFleetAccountRepository
{
    Task<FleetAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FleetAccount>> GetAllAsync(CancellationToken ct = default);
    Task<FleetAccount> AddAsync(FleetAccount fa, CancellationToken ct = default);
    Task UpdateAsync(FleetAccount fa, CancellationToken ct = default);
}

public interface IFleetVehicleRepository
{
    Task<FleetVehicle> AddAsync(FleetVehicle fv, CancellationToken ct = default);
    Task<FleetVehicle?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task RemoveAsync(FleetVehicle fv, CancellationToken ct = default);
    Task<IReadOnlyList<FleetVehicle>> GetByAccountAsync(Guid accountId, CancellationToken ct = default);
}

public interface ICustomerWalletRepository
{
    Task<CustomerWallet?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<CustomerWallet> AddAsync(CustomerWallet w, CancellationToken ct = default);
    Task UpdateAsync(CustomerWallet w, CancellationToken ct = default);
}

public interface IWalletTransactionRepository
{
    Task<WalletTransaction> AddAsync(WalletTransaction wt, CancellationToken ct = default);
    Task UpdateAsync(WalletTransaction wt, CancellationToken ct = default);
    Task<WalletTransaction?> GetByRazorpayOrderIdAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<WalletTransaction>> GetByWalletIdAsync(Guid walletId, int page, int pageSize, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
