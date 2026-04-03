using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FraudDetectionService.Application.Commands;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.Rules;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Events;
using FraudDetectionService.Domain.Interfaces;

namespace FraudDetectionService.Infrastructure.Messaging;

/// <summary>
/// Consumes SaleCompletedEvent and DipVarianceDetectedEvent from RabbitMQ.
/// On SaleCompleted → runs all 10 fraud rules via MediatR.
/// On DipVarianceDetected → creates DipVariance fraud alert directly.
/// </summary>
public class FraudConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FraudConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.fraud.queue";

    public FraudConsumerHostedService(IServiceScopeFactory scopeFactory, ILogger<FraudConsumerHostedService> logger, IConfiguration config)
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
        await _channel.QueueBindAsync(QueueName, ExchangeName, "inventory.dip.variance", cancellationToken: stoppingToken);
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

                switch (eventType)
                {
                    case nameof(SaleCompletedEvent):
                        await HandleSaleCompleted(scope.ServiceProvider, body);
                        break;
                    case nameof(DipVarianceDetectedEvent):
                        await HandleDipVariance(scope.ServiceProvider, body);
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
                _logger.LogError(ex, "Error processing fraud event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Fraud consumer started on queue {Queue}", QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleSaleCompleted(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<SaleCompletedEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Send(new EvaluateFraudRulesCommand(evt));
        _logger.LogInformation("Fraud evaluation triggered for Tx {TxId}", evt.TransactionId);
    }

    private async Task HandleDipVariance(IServiceProvider sp, string body)
    {
        var evt = JsonSerializer.Deserialize<DipVarianceDetectedEvent>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var dipRule = new DipVarianceRule();
        var (triggered, description) = dipRule.EvaluateDipVariance(evt);

        if (triggered)
        {
            var alertRepo = sp.GetRequiredService<IFraudAlertRepository>();
            var publisher = sp.GetRequiredService<IRabbitMqPublisher>();

            var alert = new FraudAlert
            {
                Id = Guid.NewGuid(), TransactionId = Guid.Empty, StationId = evt.StationId,
                RuleTriggered = "DipVarianceRule", Severity = AlertSeverity.High,
                Description = description, Status = AlertStatus.Open
            };
            await alertRepo.AddAsync(alert);

            await publisher.PublishAsync(new FraudAlertTriggeredEvent
            {
                EventType = nameof(FraudAlertTriggeredEvent),
                AlertId = alert.Id, TransactionId = Guid.Empty, StationId = evt.StationId,
                RuleTriggered = "DipVarianceRule", Severity = "High", Description = description
            }, "fraud.alert.triggered");

            _logger.LogWarning("DipVariance fraud alert created for Station {StationId}: {Desc}", evt.StationId, description);
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync(ct);
        if (_connection?.IsOpen == true) await _connection.CloseAsync(ct);
        _channel?.Dispose(); _connection?.Dispose();
        await base.StopAsync(ct);
    }
}
