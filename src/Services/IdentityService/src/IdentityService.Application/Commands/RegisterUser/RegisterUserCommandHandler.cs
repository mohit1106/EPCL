using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Events;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepo,
    IRabbitMqPublisher publisher,
    ILogger<RegisterUserCommandHandler> logger)
    : IRequestHandler<RegisterUserCommand, RegisterResponseDto>
{
    public async Task<RegisterResponseDto> Handle(RegisterUserCommand cmd, CancellationToken ct)
    {
        // 1. Check for duplicate email
        if (await userRepo.ExistsByEmailAsync(cmd.Email, ct))
            throw new DuplicateEntityException("User", "email", cmd.Email);

        // 2. Check for duplicate phone
        if (await userRepo.ExistsByPhoneAsync(cmd.PhoneNumber, ct))
            throw new DuplicateEntityException("User", "phone number", cmd.PhoneNumber);

        // 3. Parse role
        if (!Enum.TryParse<UserRole>(cmd.Role, true, out var role))
            throw new DomainException($"Invalid role: {cmd.Role}");

        // 4. Hash password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password, workFactor: 12);

        // 5. Create user entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = cmd.FullName,
            Email = cmd.Email.ToLowerInvariant(),
            PhoneNumber = cmd.PhoneNumber,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            IsEmailVerified = false,
            AuthProvider = AuthProvider.Local,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 6. Create default profile
        user.Profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreferredLanguage = "en"
        };

        await userRepo.AddAsync(user, ct);

        // 7. Publish user registered event
        await publisher.PublishAsync(new UserRegisteredEvent
        {
            EventType = nameof(UserRegisteredEvent),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        }, "identity.user.registered", ct);

        logger.LogInformation(
            "User registered successfully. UserId: {UserId}, Email: {Email}, Role: {Role}",
            user.Id, user.Email, user.Role);

        return new RegisterResponseDto(user.Id, "Registration successful. Please verify your email.");
    }
}
