using StationService.Domain.Entities;

namespace StationService.Domain.Interfaces;

public interface IStationRepository
{
    Task<Station?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Station> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? city = null, string? state = null,
        bool? isActive = null, string? searchTerm = null, CancellationToken ct = default);
    Task<IReadOnlyList<Station>> GetNearbyAsync(
        decimal latitude, decimal longitude, double radiusKm,
        Guid? fuelTypeId = null, CancellationToken ct = default);
    Task<Station> AddAsync(Station station, CancellationToken ct = default);
    Task UpdateAsync(Station station, CancellationToken ct = default);
    Task DeleteAsync(Station station, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string stationCode, CancellationToken ct = default);
    Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default);
}

public interface IFuelTypeRepository
{
    Task<FuelType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FuelType>> GetAllAsync(bool? isActive = null, CancellationToken ct = default);
    Task<FuelType> AddAsync(FuelType fuelType, CancellationToken ct = default);
    Task UpdateAsync(FuelType fuelType, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
