using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Events;
using InventoryService.Domain.Interfaces;

namespace InventoryService.Infrastructure.Messaging;

/// <summary>
/// Saga consumers for Inventory Service.
/// - SaleInitiatedEvent → Step 2: Reserve stock
/// - SaleCompletedEvent → Step 4: Deduct stock permanently
/// - SaleCancelledEvent → Step 4b: Release reserved stock
/// </summary>
public class SagaConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SagaConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.inventory.queue";

    public SagaConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SagaConsumerHostedService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

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

        // Bind to events we consume
        await _channel.QueueBindAsync(QueueName, ExchangeName, "sales.initiated", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "sales.completed", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "sales.cancelled", cancellationToken: stoppingToken);

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
                    case nameof(SaleInitiatedEvent):
                        await HandleSaleInitiated(scope.ServiceProvider, body);
                        break;
                    case nameof(SaleCompletedEvent):
                        await HandleSaleCompleted(scope.ServiceProvider, body);
                        break;
                    case nameof(SaleCancelledEvent):
                        await HandleSaleCancelled(scope.ServiceProvider, body);
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
        _logger.LogInformation("Inventory Saga consumer started on queue {Queue}", QueueName);

        // Keep alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>Saga Step 2: Reserve stock for sale.</summary>
    private async Task HandleSaleInitiated(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<SaleInitiatedEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var tankRepo = sp.GetRequiredService<ITankRepository>();
        var publisher = sp.GetRequiredService<IRabbitMqPublisher>();

        var tank = await tankRepo.GetByIdAsync(evt.TankId);
        
        // Fallback: If TankId was empty (from frontend), find tank by StationId and FuelTypeId
        if (tank == null)
        {
            var stationTanks = await tankRepo.GetByStationIdAsync(evt.StationId);
            tank = stationTanks.FirstOrDefault(t => t.FuelTypeId == evt.FuelTypeId);
        }

        if (tank == null)
        {
            await publisher.PublishAsync(new StockReservationFailedEvent
            {
                EventType = nameof(StockReservationFailedEvent),
                TransactionId = evt.TransactionId, TankId = evt.TankId,
                Reason = "Tank not found"
            }, "inventory.stock.reservation-failed");
            return;
        }

        // Rule 7.2: Only reserve if available stock >= requested
        if (tank.AvailableStock < evt.QuantityLitres)
        {
            await publisher.PublishAsync(new StockReservationFailedEvent
            {
                EventType = nameof(StockReservationFailedEvent),
                TransactionId = evt.TransactionId, TankId = evt.TankId,
                Reason = $"Insufficient stock. Available: {tank.AvailableStock}L, Requested: {evt.QuantityLitres}L"
            }, "inventory.stock.reservation-failed");
            _logger.LogWarning("Stock reservation failed. Tank: {TankId}, Available: {Avail}L, Requested: {Req}L",
                tank.Id, tank.AvailableStock, evt.QuantityLitres);
            return;
        }

        tank.ReservedLitres += evt.QuantityLitres;
        tank.UpdatedAt = DateTimeOffset.UtcNow;
        await tankRepo.UpdateAsync(tank);

        await publisher.PublishAsync(new StockReservedEvent
        {
            EventType = nameof(StockReservedEvent),
            TransactionId = evt.TransactionId, TankId = evt.TankId,
            ReservedLitres = evt.QuantityLitres
        }, "inventory.stock.reserved");

        _logger.LogInformation("Stock reserved. Tank: {TankId}, Qty: {Qty}L, Transaction: {TxId}",
            tank.Id, evt.QuantityLitres, evt.TransactionId);
    }

    /// <summary>Saga Step 4: Deduct stock permanently after sale completes.</summary>
    private async Task HandleSaleCompleted(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<SaleCompletedEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var tankRepo = sp.GetRequiredService<ITankRepository>();
        var publisher = sp.GetRequiredService<IRabbitMqPublisher>();

        var tank = await tankRepo.GetByIdAsync(evt.TankId);
        
        // Fallback: If TankId was empty (from frontend), find tank by StationId and FuelTypeId
        if (tank == null)
        {
            var stationTanks = await tankRepo.GetByStationIdAsync(evt.StationId);
            tank = stationTanks.FirstOrDefault(t => t.FuelTypeId == evt.FuelTypeId);
        }

        if (tank == null) 
        {
            sp.GetRequiredService<ILogger<SagaConsumerHostedService>>()
                .LogWarning("SaleCompletedEvent: Tank not found for StationId: {StationId}, FuelTypeId: {FuelTypeId}", evt.StationId, evt.FuelTypeId);
            return;
        }

        // Deduct from actual stock and release reservation
        tank.CurrentStockLitres -= evt.QuantityLitres;
        tank.ReservedLitres = Math.Max(0, tank.ReservedLitres - evt.QuantityLitres);
        tank.UpdatedAt = DateTimeOffset.UtcNow;

        // Rule 7.2: Check stock thresholds after deduction
        if (tank.CurrentStockLitres <= 0)
        {
            tank.Status = TankStatus.OutOfStock;
            await publisher.PublishAsync(new StockOutOfFuelEvent
            {
                EventType = nameof(StockOutOfFuelEvent),
                TankId = tank.Id, StationId = tank.StationId, FuelTypeId = tank.FuelTypeId
            }, "inventory.stock.out");
        }
        else if (tank.CurrentStockLitres < tank.CapacityLitres * 0.10m)
        {
            tank.Status = TankStatus.Critical;
            await publisher.PublishAsync(new StockLevelCriticalEvent
            {
                EventType = nameof(StockLevelCriticalEvent),
                TankId = tank.Id, StationId = tank.StationId,
                CurrentStock = tank.CurrentStockLitres, CapacityLitres = tank.CapacityLitres
            }, "inventory.stock.critical");
        }
        else if (tank.CurrentStockLitres < tank.MinThresholdLitres)
        {
            tank.Status = TankStatus.Low;
            await publisher.PublishAsync(new StockLevelLowEvent
            {
                EventType = nameof(StockLevelLowEvent),
                TankId = tank.Id, StationId = tank.StationId,
                CurrentStock = tank.CurrentStockLitres,
                Threshold = tank.MinThresholdLitres, FuelTypeId = tank.FuelTypeId
            }, "inventory.stock.low");
        }

        await tankRepo.UpdateAsync(tank);

        _logger.LogInformation("Stock deducted. Tank: {TankId}, Qty: {Qty}L, Remaining: {Stock}L, Status: {Status}",
            tank.Id, evt.QuantityLitres, tank.CurrentStockLitres, tank.Status);
    }

    /// <summary>Saga Step 4b: Release reserved stock (compensation).</summary>
    private async Task HandleSaleCancelled(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<SaleCancelledEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var tankRepo = sp.GetRequiredService<ITankRepository>();
        var tank = await tankRepo.GetByIdAsync(evt.TankId);
        if (tank == null) return;

        tank.ReservedLitres = Math.Max(0, tank.ReservedLitres - evt.ReservedLitres);
        tank.UpdatedAt = DateTimeOffset.UtcNow;
        await tankRepo.UpdateAsync(tank);

        _logger.LogInformation("Stock reservation released (compensation). Tank: {TankId}, Released: {Qty}L",
            tank.Id, evt.ReservedLitres);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync(ct);
        if (_connection?.IsOpen == true) await _connection.CloseAsync(ct);
        _channel?.Dispose();
        _connection?.Dispose();
        await base.StopAsync(ct);
    }
}
