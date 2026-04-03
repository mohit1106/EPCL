using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StationService.Application.Commands;
using StationService.Application.DTOs;
using StationService.Application.Queries;

namespace StationService.API.Controllers;

[ApiController]
[Route("api/stations/fuel-types")]
public class FuelTypesController(IMediator mediator) : ControllerBase
{
    /// <summary>Get all fuel types (public, cacheable).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetFuelTypes([FromQuery] bool? isActive = null)
    {
        var result = await mediator.Send(new GetFuelTypesQuery(isActive));
        return Ok(result);
    }

    /// <summary>Create a new fuel type (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FuelTypeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFuelType([FromBody] CreateFuelTypeRequest dto)
    {
        var result = await mediator.Send(new CreateFuelTypeCommand(dto.Name, dto.Description));
        return StatusCode(201, result);
    }

    /// <summary>Update a fuel type (Admin only).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateFuelType(Guid id, [FromBody] UpdateFuelTypeRequest dto)
    {
        var result = await mediator.Send(new UpdateFuelTypeCommand(id, dto.Name, dto.Description, dto.IsActive));
        return Ok(result);
    }
}
