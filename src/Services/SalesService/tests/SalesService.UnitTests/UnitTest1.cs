using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using SalesService.Application.Commands;
using SalesService.Application.DTOs;
using SalesService.Application.Interfaces;
using SalesService.Application.Queries;
using SalesService.Domain.Entities;
using SalesService.Domain.Enums;
using SalesService.Domain.Events;
using SalesService.Domain.Exceptions;
using SalesService.Domain.Interfaces;

namespace SalesService.UnitTests;

#region RecordFuelSale Tests

[TestFixture]
public class RecordFuelSaleHandlerTests
{
    private Mock<ITransactionRepository> _txRepo = null!;
    private Mock<IPumpRepository> _pumpRepo = null!;
    private Mock<IFuelPriceRepository> _priceRepo = null!;
    private Mock<IRabbitMqPublisher> _publisher = null!;
    private Mock<IFuelPreAuthorizationRepository> _preAuthRepo = null!;
    private Mock<ILogger<RecordFuelSaleHandler>> _logger = null!;
    private RecordFuelSaleHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _txRepo = new Mock<ITransactionRepository>();
        _pumpRepo = new Mock<IPumpRepository>();
        _priceRepo = new Mock<IFuelPriceRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();
        _preAuthRepo = new Mock<IFuelPreAuthorizationRepository>();
        _logger = new Mock<ILogger<RecordFuelSaleHandler>>();
        _handler = new RecordFuelSaleHandler(_txRepo.Object, _pumpRepo.Object, _priceRepo.Object, _publisher.Object, _preAuthRepo.Object, _logger.Object);
    }

    private static RecordFuelSaleCommand CreateValidCommand(Guid? pumpId = null, Guid? stationId = null) => new(
        stationId ?? Guid.NewGuid(), pumpId ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), Guid.NewGuid(), "MH12AB1234", 10.5m, "Cash", null);

    [Test]
    public async Task Handle_ValidSale_ReturnsTransactionDto()
    {
        var cmd = CreateValidCommand();
        _pumpRepo.Setup(r => r.GetByIdAsync(cmd.PumpId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Pump { Id = cmd.PumpId, Status = PumpStatus.Active, StationId = cmd.StationId });
        _priceRepo.Setup(r => r.GetActivePriceAsync(cmd.FuelTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new FuelPrice { PricePerLitre = 100.50m, IsActive = true });
        _txRepo.Setup(r => r.GetDailySequenceAsync(cmd.StationId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(0);
        _txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Transaction t, CancellationToken _) => t);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.QuantityLitres.Should().Be(10.5m);
        result.TotalAmount.Should().Be(Math.Round(10.5m * 100.50m, 2, MidpointRounding.AwayFromZero));
        _publisher.Verify(p => p.PublishAsync(It.IsAny<SaleInitiatedEvent>(), "sales.initiated", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_PumpNotFound_ThrowsNotFoundException()
    {
        var cmd = CreateValidCommand();
        _pumpRepo.Setup(r => r.GetByIdAsync(cmd.PumpId, It.IsAny<CancellationToken>())).ReturnsAsync((Pump?)null);

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task Handle_InactivePump_ThrowsPumpNotActive()
    {
        var cmd = CreateValidCommand();
        _pumpRepo.Setup(r => r.GetByIdAsync(cmd.PumpId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Pump { Id = cmd.PumpId, Status = PumpStatus.UnderMaintenance, StationId = cmd.StationId });

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<PumpNotActiveException>();
    }

    [Test]
    public async Task Handle_NoPriceAvailable_ThrowsDomainException()
    {
        var cmd = CreateValidCommand();
        _pumpRepo.Setup(r => r.GetByIdAsync(cmd.PumpId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Pump { Id = cmd.PumpId, Status = PumpStatus.Active, StationId = cmd.StationId });
        _priceRepo.Setup(r => r.GetActivePriceAsync(cmd.FuelTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync((FuelPrice?)null);

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*No active price*");
    }

    [Test]
    public async Task Handle_InvalidPaymentMethod_ThrowsDomainException()
    {
        var cmd = new RecordFuelSaleCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), "MH12AB1234", 10m, "Bitcoin", null);

        _pumpRepo.Setup(r => r.GetByIdAsync(cmd.PumpId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Pump { Id = cmd.PumpId, Status = PumpStatus.Active, StationId = cmd.StationId });
        _priceRepo.Setup(r => r.GetActivePriceAsync(cmd.FuelTypeId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new FuelPrice { PricePerLitre = 100m, IsActive = true });

        var act = () => _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Invalid payment method*");
    }

    [Test]
    public void Validator_ValidCommand_NoErrors()
    {
        var validator = new RecordFuelSaleValidator();
        var cmd = CreateValidCommand();
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validator_ZeroQuantity_HasError()
    {
        var validator = new RecordFuelSaleValidator();
        var cmd = new RecordFuelSaleCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, "MH12AB1234", 0m, "Cash", null);

        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

#endregion

#region Shift Handler Tests

[TestFixture]
public class ShiftHandlerTests
{
    private Mock<IShiftRepository> _shiftRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _shiftRepo = new Mock<IShiftRepository>();
    }

    [Test]
    public async Task StartShift_ValidCommand_CreatesShift()
    {
        var logger = new Mock<ILogger<StartShiftHandler>>();
        var handler = new StartShiftHandler(_shiftRepo.Object, logger.Object);

        _shiftRepo.Setup(r => r.GetActiveShiftAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Shift?)null);
        _shiftRepo.Setup(r => r.AddAsync(It.IsAny<Shift>(), It.IsAny<CancellationToken>())).ReturnsAsync((Shift s, CancellationToken _) => s);

        var cmd = new StartShiftCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _shiftRepo.Verify(r => r.AddAsync(It.IsAny<Shift>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task StartShift_AlreadyActive_ThrowsDomainException()
    {
        var logger = new Mock<ILogger<StartShiftHandler>>();
        var handler = new StartShiftHandler(_shiftRepo.Object, logger.Object);

        var dealerId = Guid.NewGuid();
        _shiftRepo.Setup(r => r.GetActiveShiftAsync(dealerId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new Shift { Id = Guid.NewGuid(), DealerUserId = dealerId });

        var cmd = new StartShiftCommand(dealerId, Guid.NewGuid(), null);
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Test]
    public async Task EndShift_NoActiveShift_ThrowsNotFoundException()
    {
        var logger = new Mock<ILogger<EndShiftHandler>>();
        var handler = new EndShiftHandler(_shiftRepo.Object, logger.Object);

        var dealerId = Guid.NewGuid();
        _shiftRepo.Setup(r => r.GetActiveShiftAsync(dealerId, It.IsAny<CancellationToken>())).ReturnsAsync((Shift?)null);

        var cmd = new EndShiftCommand(dealerId, null);
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

#endregion

#region FuelPrice Query Tests

[TestFixture]
public class FuelPriceQueryTests
{
    [Test]
    public async Task GetActiveFuelPrices_ReturnsActivePrices()
    {
        var priceRepo = new Mock<IFuelPriceRepository>();
        priceRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<FuelPrice>
                 {
                     new() { Id = Guid.NewGuid(), PricePerLitre = 95.72m, IsActive = true },
                     new() { Id = Guid.NewGuid(), PricePerLitre = 89.62m, IsActive = true }
                 });

        var handler = new GetActiveFuelPricesHandler(priceRepo.Object);
        var result = await handler.Handle(new GetActiveFuelPricesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}

#endregion

#region Wallet Query Tests

[TestFixture]
public class WalletQueryTests
{
    [Test]
    public async Task GetWalletBalance_ExistingWallet_Returns()
    {
        var walletRepo = new Mock<ICustomerWalletRepository>();
        var customerId = Guid.NewGuid();
        walletRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new CustomerWallet { Id = Guid.NewGuid(), CustomerId = customerId, Balance = 500m, TotalLoaded = 1000m, IsActive = true });

        var handler = new GetWalletBalanceHandler(walletRepo.Object);
        var result = await handler.Handle(new GetWalletBalanceQuery(customerId), CancellationToken.None);

        result.Balance.Should().Be(500m);
    }

    [Test]
    public async Task GetWalletBalance_NoWallet_ThrowsNotFound()
    {
        var walletRepo = new Mock<ICustomerWalletRepository>();
        walletRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CustomerWallet?)null);

        var handler = new GetWalletBalanceHandler(walletRepo.Object);
        var act = () => handler.Handle(new GetWalletBalanceQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

#endregion
