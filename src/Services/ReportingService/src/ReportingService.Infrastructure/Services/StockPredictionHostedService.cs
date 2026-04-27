using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportingService.Domain.Entities;
using ReportingService.Domain.Events;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Infrastructure.Services;

public class StockPredictionHostedService(
    IServiceProvider services,
    ILogger<StockPredictionHostedService> logger) : BackgroundService
{
    private class TankSummaryDto
    {
        public Guid Id { get; set; }
        public Guid StationId { get; set; }
        public Guid FuelTypeId { get; set; }
        public decimal CurrentStockLitres { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var istNow = now.ToOffset(TimeSpan.FromHours(5.5));
            var nextRun = istNow.Date.AddDays(1).AddHours(2);
            var delay = nextRun - istNow;

            // In local/docker testing, we run once right away so we don't have to wait until 2 AM IST
            await RunPredictionsAsync(ct);
            
            logger.LogInformation("Next stock prediction run scheduled in {DelayHours} hours", delay.TotalHours);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task RunPredictionsAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting daily stock prediction run at {Time}", DateTimeOffset.UtcNow);

        using var scope = services.CreateScope();
        var reportRepo = scope.ServiceProvider.GetRequiredService<IDailySalesSummaryRepository>();
        var predictionRepo = scope.ServiceProvider.GetRequiredService<IStockPredictionRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        try
        {
            // Call API Gateway to get Inventory Tanks. Base address is configured in DI.
            var httpClient = httpClientFactory.CreateClient("GatewayClient");
            var res = await httpClient.GetAsync("/gateway/inventory/tanks", ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch tanks from inventory. Status code: {StatusCode}", res.StatusCode);
                return;
            }

            var allTanks = await res.Content.ReadFromJsonAsync<List<TankSummaryDto>>(cancellationToken: ct) ?? [];
            var activeTanks = allTanks.Where(t => t.Status == "Active").ToList();

            foreach (var tank in activeTanks)
            {
                // Get last 30 days of DailySalesSummaries for this station + fuel type
                var dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
                var dateTo = DateOnly.FromDateTime(DateTime.UtcNow);
                
                var salesHistory = await reportRepo.GetAsync(
                    tank.StationId, tank.FuelTypeId, dateFrom, dateTo, ct);

                // Need at least 3 days of historical data to make a somewhat reliable prediction
                if (salesHistory.Count < 3) continue;

                // Calculate weighted average (recent days weighted more)
                var avgDailyConsumption = CalculateWeightedAverage(salesHistory);

                decimal? daysUntilEmpty = avgDailyConsumption > 0
                    ? Math.Round(tank.CurrentStockLitres / avgDailyConsumption, 1)
                    : null;

                DateTime? predictedEmptyAt = daysUntilEmpty.HasValue
                    ? DateTime.UtcNow.AddDays((double)daysUntilEmpty.Value)
                    : null;

                var prediction = new StockPrediction
                {
                    TankId = tank.Id,
                    StationId = tank.StationId,
                    FuelTypeId = tank.FuelTypeId,
                    CurrentStockLitres = tank.CurrentStockLitres,
                    AvgDailyConsumptionL = avgDailyConsumption,
                    PredictedEmptyAt = predictedEmptyAt,
                    DaysUntilEmpty = daysUntilEmpty,
                    CalculatedAt = DateTimeOffset.UtcNow,
                    DataPointsUsed = salesHistory.Count
                };

                await predictionRepo.UpsertAsync(prediction, ct);

                // Send 5-day alert if not already sent
                var existingPrediction = await predictionRepo.GetByTankIdAsync(tank.Id, ct);
                if (daysUntilEmpty is <= 5 && existingPrediction?.AlertSentAt == null)
                {
                    await publisher.PublishAsync(new PredictedStockDepletionEvent
                    {
                        TankId = tank.Id,
                        StationId = tank.StationId,
                        DaysUntilEmpty = daysUntilEmpty.Value,
                        PredictedEmptyAt = new DateTimeOffset(predictedEmptyAt!.Value, TimeSpan.Zero),
                        CurrentStockLitres = tank.CurrentStockLitres,
                        AvgDailyConsumption = avgDailyConsumption
                    }, "reporting.stock.prediction.alert", ct);

                    await predictionRepo.MarkAlertSentAsync(tank.Id, ct);
                }
            }

            logger.LogInformation("Stock prediction run complete. Processed {Count} tanks", activeTanks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during stock prediction run");
        }
    }

    private static decimal CalculateWeightedAverage(IReadOnlyList<DailySalesSummary> history)
    {
        // history is ordered by Date Descending from repository
        decimal totalWeight = 0;
        decimal weightedSum = 0;
        
        for (int i = 0; i < history.Count; i++)
        {
            // The first element in list is the most recent (i=0) -> Pow(0.9, 0) = 1.0 (highest weight)
            // Last element is oldest -> Pow(0.9, >0)
            decimal weight = (decimal)Math.Pow(0.9, i);
            weightedSum += history[i].TotalLitresSold * weight;
            totalWeight += weight;
        }
        return totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 3) : 0;
    }
}
