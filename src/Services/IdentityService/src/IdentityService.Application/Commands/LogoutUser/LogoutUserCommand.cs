using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.LogoutUser;

public record LogoutUserCommand(Guid UserId) : IRequest<MessageResponseDto>;
