using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Commands;

// ══════════════════════════════════════════════════════════════════
// SendNotification — core command used by all handlers
// ══════════════════════════════════════════════════════════════════
public record SendNotificationCommand(
    Guid? RecipientUserId, string? RecipientEmail, string? RecipientPhone,
    NotificationChannel Channel, string? Subject, string Body,
    string TriggerEvent) : IRequest<NotificationLogDto>;

public class SendNotificationHandler(
    INotificationLogRepository logRepo, IEmailService emailService,
    ISmsService smsService, ILogger<SendNotificationHandler> logger)
    : IRequestHandler<SendNotificationCommand, NotificationLogDto>
{
    public async Task<NotificationLogDto> Handle(SendNotificationCommand cmd, CancellationToken ct)
    {
        var log = new NotificationLog
        {
            Id = Guid.NewGuid(), RecipientUserId = cmd.RecipientUserId,
            RecipientEmail = cmd.RecipientEmail, RecipientPhone = cmd.RecipientPhone,
            Channel = cmd.Channel, Subject = cmd.Subject, Body = cmd.Body,
            TriggerEvent = cmd.TriggerEvent, Status = NotificationStatus.Pending
        };
        await logRepo.AddAsync(log, ct);

        try
        {
            switch (cmd.Channel)
            {
                case NotificationChannel.Email when !string.IsNullOrEmpty(cmd.RecipientEmail):
                    await emailService.SendAsync(cmd.RecipientEmail, cmd.Subject ?? "EPCL Notification", cmd.Body, ct);
                    break;
                case NotificationChannel.SMS when !string.IsNullOrEmpty(cmd.RecipientPhone):
                    await smsService.SendAsync(cmd.RecipientPhone, cmd.Body, ct);
                    break;
                case NotificationChannel.InApp:
                    // InApp notifications are just stored in DB — no external delivery
                    break;
            }
            log.Status = NotificationStatus.Sent;
            log.SentAt = DateTimeOffset.UtcNow;
            logger.LogInformation("Notification sent via {Channel} to {Recipient}. Trigger: {Event}",
                cmd.Channel, cmd.RecipientEmail ?? cmd.RecipientPhone ?? cmd.RecipientUserId?.ToString(), cmd.TriggerEvent);
        }
        catch (Exception ex)
        {
            log.Status = NotificationStatus.Failed;
            log.FailureReason = ex.Message;
            logger.LogError(ex, "Failed to send notification via {Channel}. Trigger: {Event}", cmd.Channel, cmd.TriggerEvent);
        }

        await logRepo.UpdateAsync(log, ct);
        return MapLog(log);
    }

    private static NotificationLogDto MapLog(NotificationLog l) => new(
        l.Id, l.RecipientUserId, l.RecipientEmail, l.RecipientPhone,
        l.Channel.ToString(), l.Subject, l.Body, l.Status.ToString(),
        l.TriggerEvent, l.FailureReason, l.SentAt, l.CreatedAt, l.IsRead);
}

// ══════════════════════════════════════════════════════════════════
// MarkNotificationRead
// ══════════════════════════════════════════════════════════════════
public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<MessageResponseDto>;

public class MarkNotificationReadHandler(INotificationLogRepository repo) : IRequestHandler<MarkNotificationReadCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var log = await repo.GetByIdAsync(cmd.NotificationId, ct) ?? throw new NotFoundException("Notification", cmd.NotificationId);
        log.IsRead = true;
        await repo.UpdateAsync(log, ct);
        return new MessageResponseDto("Notification marked as read.");
    }
}

// ══════════════════════════════════════════════════════════════════
// MarkAllNotificationsRead
// ══════════════════════════════════════════════════════════════════
public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<MessageResponseDto>;

public class MarkAllNotificationsReadHandler(INotificationLogRepository repo) : IRequestHandler<MarkAllNotificationsReadCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        await repo.MarkAllReadAsync(cmd.UserId, ct);
        return new MessageResponseDto("All notifications marked as read.");
    }
}

// ══════════════════════════════════════════════════════════════════
// SubscribeToPriceAlert
// ══════════════════════════════════════════════════════════════════
public record SubscribePriceAlertCommand(Guid UserId, Guid FuelTypeId, string AlertType, decimal? ThresholdPrice, string Channel) : IRequest<PriceAlertSubscriptionDto>;

public class SubscribePriceAlertHandler(IPriceAlertSubscriptionRepository repo) : IRequestHandler<SubscribePriceAlertCommand, PriceAlertSubscriptionDto>
{
    public async Task<PriceAlertSubscriptionDto> Handle(SubscribePriceAlertCommand cmd, CancellationToken ct)
    {
        Enum.TryParse<PriceAlertType>(cmd.AlertType, out var alertType);
        Enum.TryParse<NotificationChannel>(cmd.Channel, out var channel);
        var sub = new PriceAlertSubscription
        {
            Id = Guid.NewGuid(), UserId = cmd.UserId, FuelTypeId = cmd.FuelTypeId,
            AlertType = alertType, ThresholdPrice = cmd.ThresholdPrice, Channel = channel
        };
        await repo.AddAsync(sub, ct);
        return new PriceAlertSubscriptionDto(sub.Id, sub.UserId, sub.FuelTypeId, sub.AlertType.ToString(),
            sub.ThresholdPrice, sub.Channel.ToString(), sub.IsActive, sub.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// UnsubscribeFromPriceAlert
// ══════════════════════════════════════════════════════════════════
public record UnsubscribePriceAlertCommand(Guid SubscriptionId, Guid UserId) : IRequest<MessageResponseDto>;

public class UnsubscribePriceAlertHandler(IPriceAlertSubscriptionRepository repo) : IRequestHandler<UnsubscribePriceAlertCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(UnsubscribePriceAlertCommand cmd, CancellationToken ct)
    {
        var sub = await repo.GetByIdAsync(cmd.SubscriptionId, ct) ?? throw new NotFoundException("PriceAlertSubscription", cmd.SubscriptionId);
        await repo.RemoveAsync(sub, ct);
        return new MessageResponseDto("Unsubscribed from price alert.");
    }
}
