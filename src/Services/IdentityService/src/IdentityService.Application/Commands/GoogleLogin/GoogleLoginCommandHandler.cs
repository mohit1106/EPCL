using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Events;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.GoogleLogin;

public class GoogleLoginCommandHandler(
    IUserRepository userRepo,
    IJwtService jwtService,
    IGoogleAuthService googleAuthService,
    IRabbitMqPublisher publisher,
    IMapper mapper,
    ILogger<GoogleLoginCommandHandler> logger)
    : IRequestHandler<GoogleLoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(GoogleLoginCommand cmd, CancellationToken ct)
    {
        // 1. Validate the Google ID token
        var payload = await googleAuthService.ValidateIdTokenAsync(cmd.IdToken, ct);

        // 2. Check if user already exists by Google sub ID or email
        var user = await userRepo.GetByGoogleSubAsync(payload.Subject, ct)
                   ?? await userRepo.GetByEmailAsync(payload.Email, ct);

        if (user == null)
        {
            // 3a. New user — auto-register as Customer
            user = new User
            {
                Id = Guid.NewGuid(),
                FullName = payload.Name,
                Email = payload.Email.ToLowerInvariant(),
                PhoneNumber = string.Empty,
                GoogleSub = payload.Subject,
                ProfilePictureUrl = payload.PictureUrl,
                Role = UserRole.Customer,
                IsActive = true,
                IsEmailVerified = true,
                AuthProvider = AuthProvider.Google,
                CreatedAt = DateTimeOffset.UtcNow
            };

            user.Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PreferredLanguage = "en"
            };

            await userRepo.AddAsync(user, ct);

            await publisher.PublishAsync(new UserRegisteredEvent
            {
                EventType = nameof(UserRegisteredEvent),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString()
            }, "identity.user.registered", ct);

            logger.LogInformation("New user registered via Google OAuth. UserId: {UserId}, Email: {Email}",
                user.Id, user.Email);
        }
        else
        {
            // 3b. Existing user — update Google sub if not already set
            if (user.GoogleSub == null)
            {
                user.GoogleSub = payload.Subject;
                user.ProfilePictureUrl = payload.PictureUrl;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await userRepo.UpdateAsync(user, ct);
            }

            if (!user.IsActive)
                throw new Domain.Exceptions.DomainException("Account has been deactivated. Contact support.");
        }

        user.RecordSuccessfulLogin();
        await userRepo.UpdateAsync(user, ct);

        // 4. Issue JWT
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = await jwtService.GenerateRefreshTokenAsync(user.Id, ct);

        logger.LogInformation("User logged in via Google OAuth. UserId: {UserId}, Email: {Email}", user.Id, user.Email);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresIn = 900,
            User = mapper.Map<UserDto>(user)
        };
    }
}
