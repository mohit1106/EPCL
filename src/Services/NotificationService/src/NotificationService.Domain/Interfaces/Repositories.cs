using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Interfaces;

public interface INotificationLogRepository
{
    Task<NotificationLog> AddAsync(NotificationLog log, CancellationToken ct = default);
    Task UpdateAsync(NotificationLog log, CancellationToken ct = default);
    Task<NotificationLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<NotificationLog> Items, int Total)> GetByUserAsync(Guid userId, bool? isRead, int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<NotificationLog> Items, int Total)> GetLogsPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}

public interface IPriceAlertSubscriptionRepository
{
    Task<PriceAlertSubscription> AddAsync(PriceAlertSubscription sub, CancellationToken ct = default);
    Task<PriceAlertSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PriceAlertSubscription>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<PriceAlertSubscription>> GetActiveByFuelTypeAsync(Guid fuelTypeId, CancellationToken ct = default);
    Task RemoveAsync(PriceAlertSubscription sub, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
