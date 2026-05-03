using MediatR;
using IdentityService.Domain.Interfaces;

namespace IdentityService.Application.Queries.GetUserStats;

public record GetUserStatsQuery : IRequest<UserStatsResult>;

public class GetUserStatsQueryHandler(IUserRepository userRepo)
    : IRequestHandler<GetUserStatsQuery, UserStatsResult>
{
    public async Task<UserStatsResult> Handle(GetUserStatsQuery query, CancellationToken ct)
    {
        return await userRepo.GetUserStatsAsync(ct);
    }
}
