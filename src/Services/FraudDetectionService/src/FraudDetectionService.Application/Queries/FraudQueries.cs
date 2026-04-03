using MediatR;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Exceptions;
using FraudDetectionService.Domain.Interfaces;

namespace FraudDetectionService.Application.Queries;

// ── GetFraudAlerts (paginated + filtered) ──────────────────────────
public record GetFraudAlertsQuery(
    int Page = 1, int PageSize = 20, string? Status = null, string? Severity = null,
    Guid? StationId = null, DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null
) : IRequest<PagedResult<FraudAlertDto>>;

public class GetFraudAlertsHandler(IFraudAlertRepository repo)
    : IRequestHandler<GetFraudAlertsQuery, PagedResult<FraudAlertDto>>
{
    public async Task<PagedResult<FraudAlertDto>> Handle(GetFraudAlertsQuery q, CancellationToken ct)
    {
        AlertStatus? status = q.Status != null && Enum.TryParse<AlertStatus>(q.Status, out var s) ? s : null;
        AlertSeverity? severity = q.Severity != null && Enum.TryParse<AlertSeverity>(q.Severity, out var sv) ? sv : null;

        var (items, total) = await repo.GetPagedAsync(q.Page, q.PageSize, status, severity,
            q.StationId, q.DateFrom, q.DateTo, ct);

        return new PagedResult<FraudAlertDto>
        {
            Items = items.Select(a => new FraudAlertDto(a.Id, a.TransactionId, a.StationId, a.RuleTriggered,
                a.Severity.ToString(), a.Description, a.Status.ToString(),
                a.ReviewedByUserId, a.ReviewedAt, a.ReviewNotes, a.CreatedAt)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }
}

// ── GetFraudAlertById ──────────────────────────────────────────────
public record GetFraudAlertByIdQuery(Guid AlertId) : IRequest<FraudAlertDto>;

public class GetFraudAlertByIdHandler(IFraudAlertRepository repo)
    : IRequestHandler<GetFraudAlertByIdQuery, FraudAlertDto>
{
    public async Task<FraudAlertDto> Handle(GetFraudAlertByIdQuery q, CancellationToken ct)
    {
        var a = await repo.GetByIdAsync(q.AlertId, ct) ?? throw new NotFoundException("FraudAlert", q.AlertId);
        return new FraudAlertDto(a.Id, a.TransactionId, a.StationId, a.RuleTriggered,
            a.Severity.ToString(), a.Description, a.Status.ToString(),
            a.ReviewedByUserId, a.ReviewedAt, a.ReviewNotes, a.CreatedAt);
    }
}

// ── GetFraudStats ──────────────────────────────────────────────────
public record GetFraudStatsQuery(Guid? StationId = null, DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null) : IRequest<FraudStatsDto>;

public class GetFraudStatsHandler(IFraudAlertRepository repo) : IRequestHandler<GetFraudStatsQuery, FraudStatsDto>
{
    public async Task<FraudStatsDto> Handle(GetFraudStatsQuery q, CancellationToken ct)
    {
        var open = await repo.GetCountByStatusAsync(AlertStatus.Open, q.StationId, q.DateFrom, q.DateTo, ct);
        var review = await repo.GetCountByStatusAsync(AlertStatus.UnderReview, q.StationId, q.DateFrom, q.DateTo, ct);
        var dismissed = await repo.GetCountByStatusAsync(AlertStatus.Dismissed, q.StationId, q.DateFrom, q.DateTo, ct);
        var escalated = await repo.GetCountByStatusAsync(AlertStatus.Escalated, q.StationId, q.DateFrom, q.DateTo, ct);
        var total = open + review + dismissed + escalated;
        // For severity, we'd need additional queries; for now estimate from totals
        return new FraudStatsDto(total, open, review, dismissed, escalated, 0, 0, 0);
    }
}
