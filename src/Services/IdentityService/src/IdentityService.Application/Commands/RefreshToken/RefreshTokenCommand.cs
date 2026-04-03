using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<LoginResponseDto>;
