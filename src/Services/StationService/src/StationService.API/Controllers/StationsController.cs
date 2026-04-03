using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StationService.Application.Commands;
using StationService.Application.DTOs;
using StationService.Application.Queries;

namespace StationService.API.Controllers;

[ApiController]
[Route("api/stations")]
public class StationsController(IMediator mediator) : ControllerBase
{
    /// <summary>Get all stations (paginated with filters).</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetStations(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? city = null, [FromQuery] string? state = null,
        [FromQuery] bool? isActive = null, [FromQuery] string? search = null)
    {
        var result = await mediator.Send(new GetStationsQuery(page, pageSize, city, state, isActive, search));
        return Ok(result);
    }

    /// <summary>Get station by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetStationById(Guid id)
    {
        var result = await mediator.Send(new GetStationByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Create a new station (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStation([FromBody] CreateStationRequest dto)
    {
        var result = await mediator.Send(new CreateStationCommand(
            dto.StationCode, dto.StationName, dto.DealerUserId,
            dto.AddressLine1, dto.City, dto.State, dto.PinCode,
            dto.Latitude, dto.Longitude, dto.LicenseNumber,
            dto.OperatingHoursStart, dto.OperatingHoursEnd, dto.Is24x7));
        return StatusCode(201, result);
    }

    /// <summary>Update a station (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStation(Guid id, [FromBody] UpdateStationRequest dto)
    {
        var result = await mediator.Send(new UpdateStationCommand(
            id, dto.StationName, dto.AddressLine1, dto.City, dto.State,
            dto.PinCode, dto.Latitude, dto.Longitude,
            dto.OperatingHoursStart, dto.OperatingHoursEnd, dto.Is24x7));
        return Ok(result);
    }

    /// <summary>Soft-delete (deactivate) a station (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeactivateStation(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await mediator.Send(new DeactivateStationCommand(id, userId));
        return Ok(result);
    }

    /// <summary>Get nearby stations (public).</summary>
    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNearbyStations(
        [FromQuery] decimal lat, [FromQuery] decimal lng,
        [FromQuery] double radiusKm = 10, [FromQuery] Guid? fuelTypeId = null)
    {
        var result = await mediator.Send(new GetNearbyStationsQuery(lat, lng, radiusKm, fuelTypeId));
        return Ok(result);
    }

    /// <summary>Assign a dealer to a station (Admin only).</summary>
    [HttpPut("{id:guid}/dealer")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AssignDealer(Guid id, [FromBody] AssignDealerRequest dto)
    {
        var result = await mediator.Send(new AssignDealerCommand(id, dto.DealerUserId));
        return Ok(result);
    }
}
