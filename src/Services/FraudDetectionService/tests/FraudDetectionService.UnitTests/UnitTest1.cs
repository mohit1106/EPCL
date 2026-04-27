using FluentAssertions;
using Moq;
using FraudDetectionService.Application.Interfaces;
using FraudDetectionService.Application.Rules;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Events;
using FraudDetectionService.Domain.Interfaces;

namespace FraudDetectionService.UnitTests;

#region OddHoursRule

[TestFixture]
public class OddHoursRuleTests
{
    private readonly OddHoursRule _rule = new();

    [TestCase(3, true, Description = "3 AM IST should trigger")]
    [TestCase(4, true, Description = "4 AM IST should trigger")]
    [TestCase(5, false, Description = "5 AM IST should not trigger")]
    [TestCase(12, false, Description = "12 PM IST should not trigger")]
    [TestCase(22, false, Description = "10 PM IST should not trigger")]
    [TestCase(23, true, Description = "11 PM IST should trigger")]
    public async Task OddHoursRule_EvaluatesCorrectly(int istHour, bool shouldTrigger)
    {
        var istOffset = TimeSpan.FromHours(5.5);
        var istTime = new DateTimeOffset(2026, 4, 11, istHour, 0, 0, istOffset);
        var tx = FraudTestHelpers.CreateTx(timestamp: istTime);

        var (triggered, _) = await _rule.EvaluateAsync(tx, CancellationToken.None);

        triggered.Should().Be(shouldTrigger);
    }

    [Test]
    public void OddHoursRule_Properties()
    {
        _rule.RuleName.Should().Be("OddHoursRule");
        _rule.DefaultSeverity.Should().Be(AlertSeverity.Medium);
    }
}

#endregion

#region PriceMismatchRule

[TestFixture]
public class PriceMismatchRuleTests
{
    private readonly PriceMismatchRule _rule = new();

