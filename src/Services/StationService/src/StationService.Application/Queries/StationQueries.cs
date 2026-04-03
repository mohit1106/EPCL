using AutoMapper;
using MediatR;
using StationService.Application.Common;
using StationService.Application.DTOs;
using StationService.Domain.Exceptions;
using StationService.Domain.Interfaces;

namespace StationService.Application.Queries;

// ── GetStations (paginated) ────────────────────────────────────────
public record GetStationsQuery(
    int Page = 1, int PageSize = 20, string? City = null,
    string? State = null, bool? IsActive = null, string? SearchTerm = null
) : IRequest<PagedResult<StationDto>>;

public class GetStationsHandler(IStationRepository stationRepo, IMapper mapper)
    : IRequestHandler<GetStationsQuery, PagedResult<StationDto>>
{
    public async Task<PagedResult<StationDto>> Handle(GetStationsQuery query, CancellationToken ct)
    {
        var (items, totalCount) = await stationRepo.GetAllAsync(
            query.Page, query.PageSize, query.City, query.State, query.IsActive, query.SearchTerm, ct);

        return new PagedResult<StationDto>
        {
            Items = mapper.Map<IReadOnlyList<StationDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

// ── GetStationById ─────────────────────────────────────────────────
public record GetStationByIdQuery(Guid StationId) : IRequest<StationDto>;

public class GetStationByIdHandler(IStationRepository stationRepo, IMapper mapper)
    : IRequestHandler<GetStationByIdQuery, StationDto>
{
    public async Task<StationDto> Handle(GetStationByIdQuery query, CancellationToken ct)
    {
        var station = await stationRepo.GetByIdAsync(query.StationId, ct)
            ?? throw new NotFoundException("Station", query.StationId);
        return mapper.Map<StationDto>(station);
    }
}

// ── GetNearbyStations ──────────────────────────────────────────────
public record GetNearbyStationsQuery(
    decimal Latitude, decimal Longitude, double RadiusKm = 10,
    Guid? FuelTypeId = null
) : IRequest<IReadOnlyList<StationDto>>;

public class GetNearbyStationsHandler(IStationRepository stationRepo, IMapper mapper)
    : IRequestHandler<GetNearbyStationsQuery, IReadOnlyList<StationDto>>
{
    public async Task<IReadOnlyList<StationDto>> Handle(GetNearbyStationsQuery query, CancellationToken ct)
    {
        var stations = await stationRepo.GetNearbyAsync(
            query.Latitude, query.Longitude, query.RadiusKm, query.FuelTypeId, ct);

        var dtos = mapper.Map<IReadOnlyList<StationDto>>(stations);

        // Calculate distance for each station
        var result = dtos.Select((dto, i) =>
        {
            dto.DistanceKm = HaversineDistanceKm(
                (double)query.Latitude, (double)query.Longitude,
                (double)stations[i].Latitude, (double)stations[i].Longitude);
            return dto;
        }).OrderBy(d => d.DistanceKm).ToList();

        return result;
    }

    private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Earth radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(R * c, 2);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

// ── GetFuelTypes ───────────────────────────────────────────────────
public record GetFuelTypesQuery(bool? IsActive = null) : IRequest<IReadOnlyList<FuelTypeDto>>;

public class GetFuelTypesHandler(IFuelTypeRepository fuelTypeRepo)
    : IRequestHandler<GetFuelTypesQuery, IReadOnlyList<FuelTypeDto>>
{
    public async Task<IReadOnlyList<FuelTypeDto>> Handle(GetFuelTypesQuery query, CancellationToken ct)
    {
        var fuelTypes = await fuelTypeRepo.GetAllAsync(query.IsActive, ct);
        return fuelTypes.Select(f => new FuelTypeDto
        {
            Id = f.Id,
            Name = f.Name,
            Description = f.Description,
            IsActive = f.IsActive
        }).ToList();
    }
}
