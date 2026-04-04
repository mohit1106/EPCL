using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportingService.Domain.Events;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Infrastructure.Messaging;

/// <summary>
/// Consumes SaleCompletedEvent to upsert DailySalesSummaries.
/// Consumes FuelStockLoadedEvent for stock aggregate tracking.
/// </summary>
public class ReportingConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportingConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.reporting.queue";

    public ReportingConsumerHostedService(IServiceScopeFactory scopeFactory,
        ILogger<ReportingConsumerHostedService> logger, IConfiguration config)
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

        await _channel.QueueBindAsync(QueueName, ExchangeName, "sales.completed", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(QueueName, ExchangeName, "inventory.stock.loaded", cancellationToken: stoppingToken);

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

                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                if (eventType == nameof(SaleCompletedEvent))
                {
                    var evt = JsonSerializer.Deserialize<SaleCompletedEvent>(body, jsonOpts)!;
                    var summaryRepo = scope.ServiceProvider.GetRequiredService<IDailySalesSummaryRepository>();
                    await summaryRepo.UpsertAsync(evt.StationId, evt.FuelTypeId,
                        DateOnly.FromDateTime(evt.Timestamp.UtcDateTime),
                        evt.QuantityLitres, evt.TotalAmount);
                    _logger.LogInformation("DailySalesSummary upserted for station {Station}, fuel {Fuel}, date {Date}",
                        evt.StationId, evt.FuelTypeId, evt.Timestamp.Date);
                }
                else if (eventType == nameof(FuelStockLoadedEvent))
                {
                    var evt = JsonSerializer.Deserialize<FuelStockLoadedEvent>(body, jsonOpts)!;
                    _logger.LogInformation("Stock loaded event received for tank {Tank}: {Qty}L", evt.TankId, evt.QuantityLitres);
                    // Stock aggregates can be extended here
                }

                await processedRepo.MarkProcessedAsync(eventId, eventType);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reporting event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Reporting consumer started on queue {Queue}", QueueName);
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
