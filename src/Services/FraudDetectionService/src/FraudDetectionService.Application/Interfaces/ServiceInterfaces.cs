using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Events;

namespace FraudDetectionService.Application.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default) where T : IntegrationEvent;
}

/// <summary>
/// Each fraud rule implements this interface. Rules are evaluated
/// against a completed sale to determine potential fraud.
/// </summary>
public interface IFraudRule
{
    string RuleName { get; }
    AlertSeverity DefaultSeverity { get; }

    /// <summary>
    /// Returns true if the rule TRIGGERS (fraud detected).
    /// Returns false if the transaction passes the check (no fraud).
    /// The description output explains WHY the rule triggered.
    /// </summary>
    Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct = default);
}
