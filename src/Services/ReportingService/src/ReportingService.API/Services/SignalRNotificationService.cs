using Microsoft.AspNetCore.SignalR;
using ReportingService.API.Hubs;
using ReportingService.Infrastructure.Messaging;

namespace ReportingService.API.Services;

/// <summary>
/// Concrete SignalR notification service using IHubContext for AdminHub and DealerHub.
/// </summary>
public class SignalRNotificationService(
    IHubContext<AdminHub> adminHub,
    IHubContext<DealerHub> dealerHub) : ISignalRNotificationService
{
    public async Task SendFraudAlertAsync(object alert)
        => await adminHub.Clients.Group("Admins").SendAsync("NewFraudAlert", alert);

    public async Task SendStockCriticalToAdminAsync(object stockData)
        => await adminHub.Clients.Group("Admins").SendAsync("StockCritical", stockData);

    public async Task SendReplenishmentRequestedAsync(object request)
        => await adminHub.Clients.Group("Admins").SendAsync("ReplenishmentRequested", request);

    public async Task SendStockCriticalToStationAsync(string stationId, object stockData)
        => await dealerHub.Clients.Group($"Station-{stationId}").SendAsync("StockCritical", stockData);

    public async Task SendFuelPriceUpdatedAsync(object priceUpdate)
        => await dealerHub.Clients.Group("AllDealers").SendAsync("FuelPriceUpdated", priceUpdate);
}
