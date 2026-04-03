using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Commands;
using NotificationService.Application.DTOs;
using NotificationService.Application.Queries;

namespace NotificationService.API.Controllers;

/// <summary>In-app notification management.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    /// <summary>Get in-app notifications for the current user.</summary>
    [HttpGet("in-app")]
    public async Task<IActionResult> GetInApp([FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetInAppNotificationsQuery(userId, isRead, page, pageSize)));
    }

    /// <summary>Mark a single notification as read.</summary>
    [HttpPut("in-app/{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new MarkNotificationReadCommand(id, userId)));
    }

    /// <summary>Mark all notifications as read.</summary>
    [HttpPut("in-app/read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new MarkAllNotificationsReadCommand(userId)));
    }

    /// <summary>Get notification delivery logs (Admin only).</summary>
    [HttpGet("logs")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetNotificationLogsQuery(page, pageSize)));
}

/// <summary>Price alert subscription management.</summary>
[ApiController]
[Route("api/notifications/price-alerts")]
[Authorize]
public class PriceAlertsController(IMediator mediator) : ControllerBase
{
    /// <summary>Subscribe to a price alert.</summary>
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribePriceAlertRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new SubscribePriceAlertCommand(userId, body.FuelTypeId, body.AlertType, body.ThresholdPrice, body.Channel)));
    }

    /// <summary>Get own price alert subscriptions.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetPriceAlertSubscriptionsQuery(userId)));
    }

    /// <summary>Unsubscribe from a price alert.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new UnsubscribePriceAlertCommand(id, userId)));
    }
}
