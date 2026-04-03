using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Events;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.LockUser;

public class LockUserCommandHandler(
    IUserRepository userRepo,
    IRabbitMqPublisher publisher,
    ILogger<LockUserCommandHandler> logger)
    : IRequestHandler<LockUserCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(LockUserCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (cmd.IsLocked)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Indefinite lock
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await userRepo.UpdateAsync(user, ct);

            await publisher.PublishAsync(new UserAccountLockedEvent
            {
                EventType = nameof(UserAccountLockedEvent),
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                LockoutEnd = user.LockoutEnd,
                FailedAttempts = user.FailedLoginAttempts
            }, "identity.user.locked", ct);

            logger.LogInformation("User locked by admin. UserId: {UserId}, Reason: {Reason}", user.Id, cmd.Reason);
            return new MessageResponseDto($"User {user.Email} has been locked. Reason: {cmd.Reason}");
        }
        else
        {
            user.ResetFailedLoginAttempts();
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await userRepo.UpdateAsync(user, ct);

            logger.LogInformation("User unlocked by admin. UserId: {UserId}", user.Id);
            return new MessageResponseDto($"User {user.Email} has been unlocked.");
        }
    }
}
