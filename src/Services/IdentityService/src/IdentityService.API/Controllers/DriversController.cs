using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.API.Controllers;

/// <summary>
/// Driver management endpoints — admins manage delivery drivers.
/// </summary>
[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriversController(IdentityDbContext db) : ControllerBase
{
    /// <summary>Get all drivers.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var drivers = await db.Drivers.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return Ok(drivers.Select(MapToDto).ToList());
    }

    /// <summary>Get available drivers only.</summary>
    [HttpGet("available")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAvailable()
    {
        var drivers = await db.Drivers.Where(d => d.IsAvailable).OrderBy(d => d.FullName).ToListAsync();
        return Ok(drivers.Select(MapToDto).ToList());
    }

    /// <summary>Get a single driver.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver == null) return NotFound();
        return Ok(MapToDto(driver));
    }

    /// <summary>Create a new driver.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
    {
        var driver = new Driver
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            LicenseNumber = dto.LicenseNumber,
            VehicleNumber = dto.VehicleNumber,
            DriverCode = GenerateDriverCode(),
        };

        db.Drivers.Add(driver);
        await db.SaveChangesAsync();

        return Created($"/api/drivers/{driver.Id}", MapToDto(driver));
    }

    /// <summary>Update a driver.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDriverDto dto)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver == null) return NotFound();

        if (dto.FullName != null) driver.FullName = dto.FullName;
        if (dto.Phone != null) driver.Phone = dto.Phone;
        if (dto.LicenseNumber != null) driver.LicenseNumber = dto.LicenseNumber;
        if (dto.VehicleNumber != null) driver.VehicleNumber = dto.VehicleNumber;
        driver.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(MapToDto(driver));
    }

    /// <summary>Delete a driver.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver == null) return NotFound();
        if (!driver.IsAvailable)
            return BadRequest("Cannot delete a driver currently assigned to a request.");

        db.Drivers.Remove(driver);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Release a driver (mark as available) — used when replenishment completes.</summary>
    [HttpPut("{id:guid}/release")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Release(Guid id)
    {
        var driver = await db.Drivers.FindAsync(id);
        if (driver == null) return NotFound();
        driver.IsAvailable = true;
        driver.CurrentRequestId = null;
        driver.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(MapToDto(driver));
    }

    private static string GenerateDriverCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new char[4];
        for (int i = 0; i < 4; i++) code[i] = chars[random.Next(chars.Length)];
        return $"DRV-{new string(code)}";
    }

    private static DriverDto MapToDto(Driver d) => new()
    {
        Id = d.Id,
        DriverCode = d.DriverCode,
        FullName = d.FullName,
        Phone = d.Phone,
        LicenseNumber = d.LicenseNumber,
        VehicleNumber = d.VehicleNumber,
        IsAvailable = d.IsAvailable,
        CurrentRequestId = d.CurrentRequestId,
        CreatedAt = d.CreatedAt,
    };
}
