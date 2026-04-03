using AutoMapper;
using MediatR;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Exceptions;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IUserRepository userRepo,
    IMapper mapper)
    : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("User", query.UserId);

        return mapper.Map<UserDto>(user);
    }
}
