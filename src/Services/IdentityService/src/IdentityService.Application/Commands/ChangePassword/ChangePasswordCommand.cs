using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<MessageResponseDto>;
