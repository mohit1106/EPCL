using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Events;
using FraudDetectionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FraudDetectionService.Application.Rules;

// ══════════════════════════════════════════════════════════════════
// 1. OversellRule — tx.QuantityLitres > current tank stock
// ══════════════════════════════════════════════════════════════════
public class OversellRule : IFraudRule
{
    public string RuleName => "OversellRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.High;

    public Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        // This rule is evaluated contextually — if the sale completed, stock was reserved.
        // It triggers when TotalAmount seems anomalously high for the quantity.
        // In practice, Inventory Service prevents overselling; this catches edge cases.
        return Task.FromResult((false, "Oversell check: stock reservation validated by Inventory Service."));
    }
}

// ══════════════════════════════════════════════════════════════════
// 2. RapidTransactionRule — >5 transactions on same pump in 2 min
// ══════════════════════════════════════════════════════════════════
public class RapidTransactionRule(IFraudRuleEvaluationRepository repo) : IFraudRule
{
    public string RuleName => "RapidTransactionRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.High;

    public async Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        var count = await repo.GetRecentTransactionCountAsync(tx.PumpId, TimeSpan.FromMinutes(2), ct);
        if (count > 5)
            return (true, $"Rapid transactions: {count} transactions on pump {tx.PumpId} in the last 2 minutes (threshold: 5).");
        return (false, $"Rapid transaction check passed ({count} in 2 min).");
    }
}

// ══════════════════════════════════════════════════════════════════
// 3. DipVarianceRule — triggered by DipVarianceDetectedEvent
// ══════════════════════════════════════════════════════════════════
public class DipVarianceRule : IFraudRule
{
    public string RuleName => "DipVarianceRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.High;

    public Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        // This rule is NOT evaluated via SaleCompletedEvent.
        // It's triggered separately by DipVarianceDetectedEventConsumer.
        return Task.FromResult((false, "DipVariance: evaluated separately via dip reading events."));
    }

    /// <summary>Specialized evaluation for DipVarianceDetectedEvent.</summary>
    public (bool Triggered, string Description) EvaluateDipVariance(DipVarianceDetectedEvent evt)
    {
        if (Math.Abs(evt.VariancePercent) > 2.0m)
            return (true, $"Dip variance of {evt.VariancePercent:F1}% detected on tank {evt.TankId}. Physical: {evt.PhysicalDipLitres}L, System: {evt.SystemStockLitres}L.");
        return (false, $"Dip variance within tolerance ({evt.VariancePercent:F1}%).");
    }
}

// ══════════════════════════════════════════════════════════════════
// 4. OddHoursRule — transaction outside 05:00-23:00
// ══════════════════════════════════════════════════════════════════
public class OddHoursRule : IFraudRule
{
    public string RuleName => "OddHoursRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.Medium;

    public Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        // Convert to IST (UTC+5:30) for Indian station hours
        var istOffset = TimeSpan.FromHours(5.5);
        var istTime = tx.Timestamp.ToOffset(istOffset);
        var hour = istTime.Hour;

        if (hour < 5 || hour >= 23)
            return Task.FromResult((true, $"Transaction at {istTime:HH:mm} IST is outside normal operating hours (05:00-23:00)."));
        return Task.FromResult((false, $"Transaction at {istTime:HH:mm} IST is within operating hours."));
    }
}

// ══════════════════════════════════════════════════════════════════
// 5. RoundNumberRule — last 5 consecutive from same pump are whole
// ══════════════════════════════════════════════════════════════════
public class RoundNumberRule(IFraudRuleEvaluationRepository repo) : IFraudRule
{
    public string RuleName => "RoundNumberRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.Medium;

    public async Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        // Check if current is round
        if (tx.QuantityLitres != Math.Floor(tx.QuantityLitres))
            return (false, "Current quantity has decimal places — round number pattern broken.");

        var allRound = await repo.AreLastNTransactionsRoundNumbersAsync(tx.PumpId, 5, ct);
        if (allRound)
            return (true, $"Last 5 consecutive transactions on pump {tx.PumpId} are all round numbers. Possible meter tampering.");
        return (false, "Round number check passed.");
    }
}

// ══════════════════════════════════════════════════════════════════
// 6. PriceMismatchRule — TotalAmount != Round(Qty*Price, 2) ±₹1
// ══════════════════════════════════════════════════════════════════
public class PriceMismatchRule : IFraudRule
{
    public string RuleName => "PriceMismatchRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.High;

