using MediatR;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using ReportingService.Application.DTOs;
using ReportingService.Domain.Entities;
using ReportingService.Domain.Exceptions;
using ReportingService.Domain.Interfaces;

namespace ReportingService.Application.Commands;

// ══════════════════════════════════════════════════════════════════
// GeneratePdfReport
// ══════════════════════════════════════════════════════════════════
public record GeneratePdfReportCommand(Guid UserId, string ReportType,
    DateOnly? DateFrom, DateOnly? DateTo, Guid? StationId) : IRequest<GeneratedReportDto>;

public class GeneratePdfReportHandler(
    IGeneratedReportRepository reportRepo, IDailySalesSummaryRepository salesRepo,
    ILogger<GeneratePdfReportHandler> logger)
    : IRequestHandler<GeneratePdfReportCommand, GeneratedReportDto>
{
    public async Task<GeneratedReportDto> Handle(GeneratePdfReportCommand cmd, CancellationToken ct)
    {
        var report = new GeneratedReport
        {
            Id = Guid.NewGuid(), ReportType = cmd.ReportType, GeneratedByUserId = cmd.UserId,
            DateFrom = cmd.DateFrom, DateTo = cmd.DateTo, StationId = cmd.StationId,
            Format = "PDF", Status = "Generating"
        };
        await reportRepo.AddAsync(report, ct);

        try
        {
            var data = await salesRepo.GetAsync(cmd.StationId, null, cmd.DateFrom, cmd.DateTo, ct);
            var dir = Path.Combine(AppContext.BaseDirectory, "reports");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"{report.Id}.pdf");

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("EPCL").FontSize(28).Bold().FontColor("#1B3A6B");
                            col.Item().Text("Eleven Petroleum Corporation Limited").FontSize(10).FontColor("#64748B");
                        });
                        row.ConstantItem(120).AlignRight().Text(cmd.ReportType).FontSize(12).FontColor("#2563EB");
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text($"Report Period: {cmd.DateFrom} — {cmd.DateTo}").FontSize(11).FontColor("#475569");
                        col.Item().PaddingTop(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background("#1B3A6B").Padding(6).Text("Station").FontColor("#FFF").FontSize(10).Bold();
                                h.Cell().Background("#1B3A6B").Padding(6).Text("Transactions").FontColor("#FFF").FontSize(10).Bold();
                                h.Cell().Background("#1B3A6B").Padding(6).Text("Litres").FontColor("#FFF").FontSize(10).Bold();
                                h.Cell().Background("#1B3A6B").Padding(6).Text("Revenue (₹)").FontColor("#FFF").FontSize(10).Bold();
                            });

                            var grouped = data.GroupBy(d => d.StationId).ToList();
                            var alt = false;
                            foreach (var g in grouped)
                            {
                                var bg = alt ? "#F8FAFC" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(6).Text(g.Key.ToString()[..8]).FontSize(9);
                                table.Cell().Background(bg).Padding(6).Text(g.Sum(x => x.TotalTransactions).ToString()).FontSize(9);
                                table.Cell().Background(bg).Padding(6).Text(g.Sum(x => x.TotalLitresSold).ToString("N2")).FontSize(9);
                                table.Cell().Background(bg).Padding(6).Text(g.Sum(x => x.TotalRevenue).ToString("N2")).FontSize(9);
                                alt = !alt;
                            }
                        });

                        col.Item().PaddingTop(20).Text($"Total: {data.Sum(d => d.TotalLitresSold):N2} L | ₹{data.Sum(d => d.TotalRevenue):N2}")
                            .FontSize(12).Bold().FontColor("#059669");
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated: ").FontSize(8).FontColor("#94A3B8");
                        t.Span(DateTimeOffset.UtcNow.ToString("dd MMM yyyy HH:mm UTC")).FontSize(8).FontColor("#64748B");
                    });
                });
            }).GeneratePdf(filePath);

            var fileInfo = new FileInfo(filePath);
            report.FilePath = filePath;
            report.FileSize = fileInfo.Length;
            report.Status = "Ready";
            report.GeneratedAt = DateTimeOffset.UtcNow;
            report.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
            await reportRepo.UpdateAsync(report, ct);
            logger.LogInformation("PDF report generated: {Id} ({Size} bytes)", report.Id, report.FileSize);
        }
        catch (Exception ex)
        {
            report.Status = "Failed";
            await reportRepo.UpdateAsync(report, ct);
            logger.LogError(ex, "Failed to generate PDF report {Id}", report.Id);
            throw;
        }

        return new GeneratedReportDto(report.Id, report.ReportType, report.Format, report.Status,
            report.DateFrom, report.DateTo, report.StationId, report.FileSize,
            report.GeneratedAt, report.ExpiresAt, report.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// GenerateExcelReport
// ══════════════════════════════════════════════════════════════════
public record GenerateExcelReportCommand(Guid UserId, string ReportType,
    DateOnly? DateFrom, DateOnly? DateTo, Guid? StationId) : IRequest<GeneratedReportDto>;

public class GenerateExcelReportHandler(
    IGeneratedReportRepository reportRepo, IDailySalesSummaryRepository salesRepo,
    ILogger<GenerateExcelReportHandler> logger)
    : IRequestHandler<GenerateExcelReportCommand, GeneratedReportDto>
{
    public async Task<GeneratedReportDto> Handle(GenerateExcelReportCommand cmd, CancellationToken ct)
    {
        var report = new GeneratedReport
        {
            Id = Guid.NewGuid(), ReportType = cmd.ReportType, GeneratedByUserId = cmd.UserId,
            DateFrom = cmd.DateFrom, DateTo = cmd.DateTo, StationId = cmd.StationId,
            Format = "Excel", Status = "Generating"
        };
        await reportRepo.AddAsync(report, ct);

        try
        {
            var data = await salesRepo.GetAsync(cmd.StationId, null, cmd.DateFrom, cmd.DateTo, ct);
            var dir = Path.Combine(AppContext.BaseDirectory, "reports");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"{report.Id}.xlsx");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Sales Summary");

            // Header
            ws.Cell(1, 1).Value = "EPCL Sales Report";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Range("A1:D1").Merge();

            ws.Cell(2, 1).Value = $"Period: {cmd.DateFrom} — {cmd.DateTo}";
            ws.Range("A2:D2").Merge();

            // Column headers
            var headers = new[] { "Station ID", "Fuel Type", "Date", "Transactions", "Litres", "Revenue (₹)" };
            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(4, i + 1).Value = headers[i];
                ws.Cell(4, i + 1).Style.Font.Bold = true;
                ws.Cell(4, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1B3A6B");
                ws.Cell(4, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var row = 5;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.StationId.ToString()[..8];
                ws.Cell(row, 2).Value = item.FuelTypeId.ToString()[..8];
                ws.Cell(row, 3).Value = item.Date.ToString("yyyy-MM-dd");
                ws.Cell(row, 4).Value = item.TotalTransactions;
                ws.Cell(row, 5).Value = (double)item.TotalLitresSold;
                ws.Cell(row, 6).Value = (double)item.TotalRevenue;
                row++;
            }

            // Totals
            ws.Cell(row + 1, 4).Value = data.Sum(d => d.TotalTransactions);
            ws.Cell(row + 1, 5).Value = (double)data.Sum(d => d.TotalLitresSold);
            ws.Cell(row + 1, 6).Value = (double)data.Sum(d => d.TotalRevenue);
            ws.Cell(row + 1, 3).Value = "TOTAL";
            ws.Cell(row + 1, 3).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            workbook.SaveAs(filePath);

            var fileInfo = new FileInfo(filePath);
            report.FilePath = filePath;
            report.FileSize = fileInfo.Length;
            report.Status = "Ready";
            report.GeneratedAt = DateTimeOffset.UtcNow;
            report.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
            await reportRepo.UpdateAsync(report, ct);
            logger.LogInformation("Excel report generated: {Id} ({Size} bytes)", report.Id, report.FileSize);
        }
        catch (Exception ex)
        {
            report.Status = "Failed";
            await reportRepo.UpdateAsync(report, ct);
            logger.LogError(ex, "Failed to generate Excel report {Id}", report.Id);
            throw;
        }

        return new GeneratedReportDto(report.Id, report.ReportType, report.Format, report.Status,
            report.DateFrom, report.DateTo, report.StationId, report.FileSize,
            report.GeneratedAt, report.ExpiresAt, report.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// CreateScheduledReport
// ══════════════════════════════════════════════════════════════════
public record CreateScheduledReportCommand(Guid UserId, string ReportType, string CronExpression,
    Guid? StationId, string Format) : IRequest<ScheduledReportDto>;

public class CreateScheduledReportHandler(IScheduledReportRepository repo)
    : IRequestHandler<CreateScheduledReportCommand, ScheduledReportDto>
{
    public async Task<ScheduledReportDto> Handle(CreateScheduledReportCommand cmd, CancellationToken ct)
    {
        var sched = new ScheduledReport
        {
            Id = Guid.NewGuid(), ReportType = cmd.ReportType, CronExpression = cmd.CronExpression,
            CreatedByUserId = cmd.UserId, StationId = cmd.StationId, Format = cmd.Format
        };
        await repo.AddAsync(sched, ct);
        return new ScheduledReportDto(sched.Id, sched.ReportType, sched.CronExpression,
            sched.StationId, sched.Format, sched.IsActive, sched.CreatedAt);
    }
}

// ══════════════════════════════════════════════════════════════════
// DeleteScheduledReport
// ══════════════════════════════════════════════════════════════════
public record DeleteScheduledReportCommand(Guid Id) : IRequest<MessageResponseDto>;

public class DeleteScheduledReportHandler(IScheduledReportRepository repo)
    : IRequestHandler<DeleteScheduledReportCommand, MessageResponseDto>
{
    public async Task<MessageResponseDto> Handle(DeleteScheduledReportCommand cmd, CancellationToken ct)
    {
        var sched = await repo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException("ScheduledReport", cmd.Id);
        await repo.RemoveAsync(sched, ct);
        return new MessageResponseDto("Scheduled report deleted.");
    }
}
