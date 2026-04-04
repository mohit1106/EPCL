using Microsoft.EntityFrameworkCore;
using LoyaltyService.Domain.Entities;
using LoyaltyService.Domain.Interfaces;
using LoyaltyService.Infrastructure.Persistence;

namespace LoyaltyService.Infrastructure.Repositories;

public class LoyaltyAccountRepository(LoyaltyDbContext ctx) : ILoyaltyAccountRepository
{
    public async Task<LoyaltyAccount?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct)
        => await ctx.LoyaltyAccounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);

    public async Task<LoyaltyAccount> CreateAsync(LoyaltyAccount account, CancellationToken ct)
    { await ctx.LoyaltyAccounts.AddAsync(account, ct); await ctx.SaveChangesAsync(ct); return account; }

    public async Task UpdateAsync(LoyaltyAccount account, CancellationToken ct)
    { ctx.LoyaltyAccounts.Update(account); await ctx.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<LoyaltyAccount>> GetInactiveAccountsAsync(DateTimeOffset inactiveSince, CancellationToken ct)
        => await ctx.LoyaltyAccounts
            .Where(a => a.PointsBalance > 0 && (a.LastActivityAt == null || a.LastActivityAt < inactiveSince))
            .ToListAsync(ct);
}

public class LoyaltyTransactionRepository(LoyaltyDbContext ctx) : ILoyaltyTransactionRepository
{
    public async Task<LoyaltyTransaction> AddAsync(LoyaltyTransaction txn, CancellationToken ct)
    { await ctx.LoyaltyTransactions.AddAsync(txn, ct); await ctx.SaveChangesAsync(ct); return txn; }

    public async Task<(IReadOnlyList<LoyaltyTransaction> Items, int Total)> GetByAccountIdAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct)
    {
        var q = ctx.LoyaltyTransactions.Where(t => t.LoyaltyAccountId == accountId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .AsNoTracking().ToListAsync(ct);
        return (items, total);
    }
}

public class ReferralCodeRepository(LoyaltyDbContext ctx) : IReferralCodeRepository
{
    public async Task<ReferralCode?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct)
        => await ctx.ReferralCodes.FirstOrDefaultAsync(r => r.CustomerId == customerId, ct);

    public async Task<ReferralCode?> GetByCodeAsync(string code, CancellationToken ct)
        => await ctx.ReferralCodes.FirstOrDefaultAsync(r => r.Code == code, ct);

    public async Task<ReferralCode> CreateAsync(ReferralCode referral, CancellationToken ct)
    { await ctx.ReferralCodes.AddAsync(referral, ct); await ctx.SaveChangesAsync(ct); return referral; }

    public async Task UpdateAsync(ReferralCode referral, CancellationToken ct)
    { ctx.ReferralCodes.Update(referral); await ctx.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<ReferralCode>> GetTopReferrersAsync(int top, DateTimeOffset since, CancellationToken ct)
        => await ctx.ReferralCodes
            .Where(r => r.TotalReferrals > 0)
            .OrderByDescending(r => r.TotalReferrals)
            .Take(top).AsNoTracking().ToListAsync(ct);
}

public class ReferralRedemptionRepository(LoyaltyDbContext ctx) : IReferralRedemptionRepository
{
    public async Task<ReferralRedemption> AddAsync(ReferralRedemption redemption, CancellationToken ct)
    { await ctx.ReferralRedemptions.AddAsync(redemption, ct); await ctx.SaveChangesAsync(ct); return redemption; }

    public async Task<bool> HasRedeemedAsync(Guid redeemedByCustomerId, CancellationToken ct)
        => await ctx.ReferralRedemptions.AnyAsync(r => r.RedeemedByCustomerId == redeemedByCustomerId, ct);
}

public class ProcessedEventRepository(LoyaltyDbContext ctx) : IProcessedEventRepository
{
    public async Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct)
        => await ctx.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);

    public async Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct)
    { await ctx.ProcessedEvents.AddAsync(new ProcessedEvent { Id = Guid.NewGuid(), EventId = eventId, EventType = eventType }, ct); await ctx.SaveChangesAsync(ct); }
}
