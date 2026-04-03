using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Interfaces;

public interface IOtpRepository
{
    Task<OtpRequest?> GetLatestValidOtpAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);
    Task AddAsync(OtpRequest otpRequest, CancellationToken ct = default);
    Task UpdateAsync(OtpRequest otpRequest, CancellationToken ct = default);
    Task InvalidateAllForUserAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);
}
