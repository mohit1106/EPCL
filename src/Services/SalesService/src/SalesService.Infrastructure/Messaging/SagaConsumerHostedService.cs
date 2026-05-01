using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalesService.Application.Interfaces;
using SalesService.Domain.Entities;
using SalesService.Domain.Enums;
using SalesService.Domain.Events;
using SalesService.Domain.Interfaces;

namespace SalesService.Infrastructure.Messaging;

/// <summary>
/// Saga consumers for Sales Service:
/// - StockReservedEvent → Step 3a: Complete sale, publish SaleCompletedEvent
/// - StockReservationFailedEvent → Step 3b: Void sale, publish SaleCancelledEvent
/// </summary>
public class SagaConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SagaConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.sales.queue";

    public SagaConsumerHostedService(IServiceScopeFactory scopeFactory, ILogger<SagaConsumerHostedService> logger, IConfiguration config)
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
        await _channel.QueueBindAsync(QueueName, ExchangeName, "inventory.stock.reserved", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "inventory.stock.reservation-failed", cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var eventType = ea.BasicProperties?.Type ?? "";

                using var scope = _scopeFactory.CreateScope();
                var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
                var eventId = Guid.TryParse(ea.BasicProperties?.MessageId, out var eid) ? eid : Guid.NewGuid();

                if (await processedRepo.AlreadyProcessedAsync(eventId))
                {
                    _logger.LogInformation("Skipping duplicate event {EventId} ({EventType})", eventId, eventType);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                switch (eventType)
                {
                    case nameof(StockReservedEvent):
                        await HandleStockReserved(scope.ServiceProvider, body);
                        break;
                    case nameof(StockReservationFailedEvent):
                        await HandleStockReservationFailed(scope.ServiceProvider, body);
                        break;
                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", eventType);
                        break;
                }

                await processedRepo.MarkProcessedAsync(eventId, eventType);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Sales Saga consumer started on queue {Queue}", QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>Saga Step 3a: Stock reserved → Complete transaction, publish SaleCompletedEvent.</summary>
    private async Task HandleStockReserved(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<StockReservedEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var txRepo = sp.GetRequiredService<ITransactionRepository>();
        var publisher = sp.GetRequiredService<IRabbitMqPublisher>();

        var tx = await txRepo.GetByIdAsync(evt.TransactionId);
        if (tx == null) { _logger.LogWarning("Transaction not found: {TxId}", evt.TransactionId); return; }

        // Don't overwrite if already completed (instant payment methods)
        if (tx.Status == TransactionStatus.Completed)
        {
            _logger.LogInformation("Skipping saga completion for already-completed transaction: {TxId}", tx.Id);
            return;
        }

        tx.Status = TransactionStatus.Completed;
        await txRepo.UpdateAsync(tx);

        await publisher.PublishAsync(new SaleCompletedEvent
        {
            EventType = nameof(SaleCompletedEvent),
            TransactionId = tx.Id, StationId = tx.StationId, PumpId = tx.PumpId,
            TankId = evt.TankId, FuelTypeId = tx.FuelTypeId,
            DealerUserId = tx.DealerUserId, CustomerUserId = tx.CustomerUserId,
            VehicleNumber = tx.VehicleNumber, QuantityLitres = tx.QuantityLitres,
            PricePerLitre = tx.PricePerLitre, TotalAmount = tx.TotalAmount,
            PaymentMethod = tx.PaymentMethod.ToString()
        }, "sales.completed");

        _logger.LogInformation("Sale completed. Tx: {TxId}, Receipt: {Receipt}", tx.Id, tx.ReceiptNumber);
    }

    /// <summary>Saga Step 3b: Stock reservation failed → Void transaction, publish SaleCancelledEvent.</summary>
    private async Task HandleStockReservationFailed(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<StockReservationFailedEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var txRepo = sp.GetRequiredService<ITransactionRepository>();
        var publisher = sp.GetRequiredService<IRabbitMqPublisher>();

        var tx = await txRepo.GetByIdAsync(evt.TransactionId);
        if (tx == null) return;

        // Don't void transactions that are already Completed (e.g., Cash/UPI instant payments)
        if (tx.Status == TransactionStatus.Completed)
        {
            _logger.LogInformation("Skipping void for already-completed transaction: {TxId}", tx.Id);
            return;
        }

        tx.Status = TransactionStatus.Voided;
        tx.IsVoided = true;
        await txRepo.UpdateAsync(tx);

        await publisher.PublishAsync(new SaleCancelledEvent
        {
            EventType = nameof(SaleCancelledEvent),
            TransactionId = tx.Id, TankId = evt.TankId,
            ReservedLitres = tx.QuantityLitres, Reason = evt.Reason
        }, "sales.cancelled");

        _logger.LogWarning("Sale cancelled (insufficient stock). Tx: {TxId}, Reason: {Reason}", tx.Id, evt.Reason);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync(ct);
        if (_connection?.IsOpen == true) await _connection.CloseAsync(ct);
        _channel?.Dispose(); _connection?.Dispose();
        await base.StopAsync(ct);
    }
}
