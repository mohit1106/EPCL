using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using LoyaltyService.Application.Commands;
using LoyaltyService.Application.DTOs;
using LoyaltyService.Domain.Entities;
using LoyaltyService.Domain.Exceptions;
using LoyaltyService.Domain.Interfaces;

namespace LoyaltyService.UnitTests;

#region PointsEngineTests

[TestFixture]
public class PointsEngineTests
{
    private Mock<ILoyaltyAccountRepository> _accountRepo = null!;
    private Mock<ILoyaltyTransactionRepository> _txnRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepo = new Mock<ILoyaltyAccountRepository>();
        _txnRepo = new Mock<ILoyaltyTransactionRepository>();
    }

    [Test]
    public async Task EarnPoints_ValidSale_CalculatesCorrectPointsAndUpdatesTier()
    {
        var logger = new Mock<ILogger<EarnPointsHandler>>();
        var handler = new EarnPointsHandler(_accountRepo.Object, _txnRepo.Object, logger.Object);

        var customerId = Guid.NewGuid();
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = customerId, PointsBalance = 0, LifetimePoints = 0, Tier = "Silver" };
        _accountRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var cmd = new EarnPointsCommand(customerId, Guid.NewGuid(), 505.50m); // Should earn 50 points (floor of 505.50 / 10)
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.PointsEarned.Should().Be(50);
        result.NewBalance.Should().Be(50);
        
        _accountRepo.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _txnRepo.Verify(r => r.AddAsync(It.Is<LoyaltyTransaction>(t => t.Points == 50), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EarnPoints_TierUpgrade_Gold_UpdatesTier()
    {
        var logger = new Mock<ILogger<EarnPointsHandler>>();
        var handler = new EarnPointsHandler(_accountRepo.Object, _txnRepo.Object, logger.Object);

        var customerId = Guid.NewGuid();
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = customerId, LifetimePoints = 990, Tier = "Silver" };
        _accountRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var cmd = new EarnPointsCommand(customerId, Guid.NewGuid(), 200m); // Earns 20 points, lifetime = 1010 -> Gold
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Tier.Should().Be("Gold");
        account.Tier.Should().Be("Gold");
    }

    [Test]
    public async Task RedeemPoints_ValidRequest_DeductsPoints()
    {
        var handler = new RedeemPointsHandler(_accountRepo.Object, _txnRepo.Object);

        var customerId = Guid.NewGuid();
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = customerId, PointsBalance = 500 };
        _accountRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var cmd = new RedeemPointsCommand(customerId, 200); // 200 pts = ₹100 discount
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.DiscountAmount.Should().Be(100.0m);
        account.PointsBalance.Should().Be(300);
        
        _accountRepo.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _txnRepo.Verify(r => r.AddAsync(It.Is<LoyaltyTransaction>(t => t.Points == -200), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RedeemPoints_InsufficientPoints_ThrowsException()
    {
        var handler = new RedeemPointsHandler(_accountRepo.Object, _txnRepo.Object);

        var customerId = Guid.NewGuid();
        var account = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = customerId, PointsBalance = 50 };
        _accountRepo.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var cmd = new RedeemPointsCommand(customerId, 100);
        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientPointsException>();
    }

    [Test]
    public async Task ExpirePoints_InactiveAccounts_ResetsPointsToZero()
    {
        var logger = new Mock<ILogger<ExpirePointsHandler>>();
        var handler = new ExpirePointsHandler(_accountRepo.Object, _txnRepo.Object, logger.Object);

        var inactiveAccounts = new List<LoyaltyAccount>
        {
            new() { Id = Guid.NewGuid(), PointsBalance = 100 },
            new() { Id = Guid.NewGuid(), PointsBalance = 50 }
        };
        _accountRepo.Setup(r => r.GetInactiveAccountsAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(inactiveAccounts);

        var result = await handler.Handle(new ExpirePointsCommand(), CancellationToken.None);

        result.Should().Be(2); // 2 accounts expired
        inactiveAccounts[0].PointsBalance.Should().Be(0);
        inactiveAccounts[1].PointsBalance.Should().Be(0);
        _txnRepo.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

#endregion

#region ReferralTests

[TestFixture]
public class ReferralTests
{
    private Mock<IReferralCodeRepository> _referralRepo = null!;
    private Mock<IReferralRedemptionRepository> _redemptionRepo = null!;
    private Mock<ILoyaltyAccountRepository> _accountRepo = null!;
    private Mock<ILoyaltyTransactionRepository> _txnRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _referralRepo = new Mock<IReferralCodeRepository>();
        _redemptionRepo = new Mock<IReferralRedemptionRepository>();
        _accountRepo = new Mock<ILoyaltyAccountRepository>();
        _txnRepo = new Mock<ILoyaltyTransactionRepository>();
    }

    [Test]
    public async Task EarnReferralBonus_ValidCode_AwardsPointsToReferrer()
    {
        var logger = new Mock<ILogger<EarnReferralBonusHandler>>();
        var handler = new EarnReferralBonusHandler(_referralRepo.Object, _redemptionRepo.Object, _accountRepo.Object, _txnRepo.Object, logger.Object);

        var referrerId = Guid.NewGuid();
        var referral = new ReferralCode { Id = Guid.NewGuid(), CustomerId = referrerId, Code = "ABC12345" };
        var newCustomerId = Guid.NewGuid();

        _referralRepo.Setup(r => r.GetByCodeAsync("ABC12345", It.IsAny<CancellationToken>())).ReturnsAsync(referral);
        _redemptionRepo.Setup(r => r.HasRedeemedAsync(newCustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        var referrerAccount = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = referrerId, PointsBalance = 0 };
        _accountRepo.Setup(r => r.GetByCustomerIdAsync(referrerId, It.IsAny<CancellationToken>())).ReturnsAsync(referrerAccount);

        var result = await handler.Handle(new EarnReferralBonusCommand("ABC12345", newCustomerId), CancellationToken.None);

        result.Message.Should().Contain("100 points");
        referrerAccount.PointsBalance.Should().Be(100);
        referral.TotalReferrals.Should().Be(1);

        _accountRepo.Verify(r => r.UpdateAsync(referrerAccount, It.IsAny<CancellationToken>()), Times.Once);
        _redemptionRepo.Verify(r => r.AddAsync(It.IsAny<ReferralRedemption>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EarnReferralBonus_SelfReferral_ThrowsDomainException()
    {
        var logger = new Mock<ILogger<EarnReferralBonusHandler>>();
        var handler = new EarnReferralBonusHandler(_referralRepo.Object, _redemptionRepo.Object, _accountRepo.Object, _txnRepo.Object, logger.Object);

        var customerId = Guid.NewGuid();
        var referral = new ReferralCode { Id = Guid.NewGuid(), CustomerId = customerId, Code = "ABC12345" };
        
        _referralRepo.Setup(r => r.GetByCodeAsync("ABC12345", It.IsAny<CancellationToken>())).ReturnsAsync(referral);

        var act = () => handler.Handle(new EarnReferralBonusCommand("ABC12345", customerId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*own referral*");
    }

    [Test]
    public async Task CreateReferralCode_GeneratesCode()
    {
        var handler = new CreateReferralCodeHandler(_referralRepo.Object);

        _referralRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ReferralCode?)null);
        _referralRepo.Setup(r => r.CreateAsync(It.IsAny<ReferralCode>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReferralCode r, CancellationToken _) => r);

        var result = await handler.Handle(new CreateReferralCodeCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
        result.Code.Should().NotBeNullOrEmpty();
        result.Code.Length.Should().Be(8);
        
        _referralRepo.Verify(r => r.CreateAsync(It.IsAny<ReferralCode>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
