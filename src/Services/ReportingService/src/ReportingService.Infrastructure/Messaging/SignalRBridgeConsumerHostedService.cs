using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportingService.Domain.Events;

namespace ReportingService.Infrastructure.Messaging;

/// <summary>
/// Consumes 4 event types and pushes real-time notifications via SignalR:
///   1. fraud.alert.triggered → AdminHub "NewFraudAlert"
///   2. inventory.stock.critical → AdminHub "StockCritical" + DealerHub Station-{id}
///   3. inventory.replenishment.requested → AdminHub "ReplenishmentRequested"
///   4. sales.price.updated → DealerHub AllDealers "FuelPriceUpdated"
/// </summary>
public class SignalRBridgeConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SignalRBridgeConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.signalr.bridge.queue";

    public SignalRBridgeConsumerHostedService(IServiceScopeFactory scopeFactory,
        ILogger<SignalRBridgeConsumerHostedService> logger, IConfiguration config)
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

        // Bind to all 4 event routing keys
        await _channel.QueueBindAsync(QueueName, ExchangeName, "fraud.alert.triggered", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "inventory.stock.critical", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "inventory.replenishment.requested", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "sales.price.updated", cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 20, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey;
                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var scope = _scopeFactory.CreateScope();
                var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

                switch (routingKey)
                {
                    case "fraud.alert.triggered":
                    {
                        var evt = JsonSerializer.Deserialize<FraudAlertTriggeredEvent>(body, jsonOpts)!;
                        await signalR.SendFraudAlertAsync(new
                        {
                            evt.AlertId, evt.TransactionId, evt.StationId,
                            evt.RuleName, evt.Severity, evt.Description, evt.Timestamp
                        });
                        _logger.LogInformation("Pushed NewFraudAlert: {Rule} ({Severity})", evt.RuleName, evt.Severity);
                        break;
                    }

                    case "inventory.stock.critical":
                    {
                        var evt = JsonSerializer.Deserialize<StockLevelCriticalEvent>(body, jsonOpts)!;
                        var payload = new
                        {
                            evt.TankId, evt.StationId, evt.CurrentStock,
                            evt.CapacityLitres, evt.FuelTypeId, evt.Timestamp
                        };
                        await signalR.SendStockCriticalToAdminAsync(payload);
                        await signalR.SendStockCriticalToStationAsync(evt.StationId.ToString(), payload);
                        _logger.LogInformation("Pushed StockCritical to Admin + Station-{Station}", evt.StationId);
                        break;
                    }

                    case "inventory.replenishment.requested":
                    {
                        var evt = JsonSerializer.Deserialize<ReplenishmentRequestedEvent>(body, jsonOpts)!;
                        await signalR.SendReplenishmentRequestedAsync(new
                        {
                            evt.RequestId, evt.StationId, evt.TankId,
                            evt.UrgencyLevel, evt.RequestedByUserId, evt.Timestamp
                        });
                        _logger.LogInformation("Pushed ReplenishmentRequested: {Request}", evt.RequestId);
                        break;
                    }

                    case "sales.price.updated":
                    {
                        var evt = JsonSerializer.Deserialize<FuelPriceUpdatedEvent>(body, jsonOpts)!;
                        await signalR.SendFuelPriceUpdatedAsync(new
                        {
                            evt.FuelTypeId, evt.FuelTypeName, evt.OldPrice,
                            evt.NewPrice, evt.EffectiveFrom, evt.Timestamp
                        });
                        _logger.LogInformation("Pushed FuelPriceUpdated: {Fuel} {Old}→{New}",
                            evt.FuelTypeName, evt.OldPrice, evt.NewPrice);
                        break;
                    }
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SignalR bridge event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("SignalR bridge consumer started — listening on 4 event types");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync(ct);
        if (_connection?.IsOpen == true) await _connection.CloseAsync(ct);
        _channel?.Dispose(); _connection?.Dispose();
        await base.StopAsync(ct);
    }
}