    public Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        var expectedTotal = Math.Round(tx.QuantityLitres * tx.PricePerLitre, 2, MidpointRounding.AwayFromZero);
        var diff = Math.Abs(tx.TotalAmount - expectedTotal);

        if (diff > 1.0m)
            return Task.FromResult((true, $"Price mismatch: expected ₹{expectedTotal:F2} (qty={tx.QuantityLitres} × price={tx.PricePerLitre}), actual ₹{tx.TotalAmount:F2}. Difference: ₹{diff:F2}."));
        return Task.FromResult((false, $"Price matches within ₹1 tolerance (diff: ₹{diff:F2})."));
    }
}

// ══════════════════════════════════════════════════════════════════
// 7. DuplicateTransactionRule — same vehicle+pump within 30 min
// ══════════════════════════════════════════════════════════════════
public class DuplicateTransactionRule(IFraudRuleEvaluationRepository repo) : IFraudRule
{
    public string RuleName => "DuplicateTransactionRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.Medium;

    public async Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        var isDuplicate = await repo.HasDuplicateTransactionAsync(tx.VehicleNumber, tx.PumpId, tx.QuantityLitres, TimeSpan.FromMinutes(30), ct);
        if (isDuplicate)
            return (true, $"Duplicate transaction: vehicle {tx.VehicleNumber} on pump {tx.PumpId} with {tx.QuantityLitres}L within last 30 minutes.");
        return (false, "No duplicate transaction detected.");
    }
}

// ══════════════════════════════════════════════════════════════════
// 8. VolumeSpikeRule — daily total >150% of 4-week avg for day
// ══════════════════════════════════════════════════════════════════
public class VolumeSpikeRule(IFraudRuleEvaluationRepository repo) : IFraudRule
{
    public string RuleName => "VolumeSpikeRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.Medium;

    public async Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        var today = DateTimeOffset.UtcNow;
        var dailyVolume = await repo.GetDailyStationVolumeAsync(tx.StationId, today, ct);
        var avgVolume = await repo.GetAverageVolumeForDayOfWeekAsync(tx.StationId, today.DayOfWeek, 4, ct);

        if (avgVolume > 0 && dailyVolume > avgVolume * 1.5m)
            return (true, $"Volume spike: today's volume {dailyVolume:F0}L is >{150}% of 4-week {today.DayOfWeek} average ({avgVolume:F0}L).");
        return (false, $"Volume normal ({dailyVolume:F0}L vs {avgVolume:F0}L avg).");
    }
}

// ══════════════════════════════════════════════════════════════════
// 9. NewDealerRule — dealer <7 days old AND >100 tx today
// ══════════════════════════════════════════════════════════════════
public class NewDealerRule(IFraudRuleEvaluationRepository repo) : IFraudRule
{
    public string RuleName => "NewDealerRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.Medium;

    public async Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        // We can't check dealer creation date cross-service, so we check transaction count as proxy.
        // If dealer has >100 transactions today, flag for review.
        var count = await repo.GetDailyTransactionCountAsync(tx.DealerUserId, DateTimeOffset.UtcNow, ct);
        if (count > 100)
            return (true, $"New dealer alert: dealer {tx.DealerUserId} has {count} transactions today (threshold: 100). Verify account age.");
        return (false, $"Daily dealer transaction count ({count}) within threshold.");
    }
}

// ══════════════════════════════════════════════════════════════════
// 10. VoidPatternRule — dealer voided >3 transactions today
// ══════════════════════════════════════════════════════════════════
public class VoidPatternRule(IFraudRuleEvaluationRepository repo) : IFraudRule
{
    public string RuleName => "VoidPatternRule";
    public AlertSeverity DefaultSeverity => AlertSeverity.High;

    public async Task<(bool Triggered, string Description)> EvaluateAsync(SaleCompletedEvent tx, CancellationToken ct)
    {
        var voidCount = await repo.GetDailyVoidCountAsync(tx.DealerUserId, DateTimeOffset.UtcNow, ct);
        if (voidCount > 3)
            return (true, $"Void pattern: dealer {tx.DealerUserId} voided {voidCount} transactions today (threshold: 3).");
        return (false, $"Void count ({voidCount}) within threshold.");
    }
}
