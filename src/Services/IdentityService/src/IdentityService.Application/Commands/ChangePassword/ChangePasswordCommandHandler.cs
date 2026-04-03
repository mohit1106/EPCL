using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserRepository userRepo,
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new InvalidCredentialsException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await userRepo.UpdateAsync(user, ct);

        logger.LogInformation("Password changed for UserId: {UserId}", user.Id);
        return new MessageResponseDto("Password changed successfully.");
    }
}
