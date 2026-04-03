using Microsoft.EntityFrameworkCore;
using StationService.Domain.Entities;
using StationService.Domain.Interfaces;
using StationService.Infrastructure.Persistence;

namespace StationService.Infrastructure.Repositories;

public class StationRepository(StationsDbContext context) : IStationRepository
{
    public async Task<Station?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Stations.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<(IReadOnlyList<Station> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? city = null, string? state = null,
        bool? isActive = null, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = context.Stations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(s => s.City.ToLower() == city.ToLower());
        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(s => s.State.ToLower() == state.ToLower());
        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(s =>
                s.StationName.ToLower().Contains(term) ||
                s.StationCode.ToLower().Contains(term) ||
                s.City.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Station>> GetNearbyAsync(
        decimal latitude, decimal longitude, double radiusKm,
        Guid? fuelTypeId = null, CancellationToken ct = default)
    {
        // Approximate bounding box for performance (1 degree ≈ 111 km)
        var latDelta = (decimal)(radiusKm / 111.0);
        var lonDelta = (decimal)(radiusKm / (111.0 * Math.Cos((double)latitude * Math.PI / 180.0)));

        var query = context.Stations
            .Where(s => s.IsActive)
            .Where(s => s.Latitude >= latitude - latDelta && s.Latitude <= latitude + latDelta)
            .Where(s => s.Longitude >= longitude - lonDelta && s.Longitude <= longitude + lonDelta);

        return await query.ToListAsync(ct);
    }

    public async Task<Station> AddAsync(Station station, CancellationToken ct = default)
    {
        await context.Stations.AddAsync(station, ct);
        await context.SaveChangesAsync(ct);
        return station;
    }

    public async Task UpdateAsync(Station station, CancellationToken ct = default)
    {
        context.Stations.Update(station);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByCodeAsync(string stationCode, CancellationToken ct = default)
        => await context.Stations.AnyAsync(s => s.StationCode == stationCode, ct);

    public async Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default)
        => await context.Stations.AnyAsync(s => s.LicenseNumber == licenseNumber, ct);
}

public class FuelTypeRepository(StationsDbContext context) : IFuelTypeRepository
{
    public async Task<FuelType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.FuelTypes.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<FuelType>> GetAllAsync(bool? isActive = null, CancellationToken ct = default)
    {
        var query = context.FuelTypes.AsQueryable();
        if (isActive.HasValue) query = query.Where(f => f.IsActive == isActive.Value);
        return await query.OrderBy(f => f.Name).ToListAsync(ct);
    }

    public async Task<FuelType> AddAsync(FuelType fuelType, CancellationToken ct = default)
    {
        await context.FuelTypes.AddAsync(fuelType, ct);
        await context.SaveChangesAsync(ct);
        return fuelType;
    }

    public async Task UpdateAsync(FuelType fuelType, CancellationToken ct = default)
    {
        context.FuelTypes.Update(fuelType);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await context.FuelTypes.AnyAsync(f => f.Name == name, ct);
}

public class ProcessedEventRepository(StationsDbContext context) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default)
        => await context.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default)
    {
        await context.ProcessedEvents.AddAsync(new ProcessedEvent
        {
            Id = Guid.NewGuid(), EventId = eventId, EventType = eventType, ProcessedAt = DateTimeOffset.UtcNow
        }, ct);
        await context.SaveChangesAsync(ct);
    }
}
