using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ReportingService.API.Hubs;

/// <summary>
/// Admin-only hub for real-time platform-wide notifications.
/// Events: NewFraudAlert, StockCritical, ReplenishmentRequested
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        await Clients.Caller.SendAsync("Connected", new
        {
            message = "Connected to AdminHub",
            userId,
            timestamp = DateTimeOffset.UtcNow
        });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Dealer-only hub, grouped by StationId for station-specific updates.
/// Events: StockCritical, FuelPriceUpdated
/// </summary>
[Authorize(Roles = "Dealer")]
public class DealerHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var stationId = Context.User?.FindFirst("StationId")?.Value;
        if (!string.IsNullOrEmpty(stationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Station-{stationId}");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllDealers");

        await Clients.Caller.SendAsync("Connected", new
        {
            message = "Connected to DealerHub",
            stationId,
            timestamp = DateTimeOffset.UtcNow
        });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var stationId = Context.User?.FindFirst("StationId")?.Value;
        if (!string.IsNullOrEmpty(stationId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Station-{stationId}");
        }
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AllDealers");
        await base.OnDisconnectedAsync(exception);
    }
}
