namespace LoyaltyService.Domain.Entities;

/// <summary>
/// Loyalty account per customer. Tracks balance, lifetime points, and tier.
/// Tier thresholds: Silver (0-999), Gold (1000-4999), Platinum (5000+).
/// Points expire if no activity in 12 months.
/// </summary>
public class LoyaltyAccount
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public int PointsBalance { get; set; }
    public int LifetimePoints { get; set; }
    public string Tier { get; set; } = "Silver";
    public DateTimeOffset? TierUpdatedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public void RecalculateTier()
    {
        var newTier = LifetimePoints switch
        {
            >= 5000 => "Platinum",
            >= 1000 => "Gold",
            _ => "Silver"
        };
        if (newTier != Tier) { Tier = newTier; TierUpdatedAt = DateTimeOffset.UtcNow; }
    }
}

/// <summary>
/// Immutable record of every loyalty point change — Earn, Redeem, Expire, Adjust, Referral.
/// </summary>
public class LoyaltyTransaction
{
    public Guid Id { get; set; }
    public Guid LoyaltyAccountId { get; set; }
    public Guid? SaleTransactionId { get; set; }
    public string Type { get; set; } = string.Empty; // Earn, Redeem, Expire, Adjust, Referral
    public int Points { get; set; } // positive = earned, negative = used/expired
    public int BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public LoyaltyAccount? LoyaltyAccount { get; set; }
}

/// <summary>
/// Referral code per customer — unique 8-char alphanumeric.
/// Stats: total referrals count and total bonus points earned.
/// </summary>
public class ReferralCode
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Code { get; set; } = string.Empty; // unique 8-char
    public int TotalReferrals { get; set; }
    public int TotalPointsEarned { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Tracks each referral redemption — who used the code and points awarded to the referrer.
/// </summary>
public class ReferralRedemption
{
    public Guid Id { get; set; }
    public Guid ReferralCodeId { get; set; }
    public Guid RedeemedByCustomerId { get; set; }
    public int PointsAwarded { get; set; }
    public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.UtcNow;

    public ReferralCode? ReferralCode { get; set; }
}

/// <summary>Idempotency table for RabbitMQ events.</summary>
public class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
