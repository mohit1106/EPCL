namespace LoyaltyService.Domain.Events;

public class SaleCompletedEvent
{
    public Guid EventId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
    public Guid? CustomerUserId { get; set; }
    public decimal QuantityLitres { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
