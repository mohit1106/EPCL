using InventoryService.Domain.Events;

namespace InventoryService.Application.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : IntegrationEvent;
}
