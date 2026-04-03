using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SalesService.Application.Interfaces;

namespace SalesService.Infrastructure.Services;

public class RazorpaySettings
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

/// <summary>
/// Razorpay payment gateway integration.
/// Uses the Razorpay .NET SDK for order creation, payment capture, and refunds.
/// Signature verification is done manually to avoid SDK issues with dynamic dispatch.
/// </summary>
public class RazorpayService : IRazorpayService
{
    private readonly Razorpay.Api.RazorpayClient _client;
    private readonly RazorpaySettings _settings;
    private readonly ILogger<RazorpayService> _logger;

    public RazorpayService(IOptions<RazorpaySettings> settings, ILogger<RazorpayService> logger)
    {
        _settings = settings.Value;
        _client = new Razorpay.Api.RazorpayClient(_settings.KeyId, _settings.KeySecret);
        _logger = logger;
    }

    public Task<CreateOrderResult> CreateOrderAsync(decimal amountInRupees, string currency = "INR")
    {
        var options = new Dictionary<string, object>
        {
            ["amount"] = (int)(amountInRupees * 100),
            ["currency"] = currency,
            ["receipt"] = $"epcl-wallet-{Guid.NewGuid():N}",
            ["notes"] = new Dictionary<string, string> { ["platform"] = "EPCL", ["purpose"] = "wallet-topup" }
        };

        Razorpay.Api.Order order = _client.Order.Create(options);
        string orderId = (string)order["id"];
        _logger.LogInformation("Razorpay order created: {OrderId}, Amount: ₹{Amount}", orderId, amountInRupees);

        return Task.FromResult(new CreateOrderResult(orderId, amountInRupees, currency, _settings.KeyId));
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
    {
        // Manual HMAC-SHA256 to avoid SDK dynamic dispatch issues
        var body = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        var isValid = string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
        if (!isValid) _logger.LogWarning("Invalid Razorpay signature for OrderId: {OrderId}", orderId);
        return isValid;
    }

    public Task<bool> CapturePaymentAsync(string paymentId, decimal amountInRupees)
    {
        var amountPaise = (int)(amountInRupees * 100);
        Razorpay.Api.Payment payment = _client.Payment.Fetch(paymentId);
        payment.Capture(new Dictionary<string, object> { ["amount"] = amountPaise, ["currency"] = "INR" });
        string status = (string)payment["status"];
        return Task.FromResult(status == "captured");
    }

    public Task<string> InitiateRefundAsync(string paymentId, decimal amountInRupees, string reason)
    {
        var amountPaise = (int)(amountInRupees * 100);
        Razorpay.Api.Payment payment = _client.Payment.Fetch(paymentId);
        Razorpay.Api.Refund refund = payment.Refund(new Dictionary<string, object>
        {
            ["amount"] = amountPaise,
            ["notes"] = new Dictionary<string, string> { ["reason"] = reason }
        });
        string refundId = (string)refund["id"];
        return Task.FromResult(refundId);
    }
}
