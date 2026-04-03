using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Queries;

public record GetInAppNotificationsQuery(Guid UserId, bool? IsRead = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<NotificationLogDto>>;

public class GetInAppNotificationsHandler(INotificationLogRepository repo)
    : IRequestHandler<GetInAppNotificationsQuery, PagedResult<NotificationLogDto>>
{
    public async Task<PagedResult<NotificationLogDto>> Handle(GetInAppNotificationsQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.GetByUserAsync(q.UserId, q.IsRead, q.Page, q.PageSize, ct);
        return new PagedResult<NotificationLogDto>
        {
            Items = items.Select(l => new NotificationLogDto(l.Id, l.RecipientUserId, l.RecipientEmail,
                l.RecipientPhone, l.Channel.ToString(), l.Subject, l.Body, l.Status.ToString(),
                l.TriggerEvent, l.FailureReason, l.SentAt, l.CreatedAt, l.IsRead)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }
}

public record GetPriceAlertSubscriptionsQuery(Guid UserId) : IRequest<IReadOnlyList<PriceAlertSubscriptionDto>>;

public class GetPriceAlertSubscriptionsHandler(IPriceAlertSubscriptionRepository repo)
    : IRequestHandler<GetPriceAlertSubscriptionsQuery, IReadOnlyList<PriceAlertSubscriptionDto>>
{
    public async Task<IReadOnlyList<PriceAlertSubscriptionDto>> Handle(GetPriceAlertSubscriptionsQuery q, CancellationToken ct)
    {
        var subs = await repo.GetByUserAsync(q.UserId, ct);
        return subs.Select(s => new PriceAlertSubscriptionDto(s.Id, s.UserId, s.FuelTypeId,
            s.AlertType.ToString(), s.ThresholdPrice, s.Channel.ToString(), s.IsActive, s.CreatedAt)).ToList();
    }
}

public record GetNotificationLogsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<NotificationLogDto>>;

public class GetNotificationLogsHandler(INotificationLogRepository repo)
    : IRequestHandler<GetNotificationLogsQuery, PagedResult<NotificationLogDto>>
{
    public async Task<PagedResult<NotificationLogDto>> Handle(GetNotificationLogsQuery q, CancellationToken ct)
    {
        var (items, total) = await repo.GetLogsPagedAsync(q.Page, q.PageSize, ct);
        return new PagedResult<NotificationLogDto>
        {
            Items = items.Select(l => new NotificationLogDto(l.Id, l.RecipientUserId, l.RecipientEmail,
                l.RecipientPhone, l.Channel.ToString(), l.Subject, l.Body, l.Status.ToString(),
                l.TriggerEvent, l.FailureReason, l.SentAt, l.CreatedAt, l.IsRead)).ToList(),
            TotalCount = total, Page = q.Page, PageSize = q.PageSize
        };
    }
}
