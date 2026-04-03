using StationService.Domain.Events;

namespace StationService.Application.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : IntegrationEvent;
}
