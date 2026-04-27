using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly ConnectionFactory _factory;

    public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = configuration["RABBITMQ_HOST"] ?? "localhost",
            Port = int.TryParse(configuration["RABBITMQ_PORT"], out var port) ? port : 5672,
            UserName = configuration["RABBITMQ_USER"] ?? "guest",
            Password = configuration["RABBITMQ_PASS"] ?? "guest"
        };
    }

    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _connection = await _factory.CreateConnectionAsync(ct);
        }
        if (_channel == null || !_channel.IsOpen)
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            await _channel.ExchangeDeclareAsync(exchange: "epcl.events", type: ExchangeType.Topic, durable: true, cancellationToken: ct);
        }
    }

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : class
    {
        try
        {
            await EnsureConnectionAsync(ct);

            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await _channel!.BasicPublishAsync(exchange: "epcl.events", routingKey: routingKey, mandatory: false, basicProperties: properties, body: body, cancellationToken: ct);

            _logger.LogInformation("Published event {EventType} to {RoutingKey}", typeof(T).Name, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to {RoutingKey}", typeof(T).Name, routingKey);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel?.IsOpen == true) await _channel.CloseAsync();
        if (_connection?.IsOpen == true) await _connection.CloseAsync();
    }
}
