using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Application.Commands;
using ReportingService.Application.DTOs;
using ReportingService.Application.Queries;
using ReportingService.Domain.Interfaces;

namespace ReportingService.API.Controllers;

/// <summary>Report generation and data queries.</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>Get sales summary with filters.</summary>
    [HttpGet("sales-summary")]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] Guid? stationId, [FromQuery] Guid? fuelTypeId,
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
        => Ok(await mediator.Send(new GetSalesSummaryQuery(stationId, fuelTypeId, dateFrom, dateTo)));

    /// <summary>Get top-performing stations.</summary>
    [HttpGet("station-performance")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetStationPerformance(
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, [FromQuery] int top = 10)
        => Ok(await mediator.Send(new GetStationPerformanceQuery(dateFrom, dateTo, top)));

    /// <summary>Get dealer summary (monthly breakdown).</summary>
    [HttpGet("dealer-summary/{stationId}")]
    public async Task<IActionResult> GetDealerSummary(Guid stationId,
        [FromQuery] int? month, [FromQuery] int? year)
        => Ok(await mediator.Send(new GetDealerSummaryQuery(stationId, month, year)));

    /// <summary>Export report as PDF.</summary>
    [HttpPost("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromBody] ExportReportRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GeneratePdfReportCommand(userId, req.ReportType, req.DateFrom, req.DateTo, req.StationId)));
    }

    /// <summary>Export report as Excel.</summary>
    [HttpPost("export/excel")]
    public async Task<IActionResult> ExportExcel([FromBody] ExportReportRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new GenerateExcelReportCommand(userId, req.ReportType, req.DateFrom, req.DateTo, req.StationId)));
    }

    /// <summary>Check report generation status.</summary>
    [HttpGet("exports/{reportId}/status")]
    public async Task<IActionResult> GetStatus(Guid reportId)
        => Ok(await mediator.Send(new GetReportStatusQuery(reportId)));

    /// <summary>Download generated report file.</summary>
    [HttpGet("exports/{reportId}/download")]
    public async Task<IActionResult> Download(Guid reportId)
    {
        var report = await mediator.Send(new GetReportStatusQuery(reportId));
        if (report.Status != "Ready") return BadRequest(new { message = "Report not ready yet." });

        var repo = HttpContext.RequestServices.GetRequiredService<IGeneratedReportRepository>();
        var entity = await repo.GetByIdAsync(reportId);
        if (entity == null || !System.IO.File.Exists(entity.FilePath))
            return NotFound(new { message = "Report file not found." });

        var contentType = entity.Format == "PDF" ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var ext = entity.Format == "PDF" ? "pdf" : "xlsx";
        return PhysicalFile(entity.FilePath, contentType, $"{entity.ReportType}_{entity.Id}.{ext}");
    }
    /// <summary>Get predictive stock intelligence for a station.</summary>
    [HttpGet("stock-predictions")]
    public async Task<IActionResult> GetStockPredictions([FromQuery] Guid? stationId)
        => Ok(await mediator.Send(new GetStockPredictionsQuery(stationId)));

    /// <summary>Get at-risk tanks globally (Admin) or per station (Dealer).</summary>
    [HttpGet("stock-predictions/at-risk")]
    public async Task<IActionResult> GetAtRiskStockPredictions(
        [FromQuery] int daysThreshold = 7, [FromQuery] Guid? stationId = null)
        => Ok(await mediator.Send(new GetAtRiskStockPredictionsQuery(daysThreshold, stationId)));
}

/// <summary>KPI dashboard endpoints.</summary>
[ApiController]
[Route("api/reports/kpi")]
[Authorize]
public class KpiController(IMediator mediator) : ControllerBase
{
    /// <summary>Admin dashboard KPIs.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAdminKpi()
        => Ok(await mediator.Send(new GetAdminKpiQuery()));

    /// <summary>Dealer dashboard KPIs.</summary>
    [HttpGet("dealer/{stationId}")]
    public async Task<IActionResult> GetDealerKpi(Guid stationId)
        => Ok(await mediator.Send(new GetDealerKpiQuery(stationId)));
}

/// <summary>Scheduled report management.</summary>
[ApiController]
[Route("api/reports/schedule")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ScheduledReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>Create a scheduled report.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduledReportRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new CreateScheduledReportCommand(userId, req.ReportType, req.CronExpression, req.StationId, req.Format)));
    }

    /// <summary>List all active scheduled reports.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await mediator.Send(new GetScheduledReportsQuery()));

    /// <summary>Delete a scheduled report.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(await mediator.Send(new DeleteScheduledReportCommand(id)));
}
