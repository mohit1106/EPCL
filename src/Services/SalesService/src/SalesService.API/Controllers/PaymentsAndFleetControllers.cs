using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesService.Application.Commands;
using SalesService.Application.DTOs;
using SalesService.Application.Queries;

namespace SalesService.API.Controllers;

/// <summary>Customer wallet + Razorpay payments.</summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Create Razorpay order for wallet top-up.</summary>
    [HttpPost("wallet/create-order")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateWalletOrderRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new CreateWalletOrderCommand(userId, body.Amount)));
    }

    /// <summary>Verify Razorpay payment and credit wallet.</summary>
    [HttpPost("wallet/verify")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyWalletPaymentRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new VerifyWalletPaymentCommand(userId, body.OrderId, body.PaymentId, body.Signature)));
    }

    /// <summary>Get wallet balance.</summary>
    [HttpGet("wallet/balance")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetWalletBalanceQuery(userId)));
    }

    /// <summary>Get wallet transaction history.</summary>
    [HttpGet("wallet/history")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetWalletHistoryQuery(userId, page, pageSize)));
    }
}

/// <summary>Fleet account management.</summary>
[ApiController]
[Route("api/fleet")]
[Authorize]
public class FleetController(IMediator mediator) : ControllerBase
{
    [HttpGet("accounts")]
    [Authorize(Roles = "Admin,SuperAdmin,Customer")]
    public async Task<IActionResult> GetAccounts() => Ok(await mediator.Send(new GetFleetAccountsQuery()));

    [HttpPost("accounts")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateFleetAccountRequest body)
        => CreatedAtAction(nameof(GetAccounts), null, await mediator.Send(new CreateFleetAccountCommand(body.CompanyName, body.ContactUserId, body.CreditLimit)));

    [HttpPost("accounts/{accountId}/vehicles")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AddVehicle(Guid accountId, [FromBody] AddFleetVehicleRequest body)
        => Ok(await mediator.Send(new AddVehicleToFleetCommand(accountId, body.VehicleId, body.DailyLimitLitres, body.MonthlyLimitAmount)));

    [HttpDelete("accounts/{accountId}/vehicles/{vehicleId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RemoveVehicle(Guid accountId, Guid vehicleId)
    { await mediator.Send(new RemoveFleetVehicleCommand(accountId, vehicleId)); return NoContent(); }
}

/// <summary>Customer vehicle registration.</summary>
[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController(IMediator mediator, SalesService.Domain.Interfaces.IRegisteredVehicleRepository vehicleRepo) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyVehicles()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetCustomerVehiclesQuery(userId)));
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Register([FromBody] RegisterVehicleRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return CreatedAtAction(nameof(GetMyVehicles), null,
            await mediator.Send(new RegisterVehicleCommand(userId, body.RegistrationNumber, body.FuelTypePreference, body.VehicleType, body.Nickname)));
    }

    /// <summary>Lookup a vehicle by registration number (for dealers creating sales).</summary>
    [HttpGet("lookup/{registrationNumber}")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> Lookup(string registrationNumber)
    {
        var normalized = registrationNumber.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        var vehicle = await vehicleRepo.GetByRegistrationAsync(normalized);
        if (vehicle == null) return NotFound(new { message = $"No vehicle found with registration {normalized}." });
        return Ok(new
        {
            vehicle.Id,
            vehicle.CustomerId,
            vehicle.RegistrationNumber,
            FuelTypePreference = vehicle.FuelTypePreference?.ToString(),
            VehicleType = vehicle.VehicleType.ToString(),
            vehicle.Nickname,
            vehicle.IsActive,
            RegisteredAt = vehicle.RegisteredAt
        });
    }
}
