namespace NotificationService.Application.DTOs;

public record NotificationLogDto(
    Guid Id, Guid? RecipientUserId, string? RecipientEmail, string? RecipientPhone,
    string Channel, string? Subject, string Body, string Status,
    string TriggerEvent, string? FailureReason, DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt, bool IsRead);

public record PriceAlertSubscriptionDto(
    Guid Id, Guid UserId, Guid FuelTypeId, string AlertType,
    decimal? ThresholdPrice, string Channel, bool IsActive, DateTimeOffset CreatedAt);

public record MessageResponseDto(string Message);

public record SubscribePriceAlertRequest(Guid FuelTypeId, string AlertType, decimal? ThresholdPrice, string Channel);

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
