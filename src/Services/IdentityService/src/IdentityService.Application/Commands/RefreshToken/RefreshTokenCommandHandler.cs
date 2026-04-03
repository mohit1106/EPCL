using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepo,
    IUserRepository userRepo,
    IJwtService jwtService,
    IMapper mapper,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var existingToken = await refreshTokenRepo.GetByTokenAsync(cmd.Token, ct)
            ?? throw new DomainException("Invalid refresh token.");

        if (!existingToken.IsActive)
            throw new DomainException("Refresh token has been revoked or expired.");

        var user = await userRepo.GetByIdAsync(existingToken.UserId, ct)
            ?? throw new NotFoundException("User", existingToken.UserId);

        if (!user.IsActive)
            throw new DomainException("Account has been deactivated.");

        // Rotate: revoke old, create new
        var newRefreshToken = await jwtService.GenerateRefreshTokenAsync(user.Id, ct);
        existingToken.Revoke(newRefreshToken.Token);
        await refreshTokenRepo.UpdateAsync(existingToken, ct);

        var accessToken = jwtService.GenerateAccessToken(user);

        logger.LogInformation("Token refreshed for UserId: {UserId}", user.Id);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn = 900,
            User = mapper.Map<UserDto>(user)
        };
    }
}
