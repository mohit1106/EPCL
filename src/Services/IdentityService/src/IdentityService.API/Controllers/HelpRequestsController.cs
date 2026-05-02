using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IdentityService.Application.DTOs;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.API.Controllers;

/// <summary>
/// Help request endpoints — dealers create requests, admins manage them.
/// </summary>
[ApiController]
[Route("api/help-requests")]
[Authorize]
public class HelpRequestsController(IdentityDbContext db) : ControllerBase
{
    /// <summary>Create a new help request (dealer).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HelpRequestDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateHelpRequestDto dto)
    {
        var userId = GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        var entity = new HelpRequest
        {
            DealerUserId = userId,
            DealerEmail = user.Email,
            DealerName = user.FullName,
            TargetAdminId = dto.TargetAdminId,
            TargetAdminName = dto.TargetAdminName,
            Category = dto.Category,
            Message = dto.Message,
        };

        db.HelpRequests.Add(entity);
        await db.SaveChangesAsync();

        return Created($"/api/help-requests/{entity.Id}", MapToDto(entity));
    }

    /// <summary>Get help requests for the current user (dealer sees own, admin sees all).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<HelpRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var userId = GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var role = user.Role.ToString();
        IQueryable<HelpRequest> query = db.HelpRequests.Include(h => h.Replies.OrderBy(r => r.CreatedAt));

        if (role == "Admin" || role == "SuperAdmin")
        {
            // Admins see all requests
        }
        else
        {
            // Dealers see only their own
            query = query.Where(h => h.DealerUserId == userId);
        }

        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(h => h.Status == status);

        var items = await query.OrderByDescending(h => h.CreatedAt).ToListAsync();
        return Ok(items.Select(MapToDto).ToList());
    }

    /// <summary>Get a single help request by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HelpRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await db.HelpRequests.Include(h => h.Replies.OrderBy(r => r.CreatedAt)).FirstOrDefaultAsync(h => h.Id == id);
        if (entity == null) return NotFound();
        return Ok(MapToDto(entity));
    }

    /// <summary>Update help request status (admin only).</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(HelpRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateHelpRequestStatusDto dto)
    {
        var entity = await db.HelpRequests.Include(h => h.Replies).FirstOrDefaultAsync(h => h.Id == id);
        if (entity == null) return NotFound();

        entity.Status = dto.Status;
        if (dto.Status == "Resolved") entity.ResolvedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return Ok(MapToDto(entity));
    }

    /// <summary>Add a reply to a help request.</summary>
    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(HelpRequestReplyDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddReply(Guid id, [FromBody] CreateHelpReplyDto dto)
    {
        var entity = await db.HelpRequests.FindAsync(id);
        if (entity == null) return NotFound();

        var userId = GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var role = user.Role.ToString();
        var fromRole = (role == "Admin" || role == "SuperAdmin") ? "admin" : "dealer";

        var reply = new HelpRequestReply
        {
            HelpRequestId = id,
            FromRole = fromRole,
            FromName = user.FullName,
            FromUserId = userId,
            Message = dto.Message,
        };

        db.HelpRequestReplies.Add(reply);
        await db.SaveChangesAsync();

        return Created($"/api/help-requests/{id}/replies/{reply.Id}", new HelpRequestReplyDto
        {
            Id = reply.Id,
            FromRole = reply.FromRole,
            FromName = reply.FromName,
            FromUserId = reply.FromUserId,
            Message = reply.Message,
            CreatedAt = reply.CreatedAt,
        });
    }

    private static HelpRequestDto MapToDto(HelpRequest h) => new()
    {
        Id = h.Id,
        DealerUserId = h.DealerUserId,
        DealerEmail = h.DealerEmail,
        DealerName = h.DealerName,
        TargetAdminId = h.TargetAdminId,
        TargetAdminName = h.TargetAdminName,
        Category = h.Category,
        Message = h.Message,
        Status = h.Status,
        CreatedAt = h.CreatedAt,
        ResolvedAt = h.ResolvedAt,
        Replies = h.Replies.Select(r => new HelpRequestReplyDto
        {
            Id = r.Id,
            FromRole = r.FromRole,
            FromName = r.FromName,
            FromUserId = r.FromUserId,
            Message = r.Message,
            CreatedAt = r.CreatedAt,
        }).ToList(),
    };

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return Guid.Parse(claim);
    }
}
