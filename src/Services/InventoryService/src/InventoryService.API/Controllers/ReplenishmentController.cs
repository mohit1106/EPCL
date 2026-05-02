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
            body.RequestedQuantityLitres, body.UrgencyLevel, body.Notes,
            body.TargetPumpName, body.FuelTypeName, body.Priority, body.RequestedWindow));
        return CreatedAtAction(nameof(GetAll), null, result);
    }

    /// <summary>List all replenishment requests with optional filters.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] Guid? stationId = null)
        => Ok(await mediator.Send(new GetReplenishmentRequestsQuery(page, pageSize, status, stationId)));

    /// <summary>Get replenishment requests for a specific station (dealer-accessible).</summary>
    [HttpGet("station/{stationId}")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> GetByStation(Guid stationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(await mediator.Send(new GetReplenishmentRequestsQuery(page, pageSize, null, stationId)));

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

    /// <summary>Assign a driver to a replenishment request.</summary>
    [HttpPut("{id}/assign-driver")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AssignDriver(Guid id, [FromBody] AssignDriverRequest body)
        => Ok(await mediator.Send(new AssignDriverCommand(id, body.DriverId, body.DriverName, body.DriverPhone, body.DriverCode)));

    /// <summary>Update replenishment status (TankerAssigned→InTransit→Offloading).</summary>
    [HttpPut("{id}/update-status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReplenishmentStatusRequest body)
        => Ok(await mediator.Send(new UpdateReplenishmentStatusCommand(id, body.Status)));

    /// <summary>Dealer verifies offloading with Order Number + Driver Code.</summary>
    [HttpPut("{id}/verify-offloading")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> VerifyOffloading(Guid id, [FromBody] VerifyOffloadingRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new VerifyOffloadingCommand(id, body.OrderNumber, body.DriverCode, userId)));
    }

    /// <summary>Mark replenishment as complete (requires dealer verification).</summary>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Complete(Guid id)
        => Ok(await mediator.Send(new CompleteReplenishmentCommand(id)));

    /// <summary>Mark replenishment as dispatched (legacy).</summary>
    [HttpPut("{id}/dispatch")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Dispatch(Guid id)
        => Ok(await mediator.Send(new MarkDispatchedCommand(id)));

    /// <summary>Mark replenishment as delivered (legacy).</summary>
    [HttpPut("{id}/deliver")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Deliver(Guid id)
        => Ok(await mediator.Send(new MarkDeliveredCommand(id)));
}
