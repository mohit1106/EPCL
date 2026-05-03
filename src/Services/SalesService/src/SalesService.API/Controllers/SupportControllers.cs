using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesService.Application.Commands;
using SalesService.Application.DTOs;
using SalesService.Application.Queries;

namespace SalesService.API.Controllers;

/// <summary>Pump management.</summary>
[ApiController]
[Route("api/sales/pumps")]
[Authorize]
public class PumpsController(IMediator mediator) : ControllerBase
{
    [HttpGet("station/{stationId}")]
    public async Task<IActionResult> GetByStation(Guid stationId) => Ok(await mediator.Send(new GetPumpsByStationQuery(stationId)));

    [HttpPost]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Register([FromBody] RegisterPumpRequest body)
        => CreatedAtAction(nameof(GetByStation), new { stationId = body.StationId },
            await mediator.Send(new RegisterPumpCommand(body.StationId, body.FuelTypeId, body.PumpName, body.NozzleCount)));

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePumpStatusRequest body)
        => Ok(await mediator.Send(new UpdatePumpStatusCommand(id, body.Status, body.Notes)));

    [HttpDelete("{id}")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeletePumpCommand(id));
        return NoContent();
    }
}

/// <summary>Fuel price management.</summary>
[ApiController]
[Route("api/sales/fuel-prices")]
public class FuelPricesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await mediator.Send(new GetActiveFuelPricesQuery()));

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SetPrice([FromBody] SetFuelPriceRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new SetFuelPriceCommand(body.FuelTypeId, body.PricePerLitre, body.EffectiveFrom, userId)));
    }
}

/// <summary>Shift management.</summary>
[ApiController]
[Route("api/sales/shifts")]
[Authorize]
public class ShiftsController(IMediator mediator) : ControllerBase
{
    [HttpPost("start")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> StartShift([FromBody] StartShiftRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new StartShiftCommand(userId, body.StationId, body.Notes)));
    }

    [HttpPost("end")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> EndShift([FromBody] EndShiftRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new EndShiftCommand(userId, body.Notes)));
    }

    [HttpGet("current")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> GetCurrent()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var shift = await mediator.Send(new GetActiveShiftQuery(userId));
        return shift == null ? NotFound(new { message = "No active shift." }) : Ok(shift);
    }

    [HttpGet("history/{stationId}")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> GetHistory(Guid stationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return Ok(await mediator.Send(new GetShiftHistoryQuery(stationId, page, pageSize)));
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] Guid? stationId = null)
    {
        return Ok(await mediator.Send(new GetAllShiftsQuery(page, pageSize, stationId)));
    }
}
