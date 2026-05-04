using MediatR;
using ReportingService.Application.DTOs;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Application.Queries;

// ══════════════════════════════════════════════════════════════════
// GetSalesSummary
// ══════════════════════════════════════════════════════════════════
public record GetSalesSummaryQuery(Guid? StationId = null, Guid? FuelTypeId = null,
    DateOnly? DateFrom = null, DateOnly? DateTo = null) : IRequest<IReadOnlyList<DailySalesSummaryDto>>;

public class GetSalesSummaryHandler(IDailySalesSummaryRepository repo)
    : IRequestHandler<GetSalesSummaryQuery, IReadOnlyList<DailySalesSummaryDto>>
{
    public async Task<IReadOnlyList<DailySalesSummaryDto>> Handle(GetSalesSummaryQuery q, CancellationToken ct)
    {
        var items = await repo.GetAsync(q.StationId, q.FuelTypeId, q.DateFrom, q.DateTo, ct);
        return items.Select(i => new DailySalesSummaryDto(i.Id, i.StationId, i.FuelTypeId,
            i.Date, i.TotalTransactions, i.TotalLitresSold, i.TotalRevenue)).ToList();
    }
}

// ══════════════════════════════════════════════════════════════════
// GetStationPerformance
// ══════════════════════════════════════════════════════════════════
public record GetStationPerformanceQuery(DateOnly? DateFrom = null, DateOnly? DateTo = null, int Top = 10)
    : IRequest<IReadOnlyList<DailySalesSummaryDto>>;

public class GetStationPerformanceHandler(IDailySalesSummaryRepository repo)
    : IRequestHandler<GetStationPerformanceQuery, IReadOnlyList<DailySalesSummaryDto>>
{
    public async Task<IReadOnlyList<DailySalesSummaryDto>> Handle(GetStationPerformanceQuery q, CancellationToken ct)
    {
        var items = await repo.GetAsync(null, null, q.DateFrom, q.DateTo, ct);
        return items.GroupBy(i => i.StationId)
            .Select(g => new DailySalesSummaryDto(Guid.Empty, g.Key, Guid.Empty,
                q.DateFrom ?? DateOnly.FromDateTime(DateTime.UtcNow),
                g.Sum(x => x.TotalTransactions), g.Sum(x => x.TotalLitresSold), g.Sum(x => x.TotalRevenue)))
            .OrderByDescending(x => x.TotalRevenue).Take(q.Top).ToList();
    }
}

// ══════════════════════════════════════════════════════════════════
// GetDealerSummary
// ══════════════════════════════════════════════════════════════════
public record GetDealerSummaryQuery(Guid StationId, int? Month = null, int? Year = null)
    : IRequest<IReadOnlyList<MonthlyStationReportDto>>;

public class GetDealerSummaryHandler(IMonthlyStationReportRepository repo)
    : IRequestHandler<GetDealerSummaryQuery, IReadOnlyList<MonthlyStationReportDto>>
{
    public async Task<IReadOnlyList<MonthlyStationReportDto>> Handle(GetDealerSummaryQuery q, CancellationToken ct)
    {
        var items = await repo.GetAsync(q.StationId, q.Year, q.Month, ct);
        return items.Select(i => new MonthlyStationReportDto(i.Id, i.StationId, i.Year, i.Month,
            i.TotalTransactions, i.TotalLitresSold, i.TotalRevenue,
            i.PetrolLitres, i.DieselLitres, i.CngLitres)).ToList();
    }
}

// ══════════════════════════════════════════════════════════════════
// GetAdminKpi — real-time KPI for admin dashboard
// ══════════════════════════════════════════════════════════════════
public record GetAdminKpiQuery : IRequest<AdminKpiDto>;

public class GetAdminKpiHandler(IDailySalesSummaryRepository repo)
    : IRequestHandler<GetAdminKpiQuery, AdminKpiDto>
{
    public async Task<AdminKpiDto> Handle(GetAdminKpiQuery q, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var todayItems = await repo.GetAsync(null, null, today, today, ct);
        // Count distinct stations with any activity this month as proxy for active dealers
        var monthItems = await repo.GetAsync(null, null, monthStart, today, ct);
        var activeDealers = monthItems.Select(i => i.StationId).Distinct().Count();
        return new AdminKpiDto(
            TotalStations: todayItems.Select(i => i.StationId).Distinct().Count(),
            TotalTransactionsToday: todayItems.Sum(i => i.TotalTransactions),
            TotalRevenueToday: todayItems.Sum(i => i.TotalRevenue),
            TotalLitresToday: todayItems.Sum(i => i.TotalLitresSold),
            FraudAlertsToday: 0, // cross-service data; populated via API composition
            ActiveDealers: activeDealers);
    }
}

// ══════════════════════════════════════════════════════════════════
// GetDealerKpi — KPI for dealer dashboard
// ══════════════════════════════════════════════════════════════════
public record GetDealerKpiQuery(Guid StationId) : IRequest<DealerKpiDto>;

public class GetDealerKpiHandler(IDailySalesSummaryRepository repo)
    : IRequestHandler<GetDealerKpiQuery, DealerKpiDto>
{
    public async Task<DealerKpiDto> Handle(GetDealerKpiQuery q, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var todayItems = await repo.GetAsync(q.StationId, null, today, today, ct);
        var monthItems = await repo.GetAsync(q.StationId, null, monthStart, today, ct);

        return new DealerKpiDto(q.StationId,
            TransactionsToday: todayItems.Sum(i => i.TotalTransactions),
            RevenueToday: todayItems.Sum(i => i.TotalRevenue),
            LitresToday: todayItems.Sum(i => i.TotalLitresSold),
            TransactionsThisMonth: monthItems.Sum(i => i.TotalTransactions),
            RevenueThisMonth: monthItems.Sum(i => i.TotalRevenue));
    }
}

// ══════════════════════════════════════════════════════════════════
// GetReportStatus
// ══════════════════════════════════════════════════════════════════
public record GetReportStatusQuery(Guid ReportId) : IRequest<GeneratedReportDto>;

public class GetReportStatusHandler(IGeneratedReportRepository repo)
    : IRequestHandler<GetReportStatusQuery, GeneratedReportDto>
{
    public async Task<GeneratedReportDto> Handle(GetReportStatusQuery q, CancellationToken ct)
    {
        var r = await repo.GetByIdAsync(q.ReportId, ct)
            ?? throw new Domain.Exceptions.NotFoundException("GeneratedReport", q.ReportId);
        return new GeneratedReportDto(r.Id, r.ReportType, r.Format, r.Status,
            r.DateFrom, r.DateTo, r.StationId, r.FileSize, r.GeneratedAt, r.ExpiresAt, r.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// GetScheduledReports
// ══════════════════════════════════════════════════════════════════
public record GetScheduledReportsQuery : IRequest<IReadOnlyList<ScheduledReportDto>>;

public class GetScheduledReportsHandler(IScheduledReportRepository repo)
    : IRequestHandler<GetScheduledReportsQuery, IReadOnlyList<ScheduledReportDto>>
{
    public async Task<IReadOnlyList<ScheduledReportDto>> Handle(GetScheduledReportsQuery q, CancellationToken ct)
    {
        var items = await repo.GetAllActiveAsync(ct);
        return items.Select(s => new ScheduledReportDto(s.Id, s.ReportType, s.CronExpression,
            s.StationId, s.Format, s.IsActive, s.CreatedAt)).ToList();
    }
}
