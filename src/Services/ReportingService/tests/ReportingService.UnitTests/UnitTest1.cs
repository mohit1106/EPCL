using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using ReportingService.Application.Commands;
using ReportingService.Application.Queries;
using ReportingService.Application.DTOs;
using ReportingService.Domain.Entities;
using ReportingService.Domain.Exceptions;
using ReportingService.Domain.Interfaces;

namespace ReportingService.UnitTests;

#region GenerationTests

[TestFixture]
public class GenerationTests
{
    private Mock<IGeneratedReportRepository> _reportRepo = null!;
    private Mock<IDailySalesSummaryRepository> _salesRepo = null!;
    private ILogger<GenerateExcelReportHandler> _excelLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _reportRepo = new Mock<IGeneratedReportRepository>();
        _salesRepo = new Mock<IDailySalesSummaryRepository>();
        var excelMock = new Mock<ILogger<GenerateExcelReportHandler>>();
        _excelLogger = excelMock.Object;
    }

    [Test]
    public async Task GenerateExcelReport_CreatesFileAndUpdatesStatus()
    {
        var handler = new GenerateExcelReportHandler(_reportRepo.Object, _salesRepo.Object, _excelLogger);

        var data = new List<DailySalesSummary>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.UtcNow), TotalTransactions = 10, TotalLitresSold = 500, TotalRevenue = 50000 }
        };
        _salesRepo.Setup(r => r.GetAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(data);
        _reportRepo.Setup(r => r.AddAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>())).ReturnsAsync((GeneratedReport l, CancellationToken _) => l);
        _reportRepo.Setup(r => r.UpdateAsync(It.IsAny<GeneratedReport>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var cmd = new GenerateExcelReportCommand(Guid.NewGuid(), "DailySales", null, null, null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Status.Should().Be("Ready");
        result.FileSize.Should().BeGreaterThan(0);

        _reportRepo.Verify(r => r.UpdateAsync(It.Is<GeneratedReport>(gr => gr.Status == "Ready"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ScheduledReportTests

[TestFixture]
public class ScheduledReportTests
{
    private Mock<IScheduledReportRepository> _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IScheduledReportRepository>();
    }

    [Test]
    public async Task CreateScheduledReport_Valid_AddsSubscription()
    {
        var handler = new CreateScheduledReportHandler(_repo.Object);
        var cmd = new CreateScheduledReportCommand(Guid.NewGuid(), "Monthly", "0 0 1 * *", Guid.NewGuid(), "PDF");
        
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.CronExpression.Should().Be("0 0 1 * *");
        
        _repo.Verify(r => r.AddAsync(It.IsAny<ScheduledReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteScheduledReport_Found_RemovesReport()
    {
        var id = Guid.NewGuid();
        var report = new ScheduledReport { Id = id };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(report);

        var handler = new DeleteScheduledReportHandler(_repo.Object);
        await handler.Handle(new DeleteScheduledReportCommand(id), CancellationToken.None);

        _repo.Verify(r => r.RemoveAsync(report, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region KPIQueryTests

[TestFixture]
public class KPIQueryTests
{
    private Mock<IDailySalesSummaryRepository> _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IDailySalesSummaryRepository>();
    }

    [Test]
    public async Task GetAdminKpi_CalculatesAggregatesCorrectly()
    {
        var logs = new List<DailySalesSummary>
        {
            new() { StationId = Guid.NewGuid(), TotalTransactions = 10, TotalLitresSold = 100, TotalRevenue = 10000 },
            new() { StationId = Guid.NewGuid(), TotalTransactions = 5, TotalLitresSold = 50, TotalRevenue = 5000 }
        };
        _repo.Setup(r => r.GetAsync(null, null, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var handler = new GetAdminKpiHandler(_repo.Object);
        var result = await handler.Handle(new GetAdminKpiQuery(), CancellationToken.None);

        result.TotalStations.Should().Be(2);
        result.TotalTransactionsToday.Should().Be(15);
        result.TotalRevenueToday.Should().Be(15000);
        result.TotalLitresToday.Should().Be(150m);
    }

    [Test]
    public async Task GetSalesSummary_ReturnsMappedDto()
    {
        var logs = new List<DailySalesSummary>
        {
            new() { StationId = Guid.NewGuid(), TotalTransactions = 10, TotalLitresSold = 100, TotalRevenue = 10000 }
        };
        _repo.Setup(r => r.GetAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var handler = new GetSalesSummaryHandler(_repo.Object);
        var result = await handler.Handle(new GetSalesSummaryQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].TotalTransactions.Should().Be(10);
    }
}

#endregion
