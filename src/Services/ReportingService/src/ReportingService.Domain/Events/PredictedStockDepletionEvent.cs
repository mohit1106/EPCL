namespace ReportingService.Domain.Events;

public class PredictedStockDepletionEvent
{
    public Guid TankId { get; init; }
    public Guid StationId { get; init; }
    public decimal DaysUntilEmpty { get; init; }
    public DateTimeOffset PredictedEmptyAt { get; init; }
    public decimal CurrentStockLitres { get; init; }
    public decimal AvgDailyConsumption { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
