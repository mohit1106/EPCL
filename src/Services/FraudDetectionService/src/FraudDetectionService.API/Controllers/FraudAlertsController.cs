using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FraudDetectionService.Application.Commands;
using FraudDetectionService.Application.DTOs;
using FraudDetectionService.Application.Queries;

namespace FraudDetectionService.API.Controllers;

/// <summary>Fraud alert management.</summary>
[ApiController]
[Route("api/fraud")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class FraudAlertsController(IMediator mediator) : ControllerBase
{
    /// <summary>Get paginated fraud alerts.</summary>
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] string? severity = null,
        [FromQuery] Guid? stationId = null,
        [FromQuery] DateTimeOffset? dateFrom = null, [FromQuery] DateTimeOffset? dateTo = null)
        => Ok(await mediator.Send(new GetFraudAlertsQuery(page, pageSize, status, severity, stationId, dateFrom, dateTo)));

    /// <summary>Get a single fraud alert.</summary>
    [HttpGet("alerts/{id}")]
    public async Task<IActionResult> GetAlert(Guid id) => Ok(await mediator.Send(new GetFraudAlertByIdQuery(id)));

    /// <summary>Dismiss a fraud alert.</summary>
    [HttpPut("alerts/{id}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, [FromBody] DismissAlertRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new DismissAlertCommand(id, userId, body.Notes)));
    }

    /// <summary>Mark alert for investigation.</summary>
    [HttpPut("alerts/{id}/investigate")]
    public async Task<IActionResult> Investigate(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new InvestigateAlertCommand(id, userId)));
    }

    /// <summary>Escalate a fraud alert.</summary>
    [HttpPut("alerts/{id}/escalate")]
    public async Task<IActionResult> Escalate(Guid id, [FromBody] EscalateAlertRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new EscalateAlertCommand(id, userId, body.Notes)));
    }

    /// <summary>Bulk dismiss alerts.</summary>
    [HttpPost("alerts/bulk-dismiss")]
    public async Task<IActionResult> BulkDismiss([FromBody] BulkDismissRequest body)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new BulkDismissAlertsCommand(body.AlertIds, userId, body.Notes)));
    }

    /// <summary>Get fraud statistics.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? stationId = null,
        [FromQuery] DateTimeOffset? dateFrom = null, [FromQuery] DateTimeOffset? dateTo = null)
        => Ok(await mediator.Send(new GetFraudStatsQuery(stationId, dateFrom, dateTo)));
}
