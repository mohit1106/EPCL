using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuditService.Application.Queries;

namespace AuditService.API.Controllers;

/// <summary>Audit log viewer — read-only, Admin access only.</summary>
[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AuditController(IMediator mediator) : ControllerBase
{
    /// <summary>Get paginated audit logs with filters.</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? operation = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await mediator.Send(new GetAuditLogsQuery(entityType, userId, operation, serviceName, dateFrom, dateTo, page, pageSize)));

    /// <summary>Get a single audit log entry with old/new values.</summary>
    [HttpGet("logs/{id}")]
    public async Task<IActionResult> GetLog(Guid id)
        => Ok(await mediator.Send(new GetAuditLogByIdQuery(id)));

    /// <summary>Export audit trail (all matching records, no pagination).</summary>
    [HttpPost("logs/export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? operation = null,
        [FromQuery] DateTimeOffset? dateFrom = null,
        [FromQuery] DateTimeOffset? dateTo = null)
        => Ok(await mediator.Send(new ExportAuditLogQuery(entityType, userId, operation, dateFrom, dateTo)));
}
