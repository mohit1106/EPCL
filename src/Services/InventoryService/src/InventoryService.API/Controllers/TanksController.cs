using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.Commands;
using InventoryService.Application.DTOs;
using InventoryService.Application.Queries;

namespace InventoryService.API.Controllers;

/// <summary>Manage tanks and stock operations.</summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
public class TanksController(IMediator mediator) : ControllerBase
{
    /// <summary>Get all tanks for a station.</summary>
    [HttpGet("stations/{stationId}/tanks")]
    public async Task<IActionResult> GetTanksByStation(Guid stationId)
        => Ok(await mediator.Send(new GetTanksByStationQuery(stationId)));

    /// <summary>Get a single tank by ID.</summary>
    [HttpGet("tanks/{tankId}")]
    public async Task<IActionResult> GetTankById(Guid tankId)
        => Ok(await mediator.Send(new GetTankByIdQuery(tankId)));

    /// <summary>Add a new tank to a station. Admin only.</summary>
    [HttpPost("tanks")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AddTank([FromBody] AddTankRequest body)
    {
        var result = await mediator.Send(new AddTankCommand(
            body.StationId, body.FuelTypeId, body.TankSerialNumber,
            body.CapacityLitres, body.CurrentStockLitres, body.MinThresholdLitres));
        return CreatedAtAction(nameof(GetTankById), new { tankId = result.Id }, result);
    }

    public record EnsureTankRequest(Guid FuelTypeId);

    /// <summary>Ensure a tank exists for a station and fuel type. Creates a default tank if missing.</summary>
    [HttpPost("stations/{stationId}/ensure-tank")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> EnsureTank(Guid stationId, [FromBody] EnsureTankRequest body)
    {
        var tanks = await mediator.Send(new GetTanksByStationQuery(stationId));
        var existing = tanks.FirstOrDefault(t => t.FuelTypeId == body.FuelTypeId);
        if (existing != null) return Ok(existing);

        // Auto-create a tank with default capacity (e.g., 5000L)
        var result = await mediator.Send(new AddTankCommand(
            stationId, body.FuelTypeId, $"AUTO-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            5000, 0, 500));
            
        return Ok(result);
    }

    /// <summary>Update tank configuration. Admin only.</summary>
    [HttpPut("tanks/{tankId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateTank(Guid tankId, [FromBody] UpdateTankRequest body)
        => Ok(await mediator.Send(new UpdateTankCommand(tankId, body.CapacityLitres, body.MinThresholdLitres, body.Status)));

    /// <summary>Record a stock loading delivery.</summary>
    [HttpPost("stock-loading")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> RecordStockLoading([FromBody] RecordStockLoadingRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.Send(new RecordStockLoadingCommand(
            body.TankId, body.QuantityLoadedLitres, userId,
            body.TankerNumber, body.InvoiceNumber, body.SupplierName, body.Notes));
        return Ok(result);
    }

    /// <summary>Get stock loading history for a tank.</summary>
    [HttpGet("stock-loading/{tankId}")]
    public async Task<IActionResult> GetStockLoadingHistory(Guid tankId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetStockLoadingHistoryQuery(tankId, page, pageSize)));

    /// <summary>Record a dip reading for a tank.</summary>
    [HttpPut("tanks/{tankId}/dip-reading")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> RecordDipReading(Guid tankId, [FromBody] RecordDipReadingRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new RecordDipReadingCommand(tankId, body.DipValueLitres, userId, body.Notes)));
    }

    /// <summary>Get dip reading history for a tank.</summary>
    [HttpGet("dip-readings/{tankId}")]
    public async Task<IActionResult> GetDipReadingHistory(Guid tankId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetDipReadingHistoryQuery(tankId, page, pageSize)));

    /// <summary>Get system-wide stock summary.</summary>
    [HttpGet("stock-summary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetStockSummary()
        => Ok(await mediator.Send(new GetStockSummaryQuery()));

    /// <summary>Get tanks below minimum threshold.</summary>
    [HttpGet("low-stock-alerts")]
    public async Task<IActionResult> GetLowStockAlerts()
        => Ok(await mediator.Send(new GetLowStockAlertsQuery()));
}
