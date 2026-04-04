using MediatR;
using LoyaltyService.Application.DTOs;
using LoyaltyService.Domain.Exceptions;
using LoyaltyService.Domain.Interfaces;

namespace LoyaltyService.Application.Queries;

// ══════════════════════════════════════════════════════════════════
// GetLoyaltyBalance — returns balance, tier, progress to next tier
// ══════════════════════════════════════════════════════════════════
public record GetLoyaltyBalanceQuery(Guid CustomerId) : IRequest<LoyaltyBalanceDto>;

public class GetLoyaltyBalanceHandler(ILoyaltyAccountRepository repo)
    : IRequestHandler<GetLoyaltyBalanceQuery, LoyaltyBalanceDto>
{
    public async Task<LoyaltyBalanceDto> Handle(GetLoyaltyBalanceQuery q, CancellationToken ct)
    {
        var account = await repo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("LoyaltyAccount", q.CustomerId);

        var (pointsToNext, nextTier) = account.Tier switch
        {
            "Silver" => (1000 - account.LifetimePoints, "Gold"),
            "Gold" => (5000 - account.LifetimePoints, "Platinum"),
            _ => (0, "Platinum") // Already Platinum
        };

        return new LoyaltyBalanceDto(account.Id, account.CustomerId, account.PointsBalance,
            account.LifetimePoints, account.Tier, Math.Max(0, pointsToNext), nextTier,
            account.LastActivityAt, account.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// GetLoyaltyHistory — paginated list of point changes
// ══════════════════════════════════════════════════════════════════
public record GetLoyaltyHistoryQuery(Guid CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<LoyaltyHistoryDto>;

public class GetLoyaltyHistoryHandler(
    ILoyaltyAccountRepository accountRepo,
    ILoyaltyTransactionRepository txnRepo)
    : IRequestHandler<GetLoyaltyHistoryQuery, LoyaltyHistoryDto>
{
    public async Task<LoyaltyHistoryDto> Handle(GetLoyaltyHistoryQuery q, CancellationToken ct)
    {
        var account = await accountRepo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("LoyaltyAccount", q.CustomerId);

        var (items, total) = await txnRepo.GetByAccountIdAsync(account.Id, q.Page, q.PageSize, ct);
        var dtos = items.Select(t => new LoyaltyTransactionDto(t.Id, t.Type, t.Points,
            t.BalanceAfter, t.Description, t.SaleTransactionId, t.Timestamp)).ToList();

        return new LoyaltyHistoryDto(dtos, total, q.Page, q.PageSize);
    }
}

// ══════════════════════════════════════════════════════════════════
// GetMyReferralCode
// ══════════════════════════════════════════════════════════════════
public record GetMyReferralCodeQuery(Guid CustomerId) : IRequest<ReferralCodeDto>;

public class GetMyReferralCodeHandler(IReferralCodeRepository repo)
    : IRequestHandler<GetMyReferralCodeQuery, ReferralCodeDto>
{
    public async Task<ReferralCodeDto> Handle(GetMyReferralCodeQuery q, CancellationToken ct)
    {
        var referral = await repo.GetByCustomerIdAsync(q.CustomerId, ct)
            ?? throw new NotFoundException("ReferralCode", q.CustomerId);
        return new ReferralCodeDto(referral.Code, referral.TotalReferrals, referral.TotalPointsEarned, referral.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// GetReferralLeaderboard — top 10 referrers this month
// ══════════════════════════════════════════════════════════════════
public record GetReferralLeaderboardQuery(int Top = 10) : IRequest<IReadOnlyList<ReferralLeaderboardEntryDto>>;

public class GetReferralLeaderboardHandler(IReferralCodeRepository repo)
    : IRequestHandler<GetReferralLeaderboardQuery, IReadOnlyList<ReferralLeaderboardEntryDto>>
{
    public async Task<IReadOnlyList<ReferralLeaderboardEntryDto>> Handle(GetReferralLeaderboardQuery q, CancellationToken ct)
    {
        var since = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var top = await repo.GetTopReferrersAsync(q.Top, since, ct);
        return top.Select((r, i) => new ReferralLeaderboardEntryDto(
            i + 1, r.Code, r.TotalReferrals, r.TotalPointsEarned)).ToList();
    }
}
