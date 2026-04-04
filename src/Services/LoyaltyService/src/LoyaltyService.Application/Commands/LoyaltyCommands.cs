using MediatR;
using Microsoft.Extensions.Logging;
using LoyaltyService.Application.DTOs;
using LoyaltyService.Domain.Entities;
using LoyaltyService.Domain.Exceptions;
using LoyaltyService.Domain.Interfaces;

namespace LoyaltyService.Application.Commands;

// ══════════════════════════════════════════════════════════════════
// EarnPoints — called by SaleCompletedEvent consumer
// Rule: 1 point per ₹10, rounded DOWN (FLOOR)
// ══════════════════════════════════════════════════════════════════
public record EarnPointsCommand(Guid CustomerId, Guid? SaleTransactionId,
    decimal TotalAmount, string? Description = null) : IRequest<EarnPointsResultDto>;

public class EarnPointsHandler(
    ILoyaltyAccountRepository accountRepo,
    ILoyaltyTransactionRepository txnRepo,
    ILogger<EarnPointsHandler> logger)
    : IRequestHandler<EarnPointsCommand, EarnPointsResultDto>
{
    public async Task<EarnPointsResultDto> Handle(EarnPointsCommand cmd, CancellationToken ct)
    {
        var account = await accountRepo.GetByCustomerIdAsync(cmd.CustomerId, ct);
        if (account == null)
        {
            account = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = cmd.CustomerId };
            await accountRepo.CreateAsync(account, ct);
        }

        var pointsEarned = (int)Math.Floor(cmd.TotalAmount / 10m);
        if (pointsEarned <= 0)
            return new EarnPointsResultDto(0, account.PointsBalance, account.Tier);

        account.PointsBalance += pointsEarned;
        account.LifetimePoints += pointsEarned;
        account.LastActivityAt = DateTimeOffset.UtcNow;
        account.RecalculateTier();
        await accountRepo.UpdateAsync(account, ct);

        await txnRepo.AddAsync(new LoyaltyTransaction
        {
            Id = Guid.NewGuid(), LoyaltyAccountId = account.Id,
            SaleTransactionId = cmd.SaleTransactionId, Type = "Earn",
            Points = pointsEarned, BalanceAfter = account.PointsBalance,
            Description = cmd.Description ?? $"Earned {pointsEarned} pts from ₹{cmd.TotalAmount:N2} purchase"
        }, ct);

        logger.LogInformation("Customer {CustomerId} earned {Points} points (₹{Amount}). Balance: {Balance}, Tier: {Tier}",
            cmd.CustomerId, pointsEarned, cmd.TotalAmount, account.PointsBalance, account.Tier);

        return new EarnPointsResultDto(pointsEarned, account.PointsBalance, account.Tier);
    }
}

// ══════════════════════════════════════════════════════════════════
// RedeemPoints — customer redeems points for discount
// ══════════════════════════════════════════════════════════════════
public record RedeemPointsCommand(Guid CustomerId, int PointsToRedeem) : IRequest<RedeemPointsResultDto>;

public class RedeemPointsHandler(
    ILoyaltyAccountRepository accountRepo,
    ILoyaltyTransactionRepository txnRepo)
    : IRequestHandler<RedeemPointsCommand, RedeemPointsResultDto>
{
    public async Task<RedeemPointsResultDto> Handle(RedeemPointsCommand cmd, CancellationToken ct)
    {
        var account = await accountRepo.GetByCustomerIdAsync(cmd.CustomerId, ct)
            ?? throw new NotFoundException("LoyaltyAccount", cmd.CustomerId);

        if (cmd.PointsToRedeem <= 0) throw new DomainException("Points to redeem must be positive.");
        if (account.PointsBalance < cmd.PointsToRedeem)
            throw new InsufficientPointsException(cmd.PointsToRedeem, account.PointsBalance);

        // 1 point = ₹0.50 discount
        var discountAmount = cmd.PointsToRedeem * 0.50m;

        account.PointsBalance -= cmd.PointsToRedeem;
        account.LastActivityAt = DateTimeOffset.UtcNow;
        await accountRepo.UpdateAsync(account, ct);

        await txnRepo.AddAsync(new LoyaltyTransaction
        {
            Id = Guid.NewGuid(), LoyaltyAccountId = account.Id, Type = "Redeem",
            Points = -cmd.PointsToRedeem, BalanceAfter = account.PointsBalance,
            Description = $"Redeemed {cmd.PointsToRedeem} pts for ₹{discountAmount:N2} discount"
        }, ct);

        return new RedeemPointsResultDto(cmd.PointsToRedeem, discountAmount, account.PointsBalance, account.Tier);
    }
}

// ══════════════════════════════════════════════════════════════════
// ExpirePoints — batch job: expire all points for accounts inactive > 12 months
// ══════════════════════════════════════════════════════════════════
public record ExpirePointsCommand : IRequest<int>;

