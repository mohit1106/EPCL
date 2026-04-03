using SalesService.Domain.Events;

namespace SalesService.Application.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : IntegrationEvent;
}

public interface IRazorpayService
{
    Task<CreateOrderResult> CreateOrderAsync(decimal amountInRupees, string currency = "INR");
    bool VerifyPaymentSignature(string orderId, string paymentId, string signature);
    Task<bool> CapturePaymentAsync(string paymentId, decimal amountInRupees);
    Task<string> InitiateRefundAsync(string paymentId, decimal amountInRupees, string reason);
}

public record CreateOrderResult(string OrderId, decimal Amount, string Currency, string KeyId);
