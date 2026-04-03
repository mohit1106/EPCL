using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.GoogleLogin;

public record GoogleLoginCommand(string IdToken) : IRequest<LoginResponseDto>;
