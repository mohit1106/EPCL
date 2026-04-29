using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NotificationService.Application.Commands;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// Consumes ALL notification-triggering events from RabbitMQ.
/// Routes: sales.completed, fraud.alert.triggered, inventory.stock.low, inventory.stock.critical,
///         inventory.replenishment.requested, inventory.replenishment.approved,
///         inventory.dip.variance, sales.price.updated, identity.account.locked
/// </summary>
public class NotificationConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.notifications.queue";

    public NotificationConsumerHostedService(IServiceScopeFactory scopeFactory,
        ILogger<NotificationConsumerHostedService> logger, IConfiguration config)
    { _scopeFactory = scopeFactory; _logger = logger; _config = config; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RABBITMQ_HOST"] ?? "localhost",
            Port = int.Parse(_config["RABBITMQ_PORT"] ?? "5672"),
            UserName = _config["RABBITMQ_USER"] ?? "guest",
            Password = _config["RABBITMQ_PASS"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        // Bind to all notification-triggering events
        var bindings = new[] {
            "sales.completed", "fraud.alert.triggered", "inventory.stock.low",
            "inventory.stock.critical", "inventory.replenishment.requested",
            "inventory.replenishment.approved", "inventory.dip.variance",
            "sales.price.updated", "identity.account.locked", "sales.voided",
            "reporting.stock.prediction.alert", "document.expiring.alert",
            "identity.user.registered"
        };
        foreach (var key in bindings)
            await _channel.QueueBindAsync(QueueName, ExchangeName, key, cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var eventType = ea.BasicProperties?.Type ?? "";
                var eventId = Guid.TryParse(ea.BasicProperties?.MessageId, out var eid) ? eid : Guid.NewGuid();

                using var scope = _scopeFactory.CreateScope();
                var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();

                if (await processedRepo.AlreadyProcessedAsync(eventId))
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                switch (eventType)
                {
                    case nameof(SaleCompletedEvent):
                        await HandleSaleCompleted(mediator, templateService, body, jsonOpts);
                        break;
                    case nameof(FraudAlertTriggeredEvent):
                        await HandleFraudAlert(mediator, templateService, body, jsonOpts);
                        break;
                    case nameof(StockLevelLowEvent):
                        await HandleStockLow(mediator, templateService, body, jsonOpts);
                        break;
                    case nameof(StockLevelCriticalEvent):
                        await HandleStockCritical(mediator, templateService, body, jsonOpts);
                        break;
                    case nameof(ReplenishmentRequestedEvent):
                        await HandleReplenishmentRequested(mediator, body, jsonOpts);
                        break;
                    case nameof(ReplenishmentApprovedEvent):
                        await HandleReplenishmentApproved(mediator, templateService, body, jsonOpts);
                        break;
                    case nameof(FuelPriceUpdatedEvent):
                        await HandleFuelPriceUpdated(mediator, body, jsonOpts);
                        break;
                    case nameof(DipVarianceDetectedEvent):
                        await HandleDipVariance(mediator, body, jsonOpts);
                        break;
                    case nameof(UserAccountLockedEvent):
                        await HandleAccountLocked(mediator, body, jsonOpts);
                        break;
                    case nameof(PredictedStockDepletionEvent):
                        await HandlePredictedStockDepletion(mediator, templateService, body, jsonOpts);
                        break;
                    case "DocumentExpiringEvent":
                        await HandleDocumentExpiring(mediator, templateService, body, jsonOpts);
                        break;
                    case nameof(UserRegisteredEvent):
                        await HandleUserRegistered(mediator, templateService, body, jsonOpts);
                        break;
                    default:
                        _logger.LogWarning("Unknown event type: {type}", eventType);
                        break;
                }

                await processedRepo.MarkProcessedAsync(eventId, eventType);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Notification consumer started on queue {Queue}", QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    // ── Handler: SaleCompleted → SMS receipt to Customer ────────
    private async Task HandleSaleCompleted(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<SaleCompletedEvent>(body, opts)!;
        if (evt.CustomerUserId.HasValue)
        {
            var html = tpl.Render("receipt", new Dictionary<string, string>
            {
                ["ReceiptNumber"] = evt.TransactionId.ToString()[..8].ToUpper(),
                ["StationName"] = evt.StationId.ToString()[..8],
                ["VehicleNumber"] = evt.VehicleNumber,
                ["FuelType"] = evt.FuelTypeId.ToString()[..8],
                ["Quantity"] = evt.QuantityLitres.ToString("F2"),
                ["PricePerLitre"] = evt.PricePerLitre.ToString("F2"),
                ["TotalAmount"] = evt.TotalAmount.ToString("F2"),
                ["PaymentMethod"] = evt.PaymentMethod,
                ["TransactionDate"] = evt.Timestamp.ToString("dd MMM yyyy HH:mm")
            });
            await m.Send(new SendNotificationCommand(evt.CustomerUserId, null, null,
                NotificationChannel.InApp, "Transaction Receipt", html,
                nameof(SaleCompletedEvent)));
        }
    }

    // ── Handler: FraudAlert → Email+SMS to Admin ────────────────
    private async Task HandleFraudAlert(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<FraudAlertTriggeredEvent>(body, opts)!;
        var severityColor = evt.Severity == "High" ? "#DC2626" : evt.Severity == "Medium" ? "#EA580C" : "#2563EB";
        var html = tpl.Render("fraud-alert", new Dictionary<string, string>
        {
            ["Severity"] = evt.Severity,
            ["SeverityColor"] = severityColor,
            ["RuleName"] = evt.RuleTriggered,
            ["Description"] = evt.Description,
            ["AlertId"] = evt.AlertId.ToString(),
            ["StationId"] = evt.StationId.ToString(),
            ["TransactionId"] = evt.TransactionId.ToString(),
            ["Timestamp"] = evt.Timestamp.ToString("dd MMM yyyy HH:mm UTC"),
            ["ReviewUrl"] = $"http://localhost:4200/admin/fraud/alerts/{evt.AlertId}"
        });
        // InApp notification for admin (no specific admin userId here; stored as system notification)
        await m.Send(new SendNotificationCommand(null, null, null,
            NotificationChannel.InApp, $"🚨 Fraud Alert: {evt.RuleTriggered}",
            html, nameof(FraudAlertTriggeredEvent)));
    }

    // ── Handler: StockLow → Email+SMS to Dealer, Email to Admin ─
    private async Task HandleStockLow(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<StockLevelLowEvent>(body, opts)!;
        var html = tpl.Render("low-stock", new Dictionary<string, string>
        {
            ["TankName"] = evt.TankName,
            ["StationName"] = evt.StationId.ToString()[..8],
            ["CurrentStock"] = evt.CurrentStockLitres.ToString("F0"),
            ["Threshold"] = evt.ThresholdLitres.ToString("F0"),
            ["Capacity"] = evt.CapacityLitres.ToString("F0"),
            ["DashboardUrl"] = "http://localhost:4200/dealer/replenishment"
        });
        await m.Send(new SendNotificationCommand(evt.DealerUserId, null, null,
            NotificationChannel.InApp, $"⚠ Low Stock: {evt.TankName}",
            html, nameof(StockLevelLowEvent)));
    }

    // ── Handler: StockCritical → HIGH PRIORITY Email+SMS ────────
    private async Task HandleStockCritical(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<StockLevelCriticalEvent>(body, opts)!;
        var html = tpl.Render("low-stock", new Dictionary<string, string>
        {
            ["TankName"] = evt.TankName,
            ["StationName"] = evt.StationId.ToString()[..8],
            ["CurrentStock"] = evt.CurrentStockLitres.ToString("F0"),
            ["Threshold"] = evt.ThresholdLitres.ToString("F0"),
            ["Capacity"] = "0",
            ["DashboardUrl"] = "http://localhost:4200/dealer/replenishment"
        });
        await m.Send(new SendNotificationCommand(evt.DealerUserId, null, null,
            NotificationChannel.InApp, $"🔴 CRITICAL Stock: {evt.TankName}",
            html, nameof(StockLevelCriticalEvent)));
    }

    // ── Handler: ReplenishmentRequested → Email to Admin ─────────
    private async Task HandleReplenishmentRequested(IMediator m, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<ReplenishmentRequestedEvent>(body, opts)!;
        await m.Send(new SendNotificationCommand(null, null, null,
            NotificationChannel.InApp, "Replenishment Requested",
            $"New replenishment request: {evt.RequestedQuantityLitres}L (Urgency: {evt.UrgencyLevel}) for station {evt.StationId}",
            nameof(ReplenishmentRequestedEvent)));
    }

    // ── Handler: ReplenishmentApproved → SMS+Email to Dealer ────
    private async Task HandleReplenishmentApproved(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<ReplenishmentApprovedEvent>(body, opts)!;
        var html = tpl.Render("replenishment-approved", new Dictionary<string, string>
        {
            ["TankName"] = evt.TankName,
            ["ApprovedQuantity"] = evt.ApprovedQuantityLitres.ToString("F0"),
            ["RequestId"] = evt.RequestId.ToString(),
            ["DashboardUrl"] = "http://localhost:4200/dealer/replenishment"
        });
        await m.Send(new SendNotificationCommand(evt.DealerUserId, null, null,
            NotificationChannel.InApp, "✅ Replenishment Approved",
            html, nameof(ReplenishmentApprovedEvent)));
    }

    // ── Handler: FuelPriceUpdated → SMS+Email to all Dealers ────
    private async Task HandleFuelPriceUpdated(IMediator m, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<FuelPriceUpdatedEvent>(body, opts)!;
        await m.Send(new SendNotificationCommand(null, null, null,
            NotificationChannel.InApp, "Fuel Price Update",
            $"Fuel price updated: ₹{evt.NewPricePerLitre}/L effective from {evt.EffectiveFrom:dd MMM yyyy}",
            nameof(FuelPriceUpdatedEvent)));
    }

    // ── Handler: DipVarianceDetected → Email to Admin ───────────
    private async Task HandleDipVariance(IMediator m, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<DipVarianceDetectedEvent>(body, opts)!;
        await m.Send(new SendNotificationCommand(null, null, null,
            NotificationChannel.InApp, "📊 Dip Variance Detected",
            $"Tank {evt.TankId} variance: {evt.VariancePercent:F1}% (Physical: {evt.PhysicalDipLitres}L, System: {evt.SystemStockLitres}L)",
            nameof(DipVarianceDetectedEvent)));
    }

    // ── Handler: UserAccountLocked → Email to User + Admin ──────
    private async Task HandleAccountLocked(IMediator m, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<UserAccountLockedEvent>(body, opts)!;
        await m.Send(new SendNotificationCommand(evt.UserId, evt.Email, null,
            NotificationChannel.InApp, "Account Locked",
            $"Account for {evt.FullName} ({evt.Email}) has been locked. Reason: {evt.Reason}",
            nameof(UserAccountLockedEvent)));
    }

    // ── Handler: PredictedStockDepletion → Notification to Dealer ──
    private async Task HandlePredictedStockDepletion(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<PredictedStockDepletionEvent>(body, opts)!;
        var html = tpl.Render("stock-prediction", new Dictionary<string, string>
        {
            ["TankName"] = evt.TankId.ToString()[..8],
            ["StationName"] = evt.StationId.ToString()[..8],
            ["CurrentStock"] = evt.CurrentStockLitres.ToString("F0"),
            ["AvgDailyConsumption"] = evt.AvgDailyConsumption.ToString("F0"),
            ["DaysUntilEmpty"] = evt.DaysUntilEmpty.ToString("F1"),
            ["PredictedEmptyAt"] = evt.PredictedEmptyAt.ToString("dd MMM yyyy"),
            ["DashboardUrl"] = "http://localhost:4200/dealer/replenishment" // Or reports dashboard
        });
        // We do not have a direct route to UserID for a specific station here unless we fetch from IdentityService
        // But for this example we send to System Admin or assume DealerUserId logic can be extracted via user lookup.
        // We will push as an InApp notification without targeted UserId, relying on global admin roles or similar.
        await m.Send(new SendNotificationCommand(null, null, null,
            NotificationChannel.InApp, $"📈 Stock Prediction Alert: Tank {evt.TankId.ToString()[..8]} empty in {evt.DaysUntilEmpty:F1} days",
            html, nameof(PredictedStockDepletionEvent)));
    }

    // ── Handler: DocumentExpiring → Notification to Admin ─────────
    private async Task HandleDocumentExpiring(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<DocumentExpiringEvent>(body, opts)!;
        var msg = $"Document {evt.FileName} ({evt.DocumentType}) is expiring on {evt.ExpiryDate:d}. Days remaining: {evt.DaysUntilExpiry}.";
        
        await m.Send(new SendNotificationCommand(null, null, null,
            NotificationChannel.InApp, $"📄 Document Expiring: {evt.FileName}",
            msg, "DocumentExpiringEvent"));
    }

    // ── Handler: UserRegistered → Welcome Email to User ────────
    private async Task HandleUserRegistered(IMediator m, IEmailTemplateService tpl, string body, JsonSerializerOptions opts)
    {
        var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(body, opts)!;
        var roleMessage = evt.Role switch
        {
            "Dealer" => "You now have access to pump management, inventory tracking, and sales tools.",
            "Customer" => "Track your fuel purchases, earn loyalty points, and manage your wallet.",
            _ => "You can now explore all EPCL platform features."
        };
        var html = tpl.Render("welcome", new Dictionary<string, string>
        {
            ["RecipientName"] = evt.FullName,
            ["RoleMessage"] = roleMessage,
            ["DashboardUrl"] = "http://localhost:4200/auth/login"
        });
        // Send welcome email
        await m.Send(new SendNotificationCommand(
            evt.UserId, evt.Email, null,
            NotificationChannel.Email,
            "Welcome to EPCL — Eleven Petroleum Corporation Limited",
            html,
            nameof(UserRegisteredEvent)));
        _logger.LogInformation("Welcome email sent to {Email} ({Role})", evt.Email, evt.Role);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync(ct);
        if (_connection?.IsOpen == true) await _connection.CloseAsync(ct);
        _channel?.Dispose(); _connection?.Dispose();
        await base.StopAsync(ct);
    }
}
