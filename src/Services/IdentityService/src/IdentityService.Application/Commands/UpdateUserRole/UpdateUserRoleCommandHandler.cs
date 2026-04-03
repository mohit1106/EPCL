using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Events;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Commands.UpdateUserRole;

public class UpdateUserRoleCommandHandler(
    IUserRepository userRepo,
    IRabbitMqPublisher publisher,
    IMapper mapper,
    ILogger<UpdateUserRoleCommandHandler> logger)
    : IRequestHandler<UpdateUserRoleCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserRoleCommand cmd, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User", cmd.UserId);

        if (!Enum.TryParse<UserRole>(cmd.Role, true, out var newRole))
            throw new DomainException($"Invalid role: {cmd.Role}");

        var oldRole = user.Role;
        user.Role = newRole;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await userRepo.UpdateAsync(user, ct);

        await publisher.PublishAsync(new UserRoleChangedEvent
        {
            EventType = nameof(UserRoleChangedEvent),
            UserId = user.Id,
            OldRole = oldRole.ToString(),
            NewRole = newRole.ToString(),
            ChangedByUserId = cmd.ChangedByUserId
        }, "identity.user.rolechanged", ct);

        logger.LogInformation("User role changed. UserId: {UserId}, OldRole: {OldRole}, NewRole: {NewRole}",
            user.Id, oldRole, newRole);

        return mapper.Map<UserDto>(user);
    }
}
