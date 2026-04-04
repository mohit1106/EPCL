using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using AuditService.Application.Commands;
using AuditService.Domain.Events;

namespace AuditService.Infrastructure.Messaging;

/// <summary>
/// Consumes ALL audit events via the audit.# wildcard routing key.
/// Every service publishes audit events when it modifies entities:
///   audit.user.created, audit.transaction.created, audit.tank.updated, etc.
/// </summary>
public class AuditEventConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditEventConsumerHostedService> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "epcl.events";
    private const string QueueName = "epcl.audit.queue";

    public AuditEventConsumerHostedService(IServiceScopeFactory scopeFactory,
        ILogger<AuditEventConsumerHostedService> logger, IConfiguration config)
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

        // Bind to ALL audit events using wildcard routing key
        await _channel.QueueBindAsync(QueueName, ExchangeName, "audit.#", cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 20, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var auditEvent = JsonSerializer.Deserialize<AuditEvent>(body, jsonOpts);

                if (auditEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize audit event, discarding");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.Send(new AppendAuditLogCommand(
                    auditEvent.EventId,
                    auditEvent.EntityType,
                    auditEvent.EntityId,
                    auditEvent.Operation,
                    auditEvent.OldValues,
                    auditEvent.NewValues,
                    auditEvent.ChangedByUserId,
                    auditEvent.ChangedByRole,
                    auditEvent.IpAddress,
                    auditEvent.CorrelationId,
                    auditEvent.ServiceName,
                    auditEvent.Timestamp));

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audit event");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("Audit consumer started — listening on audit.# wildcard");
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
