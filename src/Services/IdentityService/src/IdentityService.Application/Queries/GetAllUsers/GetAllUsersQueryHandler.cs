using AutoMapper;
using MediatR;
using IdentityService.Application.Common;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(
    IUserRepository userRepo,
    IMapper mapper)
    : IRequestHandler<GetAllUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetAllUsersQuery query, CancellationToken ct)
    {
        var (items, totalCount) = await userRepo.GetAllAsync(
            query.Page, query.PageSize, query.Role, query.IsActive, query.SearchTerm, ct);

        return new PagedResult<UserDto>
        {
            Items = mapper.Map<IReadOnlyList<UserDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
