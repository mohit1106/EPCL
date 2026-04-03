using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.ResetPassword;

public record ResetPasswordCommand(string Email, string OtpCode, string NewPassword) : IRequest<MessageResponseDto>;
