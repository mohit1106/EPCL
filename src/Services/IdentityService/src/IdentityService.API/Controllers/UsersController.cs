using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Application.Commands.LockUser;
using IdentityService.Application.Commands.UpdateUserRole;
using IdentityService.Application.DTOs;
using IdentityService.Application.Queries.GetAllUsers;
using IdentityService.Application.Queries.GetCurrentUser;
using IdentityService.Domain.Enums;

namespace IdentityService.API.Controllers;

/// <summary>
/// User management endpoints — profile, admin user operations.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>Get the current authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetUserId();
        var result = await mediator.Send(new GetCurrentUserQuery(userId));
        return Ok(result);
    }

    /// <summary>Get all users (Admin only, paginated).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(Application.Common.PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        UserRole? roleFilter = null;
        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var parsed))
            roleFilter = parsed;

        var result = await mediator.Send(new GetAllUsersQuery(page, pageSize, roleFilter, isActive, search));
        return Ok(result);
    }

    /// <summary>Get a specific user by ID (Admin only).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(id));
        return Ok(result);
    }

    /// <summary>Update a user's role (Admin only).</summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequestDto dto)
    {
        var changedBy = GetUserId();
        var result = await mediator.Send(new UpdateUserRoleCommand(id, dto.Role, changedBy));
        return Ok(result);
    }

    /// <summary>Lock or unlock a user account (Admin only).</summary>
    [HttpPut("{id:guid}/lock")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserRequestDto dto)
    {
        var lockedBy = GetUserId();
        var result = await mediator.Send(new LockUserCommand(id, dto.IsLocked, dto.Reason, lockedBy));
        return Ok(result);
    }

    /// <summary>Soft-delete a user (Admin only).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var lockedBy = GetUserId();
        // Soft delete = deactivate
        await mediator.Send(new LockUserCommand(id, true, "Account deactivated by admin.", lockedBy));
        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return Guid.Parse(claim);
    }
}
