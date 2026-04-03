using MediatR;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;
