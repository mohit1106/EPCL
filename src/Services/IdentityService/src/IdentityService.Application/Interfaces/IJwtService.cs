using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, CancellationToken ct = default);
    Guid? ValidateAccessToken(string token);
}