    [Test]
    public async Task PriceMismatch_ExactMatch_NotTriggered()
    {
        var tx = FraudTestHelpers.CreateTx(qty: 10, price: 100.5m, total: 1005m);
        var (triggered, _) = await _rule.EvaluateAsync(tx, CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task PriceMismatch_WithinTolerance_NotTriggered()
    {
        var tx = FraudTestHelpers.CreateTx(qty: 10, price: 100.5m, total: 1005.80m); // diff = 0.80
        var (triggered, _) = await _rule.EvaluateAsync(tx, CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task PriceMismatch_OverTolerance_Triggered()
    {
        var tx = FraudTestHelpers.CreateTx(qty: 10, price: 100.5m, total: 1010m);  // diff = 5
        var (triggered, desc) = await _rule.EvaluateAsync(tx, CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("Price mismatch");
    }
}

#endregion

#region RapidTransactionRule

[TestFixture]
public class RapidTransactionRuleTests
{
    [Test]
    public async Task RapidTransaction_5Transactions_NotTriggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var pumpId = Guid.NewGuid();
        repo.Setup(r => r.GetRecentTransactionCountAsync(pumpId, TimeSpan.FromMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        var rule = new RapidTransactionRule(repo.Object);

        var (triggered, _) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(pumpId: pumpId), CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task RapidTransaction_6Transactions_Triggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var pumpId = Guid.NewGuid();
        repo.Setup(r => r.GetRecentTransactionCountAsync(pumpId, TimeSpan.FromMinutes(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);
        var rule = new RapidTransactionRule(repo.Object);

        var (triggered, desc) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(pumpId: pumpId), CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("Rapid transactions");
    }
}

#endregion

#region DipVarianceRule

[TestFixture]
public class DipVarianceRuleTests
{
    [Test]
    public void DipVariance_Over2Percent_Triggered()
    {
        var rule = new DipVarianceRule();
        var evt = new DipVarianceDetectedEvent
        {
            TankId = Guid.NewGuid(),
            VariancePercent = 3.5m,
            PhysicalDipLitres = 4800,
            SystemStockLitres = 5000
        };

        var (triggered, desc) = rule.EvaluateDipVariance(evt);
        triggered.Should().BeTrue();
        desc.Should().Contain("3.5%");
    }

    [Test]
    public void DipVariance_Under2Percent_NotTriggered()
    {
        var rule = new DipVarianceRule();
        var evt = new DipVarianceDetectedEvent
        {
            TankId = Guid.NewGuid(),
            VariancePercent = 1.5m,
            PhysicalDipLitres = 4925,
            SystemStockLitres = 5000
        };

        var (triggered, _) = rule.EvaluateDipVariance(evt);
        triggered.Should().BeFalse();
    }
}

#endregion

#region RoundNumberRule

[TestFixture]
public class RoundNumberRuleTests
{
    [Test]
    public async Task RoundNumber_WithDecimal_NotTriggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var rule = new RoundNumberRule(repo.Object);

        var tx = FraudTestHelpers.CreateTx(qty: 10.5m);  // has decimal
        var (triggered, _) = await rule.EvaluateAsync(tx, CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task RoundNumber_AllRound_Triggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var pumpId = Guid.NewGuid();
        repo.Setup(r => r.AreLastNTransactionsRoundNumbersAsync(pumpId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var rule = new RoundNumberRule(repo.Object);

        var tx = FraudTestHelpers.CreateTx(pumpId: pumpId, qty: 10m); // round number
        var (triggered, desc) = await rule.EvaluateAsync(tx, CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("round numbers");
    }
}

#endregion

#region DuplicateTransactionRule

[TestFixture]
public class DuplicateTransactionRuleTests
{
    [Test]
    public async Task Duplicate_Found_Triggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var pumpId = Guid.NewGuid();
        repo.Setup(r => r.HasDuplicateTransactionAsync("MH12AB1234", pumpId, 10m, TimeSpan.FromMinutes(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var rule = new DuplicateTransactionRule(repo.Object);

        var (triggered, desc) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(pumpId: pumpId, vehicle: "MH12AB1234", qty: 10m), CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("Duplicate");
    }

    [Test]
    public async Task NoDuplicate_NotTriggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        repo.Setup(r => r.HasDuplicateTransactionAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var rule = new DuplicateTransactionRule(repo.Object);

        var (triggered, _) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(), CancellationToken.None);
        triggered.Should().BeFalse();
    }
}

#endregion

#region VoidPatternRule

[TestFixture]
public class VoidPatternRuleTests
{
    [Test]
    public async Task VoidPattern_3Voids_NotTriggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        repo.Setup(r => r.GetDailyVoidCountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        var rule = new VoidPatternRule(repo.Object);

        var (triggered, _) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(), CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task VoidPattern_4Voids_Triggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        repo.Setup(r => r.GetDailyVoidCountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        var rule = new VoidPatternRule(repo.Object);

        var (triggered, desc) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(), CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("Void pattern");
    }
}

#endregion

#region VolumeSpikeRule

[TestFixture]
public class VolumeSpikeRuleTests
{
    [Test]
    public async Task VolumeSpike_Under150Pct_NotTriggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var stationId = Guid.NewGuid();
        repo.Setup(r => r.GetDailyStationVolumeAsync(stationId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(14000m);
        repo.Setup(r => r.GetAverageVolumeForDayOfWeekAsync(stationId, It.IsAny<DayOfWeek>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10000m);  // 14000 < 15000 (150%)
        var rule = new VolumeSpikeRule(repo.Object);

        var (triggered, _) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(stationId: stationId), CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task VolumeSpike_Over150Pct_Triggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        var stationId = Guid.NewGuid();
        repo.Setup(r => r.GetDailyStationVolumeAsync(stationId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(16000m);
        repo.Setup(r => r.GetAverageVolumeForDayOfWeekAsync(stationId, It.IsAny<DayOfWeek>(), 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10000m); // 16000 > 15000 (150%)
        var rule = new VolumeSpikeRule(repo.Object);

        var (triggered, desc) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(stationId: stationId), CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("Volume spike");
    }
}

#endregion

#region NewDealerRule

[TestFixture]
public class NewDealerRuleTests
{
    [Test]
    public async Task NewDealer_100Tx_NotTriggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        repo.Setup(r => r.GetDailyTransactionCountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);
        var rule = new NewDealerRule(repo.Object);

        var (triggered, _) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(), CancellationToken.None);
        triggered.Should().BeFalse();
    }

    [Test]
    public async Task NewDealer_101Tx_Triggered()
    {
        var repo = new Mock<IFraudRuleEvaluationRepository>();
        repo.Setup(r => r.GetDailyTransactionCountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101);
        var rule = new NewDealerRule(repo.Object);

        var (triggered, desc) = await rule.EvaluateAsync(FraudTestHelpers.CreateTx(), CancellationToken.None);
        triggered.Should().BeTrue();
        desc.Should().Contain("New dealer alert");
    }
}

#endregion

#region Test Helpers

public static class FraudTestHelpers
{
    public static SaleCompletedEvent CreateTx(
        Guid? stationId = null, Guid? pumpId = null, string vehicle = "MH12AB1234",
        decimal qty = 10, decimal price = 100, decimal total = 1000,
        DateTimeOffset? timestamp = null) => new()
    {
        TransactionId = Guid.NewGuid(),
        StationId = stationId ?? Guid.NewGuid(),
        PumpId = pumpId ?? Guid.NewGuid(),
        VehicleNumber = vehicle,
        QuantityLitres = qty,
        PricePerLitre = price,
        TotalAmount = total,
        DealerUserId = Guid.NewGuid(),
        Timestamp = timestamp ?? DateTimeOffset.UtcNow
    };
}

#endregion
