using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using InventoryService.Application.Commands;
using InventoryService.Application.Queries;
using InventoryService.Application.DTOs;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Events;
using InventoryService.Domain.Exceptions;
using InventoryService.Domain.Interfaces;

namespace InventoryService.UnitTests;

#region StockLoadingTests

[TestFixture]
public class StockLoadingTests
{
    private Mock<ITankRepository> _tankRepo = null!;
    private Mock<IStockLoadingRepository> _loadingRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    
    [SetUp]
    public void SetUp()
    {
        _tankRepo = new Mock<ITankRepository>();
        _loadingRepo = new Mock<IStockLoadingRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
    }

    [Test]
    public async Task RecordStockLoading_Valid_UpdatesStockAndAddsHistory()
    {
        var logger = new Mock<ILogger<RecordStockLoadingHandler>>();
        var handler = new RecordStockLoadingHandler(_tankRepo.Object, _loadingRepo.Object, _publisher.Object, logger.Object);

        var tankId = Guid.NewGuid();
        var tank = new Tank { Id = tankId, CapacityLitres = 10000, CurrentStockLitres = 2000, MinThresholdLitres = 3000, Status = TankStatus.Low };
        _tankRepo.Setup(r => r.GetByIdAsync(tankId, It.IsAny<CancellationToken>())).ReturnsAsync(tank);

        var cmd = new RecordStockLoadingCommand(tankId, 5000, Guid.NewGuid(), "TN01", "INV123", "Supplier", "Notes");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.StockBefore.Should().Be(2000);
        result.StockAfter.Should().Be(7000);
        tank.CurrentStockLitres.Should().Be(7000);
        tank.Status.Should().Be(TankStatus.Available);

        _loadingRepo.Verify(r => r.AddAsync(It.IsAny<StockLoading>(), It.IsAny<CancellationToken>()), Times.Once);
        _tankRepo.Verify(r => r.UpdateAsync(tank, It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<FuelStockLoadedEvent>(), "inventory.stock.loaded", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RecordStockLoading_ExceedsCapacity_ThrowsInsufficientCapacityException()
    {
        var logger = new Mock<ILogger<RecordStockLoadingHandler>>();
        var handler = new RecordStockLoadingHandler(_tankRepo.Object, _loadingRepo.Object, _publisher.Object, logger.Object);

        var tankId = Guid.NewGuid();
        var tank = new Tank { Id = tankId, CapacityLitres = 10000, CurrentStockLitres = 8000, MinThresholdLitres = 3000 };
        _tankRepo.Setup(r => r.GetByIdAsync(tankId, It.IsAny<CancellationToken>())).ReturnsAsync(tank);

        var cmd = new RecordStockLoadingCommand(tankId, 5000, Guid.NewGuid(), "TN01", "INV123", "Supplier", "Notes");
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientCapacityException>();
    }
}

#endregion

#region DipReadingTests

[TestFixture]
public class DipReadingTests
{
    private Mock<ITankRepository> _tankRepo = null!;
    private Mock<IDipReadingRepository> _dipRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;

    [SetUp]
    public void SetUp()
    {
        _tankRepo = new Mock<ITankRepository>();
        _dipRepo = new Mock<IDipReadingRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
    }

    [Test]
    public async Task RecordDipReading_NormalVariance_DoesNotFlagFraud()
    {
        var logger = new Mock<ILogger<RecordDipReadingHandler>>();
        var handler = new RecordDipReadingHandler(_tankRepo.Object, _dipRepo.Object, _publisher.Object, logger.Object);

        var tankId = Guid.NewGuid();
        var tank = new Tank { Id = tankId, CurrentStockLitres = 5000 };
        _tankRepo.Setup(r => r.GetByIdAsync(tankId, It.IsAny<CancellationToken>())).ReturnsAsync(tank);

        var cmd = new RecordDipReadingCommand(tankId, 4950, Guid.NewGuid(), null); // 1% variance
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.VariancePercent.Should().Be(1.0m);
        result.IsFraudFlagged.Should().BeFalse();
        _publisher.Verify(p => p.PublishAsync(It.IsAny<DipVarianceDetectedEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RecordDipReading_HighVariance_FlagsFraudAndPublishesEvent()
    {
        var logger = new Mock<ILogger<RecordDipReadingHandler>>();
        var handler = new RecordDipReadingHandler(_tankRepo.Object, _dipRepo.Object, _publisher.Object, logger.Object);

        var tankId = Guid.NewGuid();
        var tank = new Tank { Id = tankId, CurrentStockLitres = 5000 };
        _tankRepo.Setup(r => r.GetByIdAsync(tankId, It.IsAny<CancellationToken>())).ReturnsAsync(tank);

        var cmd = new RecordDipReadingCommand(tankId, 4800, Guid.NewGuid(), null); // 4% variance
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.VariancePercent.Should().Be(4.0m);
        result.IsFraudFlagged.Should().BeTrue();
        _publisher.Verify(p => p.PublishAsync(It.IsAny<DipVarianceDetectedEvent>(), "inventory.dip.variance", It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ReplenishmentTests

[TestFixture]
public class ReplenishmentTests
{
    private Mock<IReplenishmentRequestRepository> _replRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;

    [SetUp]
    public void SetUp()
    {
        _replRepo = new Mock<IReplenishmentRequestRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
    }

    [Test]
    public async Task ApproveReplenishment_Valid_UpdatesStatus()
    {
        var logger = new Mock<ILogger<ApproveReplenishmentHandler>>();
        var handler = new ApproveReplenishmentHandler(_replRepo.Object, _publisher.Object, logger.Object);

        var reqId = Guid.NewGuid();
        var req = new ReplenishmentRequest { Id = reqId, Status = ReplenishmentStatus.Submitted };
        _replRepo.Setup(r => r.GetByIdAsync(reqId, It.IsAny<CancellationToken>())).ReturnsAsync(req);

        var cmd = new ApproveReplenishmentCommand(reqId, Guid.NewGuid(), "Approved");
        await handler.Handle(cmd, CancellationToken.None);

        req.Status.Should().Be(ReplenishmentStatus.Approved);
        _replRepo.Verify(r => r.UpdateAsync(req, It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<ReplenishmentApprovedEvent>(), "inventory.replenishment.approved", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ApproveReplenishment_InvalidStatus_ThrowsDomainException()
    {
        var logger = new Mock<ILogger<ApproveReplenishmentHandler>>();
        var handler = new ApproveReplenishmentHandler(_replRepo.Object, _publisher.Object, logger.Object);

        var reqId = Guid.NewGuid();
        var req = new ReplenishmentRequest { Id = reqId, Status = ReplenishmentStatus.Delivered }; // Cannot approve delivered
        _replRepo.Setup(r => r.GetByIdAsync(reqId, It.IsAny<CancellationToken>())).ReturnsAsync(req);

        var cmd = new ApproveReplenishmentCommand(reqId, Guid.NewGuid(), "Approved");
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

#endregion

#region TankQueryTests

[TestFixture]
public class TankQueryTests
{
    private Mock<ITankRepository> _tankRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _tankRepo = new Mock<ITankRepository>();
    }

    [Test]
    public async Task GetLowStockAlerts_ReturnsLowStockTanks()
    {
        _tankRepo.Setup(r => r.GetLowStockTanksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tank> { new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() } });

        var handler = new GetLowStockAlertsHandler(_tankRepo.Object);
        var result = await handler.Handle(new GetLowStockAlertsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetTankById_NotFound_ThrowsNotFoundException()
    {
        _tankRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Tank?)null);

        var handler = new GetTankByIdHandler(_tankRepo.Object);
        var act = () => handler.Handle(new GetTankByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

#endregion
