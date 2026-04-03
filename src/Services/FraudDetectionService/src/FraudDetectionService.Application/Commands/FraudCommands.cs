using MediatR;
using Microsoft.Extensions.Logging;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Events;
using FraudDetectionService.Domain.Exceptions;
using FraudDetectionService.Domain.Interfaces;

namespace FraudDetectionService.Application.Commands;

// ══════════════════════════════════════════════════════════════════
// EvaluateFraudRules — runs ALL 10 rules, creates FraudAlert per triggered rule
// ══════════════════════════════════════════════════════════════════
public record EvaluateFraudRulesCommand(SaleCompletedEvent Transaction) : IRequest<IReadOnlyList<FraudAlertDto>>;

public class EvaluateFraudRulesHandler(
    IEnumerable<IFraudRule> rules,
    IFraudAlertRepository alertRepo,
    IFraudRuleEvaluationRepository evalRepo,
    IRabbitMqPublisher publisher,
    ILogger<EvaluateFraudRulesHandler> logger)
    : IRequestHandler<EvaluateFraudRulesCommand, IReadOnlyList<FraudAlertDto>>
{
    public async Task<IReadOnlyList<FraudAlertDto>> Handle(EvaluateFraudRulesCommand cmd, CancellationToken ct)
    {
        var tx = cmd.Transaction;
        var alerts = new List<FraudAlert>();
        var evaluations = new List<FraudRuleEvaluation>();

        foreach (var rule in rules)
        {
            try
            {
                var (triggered, description) = await rule.EvaluateAsync(tx, ct);
                evaluations.Add(new FraudRuleEvaluation
                {
                    Id = Guid.NewGuid(), TransactionId = tx.TransactionId,
                    RuleName = rule.RuleName, Passed = !triggered, Details = description
                });

                if (triggered)
                {
                    var alert = new FraudAlert
                    {
                        Id = Guid.NewGuid(), TransactionId = tx.TransactionId, StationId = tx.StationId,
                        RuleTriggered = rule.RuleName, Severity = rule.DefaultSeverity,
                        Description = description, Status = AlertStatus.Open
                    };
                    alerts.Add(alert);
                    logger.LogWarning("FRAUD ALERT: {Rule} triggered for Tx {TxId} at Station {StationId}: {Desc}",
                        rule.RuleName, tx.TransactionId, tx.StationId, description);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error evaluating rule {Rule} for Tx {TxId}", rule.RuleName, tx.TransactionId);
                evaluations.Add(new FraudRuleEvaluation
                {
                    Id = Guid.NewGuid(), TransactionId = tx.TransactionId,
                    RuleName = rule.RuleName, Passed = true, Details = $"Error: {ex.Message}"
                });
            }
        }

        await evalRepo.AddRangeAsync(evaluations, ct);
        if (alerts.Count > 0)
        {
            await alertRepo.AddRangeAsync(alerts, ct);
            foreach (var alert in alerts)
            {
                await publisher.PublishAsync(new FraudAlertTriggeredEvent
                {
                    EventType = nameof(FraudAlertTriggeredEvent),
                    AlertId = alert.Id, TransactionId = alert.TransactionId, StationId = alert.StationId,
                    RuleTriggered = alert.RuleTriggered, Severity = alert.Severity.ToString(),
                    Description = alert.Description
                }, "fraud.alert.triggered", ct);
            }
        }

        logger.LogInformation("Fraud evaluation complete for Tx {TxId}: {RulesRun} rules, {AlertsCreated} alerts",
            tx.TransactionId, evaluations.Count, alerts.Count);

        return alerts.Select(MapAlert).ToList();
    }

    private static FraudAlertDto MapAlert(FraudAlert a) => new(
        a.Id, a.TransactionId, a.StationId, a.RuleTriggered,
        a.Severity.ToString(), a.Description, a.Status.ToString(),
        a.ReviewedByUserId, a.ReviewedAt, a.ReviewNotes, a.CreatedAt);
}

// ══════════════════════════════════════════════════════════════════
// DismissAlert
// ══════════════════════════════════════════════════════════════════
public record DismissAlertCommand(Guid AlertId, Guid ReviewedByUserId, string? Notes) : IRequest<MessageResponseDto>;

public class DismissAlertHandler(IFraudAlertRepository repo) : IRequestHandler<DismissAlertCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(DismissAlertCommand cmd, CancellationToken ct)
    {
        var alert = await repo.GetByIdAsync(cmd.AlertId, ct) ?? throw new NotFoundException("FraudAlert", cmd.AlertId);
        alert.Status = AlertStatus.Dismissed;
        alert.ReviewedByUserId = cmd.ReviewedByUserId;
        alert.ReviewedAt = DateTimeOffset.UtcNow;
        alert.ReviewNotes = cmd.Notes;
        await repo.UpdateAsync(alert, ct);
        return new MessageResponseDto("Alert dismissed.");
    }
}

