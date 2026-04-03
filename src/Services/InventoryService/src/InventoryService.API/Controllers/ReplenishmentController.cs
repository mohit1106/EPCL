using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.Commands;
using InventoryService.Application.DTOs;
using InventoryService.Application.Queries;

namespace InventoryService.API.Controllers;

/// <summary>Manage replenishment requests.</summary>
[ApiController]
[Route("api/inventory/replenishment-requests")]
[Authorize]
public class ReplenishmentController(IMediator mediator) : ControllerBase
{
    /// <summary>Submit a replenishment request.</summary>
    [HttpPost]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Submit([FromBody] SubmitReplenishmentRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.Send(new SubmitReplenishmentCommand(
            body.StationId, body.TankId, userId,
            body.RequestedQuantityLitres, body.UrgencyLevel, body.Notes));
        return CreatedAtAction(nameof(GetAll), null, result);
    }

    /// <summary>List all replenishment requests with optional filters.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] Guid? stationId = null)
        => Ok(await mediator.Send(new GetReplenishmentRequestsQuery(page, pageSize, status, stationId)));

    /// <summary>Approve a replenishment request.</summary>
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewReplenishmentRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new ApproveReplenishmentCommand(id, userId, body.Notes)));
    }

    /// <summary>Reject a replenishment request.</summary>
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectReplenishmentRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new RejectReplenishmentCommand(id, userId, body.Reason)));
    }

    /// <summary>Mark replenishment as dispatched.</summary>
    [HttpPut("{id}/dispatch")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Dispatch(Guid id)
        => Ok(await mediator.Send(new MarkDispatchedCommand(id)));

    /// <summary>Mark replenishment as delivered.</summary>
    [HttpPut("{id}/deliver")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Deliver(Guid id)
        => Ok(await mediator.Send(new MarkDeliveredCommand(id)));
}
