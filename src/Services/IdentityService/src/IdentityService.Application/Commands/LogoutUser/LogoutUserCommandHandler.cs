using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.LogoutUser;

public class LogoutUserCommandHandler(
    IRefreshTokenRepository refreshTokenRepo,
    ILogger<LogoutUserCommandHandler> logger)
    : IRequestHandler<LogoutUserCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(LogoutUserCommand cmd, CancellationToken ct)
    {
        await refreshTokenRepo.RevokeAllByUserIdAsync(cmd.UserId, ct);
        logger.LogInformation("User logged out. UserId: {UserId}", cmd.UserId);
        return new MessageResponseDto("Logged out successfully.");
    }
}
