using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.LockUser;

public record LockUserCommand(Guid UserId, bool IsLocked, string? Reason, Guid LockedByUserId) : IRequest<MessageResponseDto>;
