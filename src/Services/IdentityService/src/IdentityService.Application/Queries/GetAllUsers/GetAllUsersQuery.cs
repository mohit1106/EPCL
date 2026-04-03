using MediatR;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Enums;

namespace IdentityService.Application.Queries.GetAllUsers;

public record GetAllUsersQuery(
    int Page = 1,
    int PageSize = 20,
    UserRole? Role = null,
    bool? IsActive = null,
    string? SearchTerm = null
) : IRequest<PagedResult<UserDto>>;
