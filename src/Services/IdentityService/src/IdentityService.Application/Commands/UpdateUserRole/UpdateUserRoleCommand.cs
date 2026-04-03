using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.UpdateUserRole;

public record UpdateUserRoleCommand(Guid UserId, string Role, Guid ChangedByUserId) : IRequest<UserDto>;
