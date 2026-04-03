using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<LoginResponseDto>;
