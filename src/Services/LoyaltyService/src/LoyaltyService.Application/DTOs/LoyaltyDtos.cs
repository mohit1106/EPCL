namespace LoyaltyService.Application.DTOs;

public record LoyaltyBalanceDto(Guid AccountId, Guid CustomerId, int PointsBalance,
    int LifetimePoints, string Tier, int PointsToNextTier, string NextTier,
    DateTimeOffset? LastActivityAt, DateTimeOffset CreatedAt);

public record LoyaltyTransactionDto(Guid Id, string Type, int Points, int BalanceAfter,
    string? Description, Guid? SaleTransactionId, DateTimeOffset Timestamp);

public record LoyaltyHistoryDto(IReadOnlyList<LoyaltyTransactionDto> Items, int Total,
    int Page, int PageSize);

public record EarnPointsResultDto(int PointsEarned, int NewBalance, string Tier);

public record RedeemPointsResultDto(int PointsRedeemed, decimal DiscountAmount, int NewBalance, string Tier);

public record ReferralCodeDto(string Code, int TotalReferrals, int TotalPointsEarned, DateTimeOffset CreatedAt);

public record ReferralLeaderboardEntryDto(int Rank, string Code, int TotalReferrals, int TotalPointsEarned);

public record MessageResponseDto(string Message);
