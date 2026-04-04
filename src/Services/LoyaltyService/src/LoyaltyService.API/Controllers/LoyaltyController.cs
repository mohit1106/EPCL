using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoyaltyService.Application.Commands;
using LoyaltyService.Application.Queries;

namespace LoyaltyService.API.Controllers;

/// <summary>Loyalty program — balance, history, redeem, referral.</summary>
[ApiController]
[Route("api/loyalty")]
[Authorize]
public class LoyaltyController(IMediator mediator) : ControllerBase
{
    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Get current points balance + tier + progress.</summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
        => Ok(await mediator.Send(new GetLoyaltyBalanceQuery(GetUserId())));

    /// <summary>Get points history (paginated).</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetLoyaltyHistoryQuery(GetUserId(), page, pageSize)));

    /// <summary>Redeem points for discount (1pt = ₹0.50).</summary>
    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem([FromBody] RedeemRequest req)
        => Ok(await mediator.Send(new RedeemPointsCommand(GetUserId(), req.PointsToRedeem)));

    /// <summary>Get own referral code + stats.</summary>
    [HttpGet("referral/my-code")]
    public async Task<IActionResult> GetMyReferralCode()
    {
        try { return Ok(await mediator.Send(new GetMyReferralCodeQuery(GetUserId()))); }
        catch (Domain.Exceptions.NotFoundException)
        {
            // Auto-create referral code if not exists
            return Ok(await mediator.Send(new CreateReferralCodeCommand(GetUserId())));
        }
    }

    /// <summary>Referral leaderboard — top 10 referrers this month.</summary>
    [HttpGet("referral/leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 10)
        => Ok(await mediator.Send(new GetReferralLeaderboardQuery(top)));

    /// <summary>Apply a referral code (called during registration flow).</summary>
    [HttpPost("referral/apply")]
    public async Task<IActionResult> ApplyReferralCode([FromBody] ApplyReferralRequest req)
        => Ok(await mediator.Send(new EarnReferralBonusCommand(req.ReferralCode, GetUserId())));
}

public record RedeemRequest(int PointsToRedeem);
public record ApplyReferralRequest(string ReferralCode);
