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

    /// <summary>Get pending wallet payment requests (customer).</summary>
    [HttpGet("wallet/pending-requests")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetPendingPaymentRequestsQuery(userId)));
    }

    /// <summary>Get all payment requests including history (customer).</summary>
    [HttpGet("wallet/all-requests")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetAllRequests()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GetAllPaymentRequestsQuery(userId)));
    }

    /// <summary>Approve a wallet payment request (customer).</summary>
    [HttpPost("wallet/approve/{requestId}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> ApproveRequest(Guid requestId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new ApproveWalletPaymentCommand(userId, requestId)));
    }

    /// <summary>Reject a wallet payment request (customer).</summary>
    [HttpPost("wallet/reject/{requestId}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> RejectRequest(Guid requestId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new RejectWalletPaymentCommand(userId, requestId)));
    }

    /// <summary>Create wallet payment request (dealer, during sale).</summary>
    [HttpPost("wallet/request")]
    [Authorize(Roles = "Dealer")]
    public async Task<IActionResult> CreateWalletPaymentRequest([FromBody] CreateWalletPaymentRequestDto body)
    {
        var dealerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await mediator.Send(new GetActiveShiftQuery(dealerId));
        var stationId = user?.StationId ?? Guid.Empty;
        return Ok(await mediator.Send(new CreateWalletPaymentRequestCommand(
            dealerId, stationId, body.SaleTransactionId, body.CustomerId,
            body.Amount, body.Description, body.VehicleNumber, body.FuelTypeName, body.QuantityLitres,
            body.PaymentMethod)));
    }

    /// <summary>Create Razorpay order to pay a pending payment request (for UPI/Bank).</summary>
    [HttpPost("request/{requestId}/create-order")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateRequestPaymentOrder(Guid requestId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new CreatePaymentRequestOrderCommand(userId, requestId)));
    }

    /// <summary>Verify Razorpay payment for a pending payment request.</summary>
    [HttpPost("request/{requestId}/verify")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> VerifyRequestPayment(Guid requestId, [FromBody] VerifyWalletPaymentRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new VerifyPaymentRequestCommand(
            userId, requestId, body.OrderId, body.PaymentId, body.Signature)));
    }

    /// <summary>Get customer wallet balance (for dealers during sale).</summary>
    [HttpGet("wallet/customer-balance/{customerId}")]
    [Authorize(Roles = "Dealer,Admin,SuperAdmin")]
    public async Task<IActionResult> GetCustomerBalance(Guid customerId)
    {
        var result = await mediator.Send(new GetCustomerWalletBalanceQuery(customerId));
        if (result == null) return NotFound(new { message = "Customer has no wallet." });
        return Ok(result);
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vehicle = await vehicleRepo.GetByIdAsync(id);
        if (vehicle == null || vehicle.CustomerId != userId) return NotFound(new { message = "Vehicle not found." });
        await vehicleRepo.DeleteAsync(vehicle);
        return NoContent();
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
