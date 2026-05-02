using MediatR;
using InventoryService.Application.Common;
using InventoryService.Application.DTOs;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Exceptions;
using InventoryService.Domain.Interfaces;

namespace InventoryService.Application.Queries;

// ── GetTanksByStation ──────────────────────────────────────────────
public record GetTanksByStationQuery(Guid StationId) : IRequest<IReadOnlyList<TankDto>>;

public class GetTanksByStationHandler(ITankRepository tankRepo)
    : IRequestHandler<GetTanksByStationQuery, IReadOnlyList<TankDto>>
{
    public async Task<IReadOnlyList<TankDto>> Handle(GetTanksByStationQuery q, CancellationToken ct)
    {
        var tanks = await tankRepo.GetByStationIdAsync(q.StationId, ct);
        return tanks.Select(t => new TankDto(t.Id, t.StationId, t.FuelTypeId, t.TankSerialNumber,
            t.CapacityLitres, t.CurrentStockLitres, t.ReservedLitres, t.AvailableStock,
            t.MinThresholdLitres, t.Status.ToString(), t.LastReplenishedAt,
            t.LastDipReadingAt, t.CreatedAt)).ToList();
    }
}

// ── GetTankById ────────────────────────────────────────────────────
public record GetTankByIdQuery(Guid TankId) : IRequest<TankDto>;

public class GetTankByIdHandler(ITankRepository tankRepo)
    : IRequestHandler<GetTankByIdQuery, TankDto>
{
    public async Task<TankDto> Handle(GetTankByIdQuery q, CancellationToken ct)
    {
        var t = await tankRepo.GetByIdAsync(q.TankId, ct)
            ?? throw new NotFoundException("Tank", q.TankId);
        return new TankDto(t.Id, t.StationId, t.FuelTypeId, t.TankSerialNumber,
            t.CapacityLitres, t.CurrentStockLitres, t.ReservedLitres, t.AvailableStock,
            t.MinThresholdLitres, t.Status.ToString(), t.LastReplenishedAt,
            t.LastDipReadingAt, t.CreatedAt);
    }
}

// ── GetStockLoadingHistory ─────────────────────────────────────────
public record GetStockLoadingHistoryQuery(Guid TankId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<StockLoadingDto>>;

public class GetStockLoadingHistoryHandler(IStockLoadingRepository loadingRepo)
    : IRequestHandler<GetStockLoadingHistoryQuery, IReadOnlyList<StockLoadingDto>>
{
    public async Task<IReadOnlyList<StockLoadingDto>> Handle(GetStockLoadingHistoryQuery q, CancellationToken ct)
    {
        var items = await loadingRepo.GetByTankIdAsync(q.TankId, q.Page, q.PageSize, ct);
        return items.Select(l => new StockLoadingDto(l.Id, l.TankId, l.QuantityLoadedLitres,
            l.LoadedByUserId, l.TankerNumber, l.InvoiceNumber, l.SupplierName,
            l.StockBefore, l.StockAfter, l.Timestamp, l.Notes)).ToList();
    }
}

// ── GetDipReadingHistory ───────────────────────────────────────────
public record GetDipReadingHistoryQuery(Guid TankId, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<DipReadingDto>>;

public class GetDipReadingHistoryHandler(IDipReadingRepository dipRepo)
    : IRequestHandler<GetDipReadingHistoryQuery, IReadOnlyList<DipReadingDto>>
{
    public async Task<IReadOnlyList<DipReadingDto>> Handle(GetDipReadingHistoryQuery q, CancellationToken ct)
    {
        var items = await dipRepo.GetByTankIdAsync(q.TankId, q.Page, q.PageSize, ct);
        return items.Select(d => new DipReadingDto(d.Id, d.TankId, d.DipValueLitres,
            d.SystemStockLitres, d.VarianceLitres, d.VariancePercent, d.IsFraudFlagged,
            d.RecordedByUserId, d.Timestamp, d.Notes)).ToList();
    }
}

// ── GetReplenishmentRequests ───────────────────────────────────────
public record GetReplenishmentRequestsQuery(
    int Page = 1, int PageSize = 20, string? Status = null, Guid? StationId = null
) : IRequest<PagedResult<ReplenishmentRequestDto>>;

public class GetReplenishmentRequestsHandler(IReplenishmentRequestRepository replRepo)
    : IRequestHandler<GetReplenishmentRequestsQuery, PagedResult<ReplenishmentRequestDto>>
{
    public async Task<PagedResult<ReplenishmentRequestDto>> Handle(GetReplenishmentRequestsQuery q, CancellationToken ct)
    {
        ReplenishmentStatus? statusFilter = null;
        if (q.Status != null && Enum.TryParse<ReplenishmentStatus>(q.Status, out var s))
            statusFilter = s;

        var (items, total) = await replRepo.GetAllAsync(q.Page, q.PageSize, statusFilter, q.StationId, ct);

        return new PagedResult<ReplenishmentRequestDto>
        {
            Items = items.Select(r => new ReplenishmentRequestDto(r.Id, r.StationId, r.TankId,
                r.RequestedByUserId, r.RequestedQuantityLitres, r.UrgencyLevel.ToString(),
                r.Status.ToString(), r.RequestedAt, r.ReviewedByUserId, r.ReviewedAt,
                r.RejectionReason, r.Notes,
                r.OrderNumber, r.TargetPumpName, r.FuelTypeName, r.Priority, r.RequestedWindow,
                r.AssignedDriverId, r.AssignedDriverName, r.AssignedDriverPhone, r.AssignedDriverCode,
                r.DealerVerifiedAt, r.DealerVerifiedDriverCode)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }
}

// ── GetLowStockAlerts ──────────────────────────────────────────────
public record GetLowStockAlertsQuery() : IRequest<IReadOnlyList<TankDto>>;

public class GetLowStockAlertsHandler(ITankRepository tankRepo)
    : IRequestHandler<GetLowStockAlertsQuery, IReadOnlyList<TankDto>>
{
    public async Task<IReadOnlyList<TankDto>> Handle(GetLowStockAlertsQuery q, CancellationToken ct)
    {
        var tanks = await tankRepo.GetLowStockTanksAsync(ct);
        return tanks.Select(t => new TankDto(t.Id, t.StationId, t.FuelTypeId, t.TankSerialNumber,
            t.CapacityLitres, t.CurrentStockLitres, t.ReservedLitres, t.AvailableStock,
            t.MinThresholdLitres, t.Status.ToString(), t.LastReplenishedAt,
            t.LastDipReadingAt, t.CreatedAt)).ToList();
    }
}

// ── GetStockSummary ────────────────────────────────────────────────
public record GetStockSummaryQuery() : IRequest<StockSummaryDto>;

public class GetStockSummaryHandler(ITankRepository tankRepo)
    : IRequestHandler<GetStockSummaryQuery, StockSummaryDto>
{
    public async Task<StockSummaryDto> Handle(GetStockSummaryQuery q, CancellationToken ct)
    {
        var tanks = await tankRepo.GetAllAsync(ct);
        return new StockSummaryDto(
            TotalTanks: tanks.Count,
            TotalCapacity: tanks.Sum(t => t.CapacityLitres),
            TotalCurrentStock: tanks.Sum(t => t.CurrentStockLitres),
            TotalReserved: tanks.Sum(t => t.ReservedLitres),
            LowStockTanks: tanks.Count(t => t.Status == TankStatus.Low),
            CriticalTanks: tanks.Count(t => t.Status == TankStatus.Critical),
            OutOfStockTanks: tanks.Count(t => t.Status == TankStatus.OutOfStock));
    }
}
