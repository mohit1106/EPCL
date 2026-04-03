using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<MessageResponseDto>;
