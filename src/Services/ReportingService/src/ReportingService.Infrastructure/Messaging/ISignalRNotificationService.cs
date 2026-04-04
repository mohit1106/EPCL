namespace ReportingService.Infrastructure.Messaging;

/// <summary>
/// Abstraction for pushing real-time SignalR events from the Infrastructure layer.
/// Implemented in the API project using IHubContext.
/// </summary>
public interface ISignalRNotificationService
{
    // AdminHub events
    Task SendFraudAlertAsync(object alert);
    Task SendStockCriticalToAdminAsync(object stockData);
    Task SendReplenishmentRequestedAsync(object request);

    // DealerHub events
    Task SendStockCriticalToStationAsync(string stationId, object stockData);
    Task SendFuelPriceUpdatedAsync(object priceUpdate);
}
