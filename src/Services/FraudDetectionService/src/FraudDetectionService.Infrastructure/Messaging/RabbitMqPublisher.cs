using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Domain.Events;

namespace FraudDetectionService.Infrastructure.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "epcl.events";

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = config["RABBITMQ_HOST"] ?? "localhost",
            Port = int.Parse(config["RABBITMQ_PORT"] ?? "5672"),
            UserName = config["RABBITMQ_USER"] ?? "guest",
            Password = config["RABBITMQ_PASS"] ?? "guest"
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : IntegrationEvent
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var props = new BasicProperties
        {
            ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent,
            MessageId = @event.EventId.ToString(), Type = @event.EventType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };
        await _channel.BasicPublishAsync(ExchangeName, routingKey, false, props, body, ct);
        _logger.LogInformation("Published {EventType} → {RoutingKey}", @event.EventType, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel.IsOpen) await _channel.CloseAsync();
        if (_connection.IsOpen) await _connection.CloseAsync();
        _channel.Dispose(); _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
