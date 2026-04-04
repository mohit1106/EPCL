using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LoyaltyService.Application.Commands;
using LoyaltyService.Domain.Events;
using LoyaltyService.Domain.Interfaces;

namespace LoyaltyService.Infrastructure.Messaging;

/// <summary>
/// Consumes SaleCompletedEvent → auto-earns loyalty points for the customer.
/// Rule: 1 point per ₹10 of TotalAmount (rounded down).
/// Only processes if CustomerUserId is present (cash sales without customer link are skipped).
/// </summary>
public class LoyaltyConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoyaltyConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.loyalty.queue";

    public LoyaltyConsumerHostedService(IServiceScopeFactory scopeFactory,
        ILogger<LoyaltyConsumerHostedService> logger, IConfiguration config)
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

        await _channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var evt = JsonSerializer.Deserialize<SaleCompletedEvent>(body, jsonOpts);

                if (evt == null || evt.CustomerUserId == null || evt.CustomerUserId == Guid.Empty)
                {
                    // No customer linked — skip loyalty points
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();

                if (await processedRepo.AlreadyProcessedAsync(evt.EventId))
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new EarnPointsCommand(
                    evt.CustomerUserId.Value, evt.TransactionId, evt.TotalAmount,
                    $"Purchase at station {evt.StationId.ToString()[..8]}"));

                await processedRepo.MarkProcessedAsync(evt.EventId, nameof(SaleCompletedEvent));
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Loyalty points earned for customer {Customer} from sale {Sale}",
                    evt.CustomerUserId, evt.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SaleCompletedEvent for loyalty");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Loyalty consumer started on queue {Queue}", QueueName);
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
