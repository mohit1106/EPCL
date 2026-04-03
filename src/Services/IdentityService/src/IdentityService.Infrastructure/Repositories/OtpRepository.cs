using Microsoft.EntityFrameworkCore;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Interfaces;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.Infrastructure.Repositories;

public class OtpRepository(IdentityDbContext context) : IOtpRepository
{
    public async Task<OtpRequest?> GetLatestValidOtpAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default)
    {
        return await context.OtpRequests
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(OtpRequest otpRequest, CancellationToken ct = default)
    {
        await context.OtpRequests.AddAsync(otpRequest, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OtpRequest otpRequest, CancellationToken ct = default)
    {
        context.OtpRequests.Update(otpRequest);
        await context.SaveChangesAsync(ct);
    }

    public async Task InvalidateAllForUserAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default)
    {
        var otps = await context.OtpRequests
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync(ct);

        foreach (var otp in otps)
            otp.MarkUsed();

        await context.SaveChangesAsync(ct);
    }
}
