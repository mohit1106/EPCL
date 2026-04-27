using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SalesService.API.Controllers
{
    [ApiController]
    [Route("api/driver")]
    [Authorize(Roles = "Driver")]
    public class DriverController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DriverController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("nearest-station")]
        public async Task<IActionResult> GetNearestStation([FromQuery] double lat, [FromQuery] double lng)
        {
            // Simplified proxy to StationService, or mock for now
            return Ok(new { message = "Nearest station found", stationId = Guid.NewGuid() });
        }

        [HttpGet("fuel-prices")]
        public async Task<IActionResult> GetFuelPrices()
        {
            // Would fetch prices from FuelPrice endpoint/repo
            return Ok(new { message = "Current prices" });
        }

        [HttpPost("pre-authorize")]
        public async Task<IActionResult> PreAuthorize([FromBody] PreAuthorizeRequest request)
        {
            var userIdStr = User.FindFirst("userId")?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            // 1. Fetch Fleet Account for Driver (mocking to any active fleet account here)
            var fleetAccountId = Guid.NewGuid(); // To be handled by actual DB fetch
            
            // 2. We'll generate an auth code and save to DB
            var command = new Application.Commands.CreatePreAuthorizationCommand(
                userId, fleetAccountId, request.VehicleId, request.StationId, 
                request.FuelTypeId, request.RequestedAmountINR);

            var result = await _mediator.Send(command);

            return Ok(new
            {
                AuthCode = result.AuthCode,
                ExpiresAt = result.ExpiresAt,
                AuthorizedAmount = result.AuthorizedAmountINR
            });
        }

        [HttpGet("my-transactions")]
        public async Task<IActionResult> GetMyTransactions()
        {
            // Placeholder: driver transactions
            return Ok(new { message = "Transactions list" });
        }

        [HttpGet("fleet-balance")]
        public async Task<IActionResult> GetFleetBalance()
        {
            // Placeholder: fleet balance
            return Ok(new { CreditLimit = 50000, CurrentBalance = 12000 });
        }

        [HttpGet("pre-authorizations")]
        public async Task<IActionResult> GetPreAuthorizations()
        {
            // Placeholder: Active pre-authorizations
            return Ok(new { message = "Active authorizations list" });
        }
    }

    public class PreAuthorizeRequest
    {
        public Guid StationId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid FuelTypeId { get; set; }
        public decimal RequestedAmountINR { get; set; }
    }
}
