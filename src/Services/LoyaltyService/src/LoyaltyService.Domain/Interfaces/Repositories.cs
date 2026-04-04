using LoyaltyService.Domain.Entities;

namespace LoyaltyService.Domain.Interfaces;

public interface ILoyaltyAccountRepository
{
    Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<LoyaltyAccount> CreateAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task UpdateAsync(LoyaltyAccount account, CancellationToken ct = default);
    Task<IReadOnlyList<LoyaltyAccount>> GetInactiveAccountsAsync(DateTimeOffset inactiveSince, CancellationToken ct = default);
}

public interface ILoyaltyTransactionRepository
{
    Task<LoyaltyTransaction> AddAsync(LoyaltyTransaction txn, CancellationToken ct = default);
    Task<(IReadOnlyList<LoyaltyTransaction> Items, int Total)> GetByAccountIdAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct = default);
}

public interface IReferralCodeRepository
{
    Task<ReferralCode?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<ReferralCode?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<ReferralCode> CreateAsync(ReferralCode referral, CancellationToken ct = default);
    Task UpdateAsync(ReferralCode referral, CancellationToken ct = default);
    Task<IReadOnlyList<ReferralCode>> GetTopReferrersAsync(int top, DateTimeOffset since, CancellationToken ct = default);
}

public interface IReferralRedemptionRepository
{
    Task<ReferralRedemption> AddAsync(ReferralRedemption redemption, CancellationToken ct = default);
    Task<bool> HasRedeemedAsync(Guid redeemedByCustomerId, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