// ══════════════════════════════════════════════════════════════════
// InvestigateAlert
// ══════════════════════════════════════════════════════════════════
public record InvestigateAlertCommand(Guid AlertId, Guid ReviewedByUserId) : IRequest<MessageResponseDto>;

public class InvestigateAlertHandler(IFraudAlertRepository repo) : IRequestHandler<InvestigateAlertCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(InvestigateAlertCommand cmd, CancellationToken ct)
    {
        var alert = await repo.GetByIdAsync(cmd.AlertId, ct) ?? throw new NotFoundException("FraudAlert", cmd.AlertId);
        alert.Status = AlertStatus.UnderReview;
        alert.ReviewedByUserId = cmd.ReviewedByUserId;
        alert.ReviewedAt = DateTimeOffset.UtcNow;
        await repo.UpdateAsync(alert, ct);
        return new MessageResponseDto("Alert marked for investigation.");
    }
}

// ══════════════════════════════════════════════════════════════════
// EscalateAlert
// ══════════════════════════════════════════════════════════════════
public record EscalateAlertCommand(Guid AlertId, Guid ReviewedByUserId, string? Notes) : IRequest<MessageResponseDto>;

public class EscalateAlertHandler(IFraudAlertRepository repo) : IRequestHandler<EscalateAlertCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(EscalateAlertCommand cmd, CancellationToken ct)
    {
        var alert = await repo.GetByIdAsync(cmd.AlertId, ct) ?? throw new NotFoundException("FraudAlert", cmd.AlertId);
        alert.Status = AlertStatus.Escalated;
        alert.ReviewedByUserId = cmd.ReviewedByUserId;
        alert.ReviewedAt = DateTimeOffset.UtcNow;
        alert.ReviewNotes = cmd.Notes;
        await repo.UpdateAsync(alert, ct);
        return new MessageResponseDto("Alert escalated.");
    }
}

// ══════════════════════════════════════════════════════════════════
// BulkDismissAlerts
// ══════════════════════════════════════════════════════════════════
public record BulkDismissAlertsCommand(Guid[] AlertIds, Guid ReviewedByUserId, string? Notes) : IRequest<MessageResponseDto>;

public class BulkDismissAlertsHandler(IFraudAlertRepository repo) : IRequestHandler<BulkDismissAlertsCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(BulkDismissAlertsCommand cmd, CancellationToken ct)
    {
        foreach (var id in cmd.AlertIds)
        {
            var alert = await repo.GetByIdAsync(id, ct);
            if (alert == null) continue;
            alert.Status = AlertStatus.Dismissed;
            alert.ReviewedByUserId = cmd.ReviewedByUserId;
            alert.ReviewedAt = DateTimeOffset.UtcNow;
            alert.ReviewNotes = cmd.Notes;
            await repo.UpdateAsync(alert, ct);
        }
        return new MessageResponseDto($"{cmd.AlertIds.Length} alerts dismissed.");
    }
}