public class ExpirePointsHandler(
    ILoyaltyAccountRepository accountRepo,
    ILoyaltyTransactionRepository txnRepo,
    ILogger<ExpirePointsHandler> logger)
    : IRequestHandler<ExpirePointsCommand, int>
{
    public async Task<int> Handle(ExpirePointsCommand cmd, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-12);
        var inactiveAccounts = await accountRepo.GetInactiveAccountsAsync(cutoff, ct);
        var totalExpired = 0;

        foreach (var acct in inactiveAccounts.Where(a => a.PointsBalance > 0))
        {
            var expired = acct.PointsBalance;
            acct.PointsBalance = 0;
            await accountRepo.UpdateAsync(acct, ct);

            await txnRepo.AddAsync(new LoyaltyTransaction
            {
                Id = Guid.NewGuid(), LoyaltyAccountId = acct.Id, Type = "Expire",
                Points = -expired, BalanceAfter = 0,
                Description = $"Points expired due to 12 months inactivity ({expired} pts)"
            }, ct);

            totalExpired++;
            logger.LogInformation("Expired {Points} pts for customer {CustomerId}", expired, acct.CustomerId);
        }

        return totalExpired;
    }
}

// ══════════════════════════════════════════════════════════════════
// EarnReferralBonus — called when a new customer uses a referral code
// Rule: referrer earns 100 bonus points
// ══════════════════════════════════════════════════════════════════
public record EarnReferralBonusCommand(string ReferralCode, Guid NewCustomerId) : IRequest<MessageResponseDto>;

public class EarnReferralBonusHandler(
    IReferralCodeRepository referralRepo,
    IReferralRedemptionRepository redemptionRepo,
    ILoyaltyAccountRepository accountRepo,
    ILoyaltyTransactionRepository txnRepo,
    ILogger<EarnReferralBonusHandler> logger)
    : IRequestHandler<EarnReferralBonusCommand, MessageResponseDto>
{
    private const int ReferralBonusPoints = 100;

    public async Task<MessageResponseDto> Handle(EarnReferralBonusCommand cmd, CancellationToken ct)
    {
        var referral = await referralRepo.GetByCodeAsync(cmd.ReferralCode, ct)
            ?? throw new NotFoundException("ReferralCode", cmd.ReferralCode);

        // Prevent self-referral
        if (referral.CustomerId == cmd.NewCustomerId)
            throw new DomainException("Cannot use your own referral code.");

        // Each new customer can only use a referral code once
        if (await redemptionRepo.HasRedeemedAsync(cmd.NewCustomerId, ct))
            return new MessageResponseDto("Referral already applied.");

        // Ensure referrer has a loyalty account
        var referrerAccount = await accountRepo.GetByCustomerIdAsync(referral.CustomerId, ct);
        if (referrerAccount == null)
        {
            referrerAccount = new LoyaltyAccount { Id = Guid.NewGuid(), CustomerId = referral.CustomerId };
            await accountRepo.CreateAsync(referrerAccount, ct);
        }

        // Credit bonus points
        referrerAccount.PointsBalance += ReferralBonusPoints;
        referrerAccount.LifetimePoints += ReferralBonusPoints;
        referrerAccount.LastActivityAt = DateTimeOffset.UtcNow;
        referrerAccount.RecalculateTier();
        await accountRepo.UpdateAsync(referrerAccount, ct);

        await txnRepo.AddAsync(new LoyaltyTransaction
        {
            Id = Guid.NewGuid(), LoyaltyAccountId = referrerAccount.Id, Type = "Referral",
            Points = ReferralBonusPoints, BalanceAfter = referrerAccount.PointsBalance,
            Description = $"Referral bonus — new customer joined using code {referral.Code}"
        }, ct);

        // Update referral stats
        referral.TotalReferrals++;
        referral.TotalPointsEarned += ReferralBonusPoints;
        await referralRepo.UpdateAsync(referral, ct);

        // Record redemption
        await redemptionRepo.AddAsync(new ReferralRedemption
        {
            Id = Guid.NewGuid(), ReferralCodeId = referral.Id,
            RedeemedByCustomerId = cmd.NewCustomerId, PointsAwarded = ReferralBonusPoints
        }, ct);

        logger.LogInformation("Referral bonus: {Points} pts credited to customer {Referrer} for code {Code}",
            ReferralBonusPoints, referral.CustomerId, referral.Code);

        return new MessageResponseDto($"Referral bonus of {ReferralBonusPoints} points credited to referrer.");
    }
}

// ══════════════════════════════════════════════════════════════════
// CreateReferralCode — auto-generates a unique 8-char code for a customer
// ══════════════════════════════════════════════════════════════════
public record CreateReferralCodeCommand(Guid CustomerId) : IRequest<ReferralCodeDto>;

public class CreateReferralCodeHandler(IReferralCodeRepository referralRepo)
    : IRequestHandler<CreateReferralCodeCommand, ReferralCodeDto>
{
    public async Task<ReferralCodeDto> Handle(CreateReferralCodeCommand cmd, CancellationToken ct)
    {
        var existing = await referralRepo.GetByCustomerIdAsync(cmd.CustomerId, ct);
        if (existing != null)
            return new ReferralCodeDto(existing.Code, existing.TotalReferrals, existing.TotalPointsEarned, existing.CreatedAt);

        var code = GenerateCode();
        var referral = new ReferralCode
        {
            Id = Guid.NewGuid(), CustomerId = cmd.CustomerId, Code = code
        };
        await referralRepo.CreateAsync(referral, ct);
        return new ReferralCodeDto(referral.Code, 0, 0, referral.CreatedAt);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I for readability
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
