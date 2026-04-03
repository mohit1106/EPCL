using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;

namespace InventoryService.Domain.Interfaces;

public interface ITankRepository
{
    Task<Tank?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Tank>> GetByStationIdAsync(Guid stationId, CancellationToken ct = default);
    Task<Tank> AddAsync(Tank tank, CancellationToken ct = default);
    Task UpdateAsync(Tank tank, CancellationToken ct = default);
    Task<bool> ExistsBySerialAsync(string serialNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Tank>> GetLowStockTanksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Tank>> GetAllAsync(CancellationToken ct = default);
}

public interface IStockLoadingRepository
{
    Task<StockLoading> AddAsync(StockLoading loading, CancellationToken ct = default);
    Task<IReadOnlyList<StockLoading>> GetByTankIdAsync(Guid tankId, int page, int pageSize, CancellationToken ct = default);
}

public interface IDipReadingRepository
{
    Task<DipReading> AddAsync(DipReading reading, CancellationToken ct = default);
    Task<IReadOnlyList<DipReading>> GetByTankIdAsync(Guid tankId, int page, int pageSize, CancellationToken ct = default);
}

public interface IReplenishmentRequestRepository
{
    Task<ReplenishmentRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReplenishmentRequest> AddAsync(ReplenishmentRequest request, CancellationToken ct = default);
    Task UpdateAsync(ReplenishmentRequest request, CancellationToken ct = default);
    Task<(IReadOnlyList<ReplenishmentRequest> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, ReplenishmentStatus? status = null,
        Guid? stationId = null, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
