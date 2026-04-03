using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Events;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.LoginUser;

public class LoginUserCommandHandler(
    IUserRepository userRepo,
    IJwtService jwtService,
    IRabbitMqPublisher publisher,
    IMapper mapper,
    ILogger<LoginUserCommandHandler> logger)
    : IRequestHandler<LoginUserCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginUserCommand cmd, CancellationToken ct)
    {
        // 1. Find user by email
        var user = await userRepo.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct)
            ?? throw new InvalidCredentialsException();

        // 2. Check if account is active
        if (!user.IsActive)
            throw new DomainException("Account has been deactivated. Contact support.");

        // 3. Check lockout
        if (user.IsLockedOut)
            throw new AccountLockedException(user.LockoutEnd);

        // 4. Verify password
        if (user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
        {
            user.IncrementFailedLogin();
            await userRepo.UpdateAsync(user, ct);

            // Publish lockout event if just locked
            if (user.IsLockedOut)
            {
                await publisher.PublishAsync(new UserAccountLockedEvent
                {
                    EventType = nameof(UserAccountLockedEvent),
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    LockoutEnd = user.LockoutEnd,
                    FailedAttempts = user.FailedLoginAttempts
                }, "identity.user.locked", ct);

                logger.LogWarning("User account locked. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
                throw new AccountLockedException(user.LockoutEnd);
            }

            logger.LogWarning(
                "Failed login attempt {Attempt}/5 for UserId: {UserId}, Email: {Email}",
                user.FailedLoginAttempts, user.Id, user.Email);
            throw new InvalidCredentialsException();
        }

        // 5. Successful login — reset failed attempts
        user.RecordSuccessfulLogin();
        await userRepo.UpdateAsync(user, ct);

        // 6. Generate tokens
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = await jwtService.GenerateRefreshTokenAsync(user.Id, ct);

        logger.LogInformation("User logged in successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn = 900,
            User = mapper.Map<UserDto>(user)
        };
    }
}
