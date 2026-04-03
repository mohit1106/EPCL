using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesService.Application.Commands;
using SalesService.Application.DTOs;
using SalesService.Application.Queries;

namespace SalesService.API.Controllers;

/// <summary>Fuel sale transactions.</summary>
[ApiController]
[Route("api/sales")]
[Authorize]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    /// <summary>Record a new fuel sale (Saga Step 1).</summary>
    [HttpPost("transactions")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> RecordSale([FromBody] RecordFuelSaleRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await mediator.Send(new RecordFuelSaleCommand(
            body.StationId, body.PumpId, body.TankId, body.FuelTypeId,
            userId, body.CustomerUserId, body.VehicleNumber,
            body.QuantityLitres, body.PaymentMethod, body.PaymentReferenceId));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get paginated transactions.</summary>
    [HttpGet("transactions")]
    [Authorize(Roles = "Admin,SuperAdmin,Dealer")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? stationId = null, [FromQuery] string? vehicleNumber = null, [FromQuery] string? status = null)
    {
        var dealerId = User.IsInRole("Dealer") ? Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : (Guid?)null;
        return Ok(await mediator.Send(new GetTransactionsQuery(page, pageSize, stationId, dealerId, null, vehicleNumber, status)));
    }

    /// <summary>Get a single transaction.</summary>
    [HttpGet("transactions/{id}")]
    public async Task<IActionResult> GetById(Guid id) => Ok(await mediator.Send(new GetTransactionByIdQuery(id)));

    /// <summary>Get transactions by station.</summary>
    [HttpGet("transactions/station/{stationId}")]
    [Authorize(Roles = "Admin,SuperAdmin,Dealer")]
    public async Task<IActionResult> GetByStation(Guid stationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetTransactionsQuery(page, pageSize, stationId)));

    /// <summary>Get customer's own transactions.</summary>
    [HttpGet("transactions/my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetTransactionsQuery(page, pageSize, CustomerId: userId)));
    }

    /// <summary>Get transactions by vehicle number.</summary>
    [HttpGet("transactions/vehicle/{vehicleNumber}")]
    [Authorize(Roles = "Admin,SuperAdmin,Dealer")]
    public async Task<IActionResult> GetByVehicle(string vehicleNumber, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetTransactionsQuery(page, pageSize, VehicleNumber: vehicleNumber)));

    /// <summary>Void a transaction.</summary>
    [HttpPost("transactions/{id}/void")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> VoidTransaction(Guid id, [FromBody] VoidTransactionRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new VoidTransactionCommand(id, userId, body.Reason)));
    }

    /// <summary>Get receipt data.</summary>
    [HttpGet("transactions/{id}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id) => Ok(await mediator.Send(new GetTransactionByIdQuery(id)));
}
