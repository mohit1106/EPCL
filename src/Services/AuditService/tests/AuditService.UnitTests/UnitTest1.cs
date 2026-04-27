using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using AuditService.Application.Commands;
using AuditService.Application.Queries;
using AuditService.Application.DTOs;
using AuditService.Domain.Entities;
using AuditService.Domain.Exceptions;
using AuditService.Domain.Interfaces;

namespace AuditService.UnitTests;

#region AuditLogCommandTests

[TestFixture]
public class AuditLogCommandTests
{
    private Mock<IAuditLogRepository> _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IAuditLogRepository>();
    }

    [Test]
    public async Task AppendAuditLog_NewEvent_AddsToRepository()
    {
        var logger = new Mock<ILogger<AppendAuditLogHandler>>();
        var handler = new AppendAuditLogHandler(_repo.Object, logger.Object);

        var eventId = Guid.NewGuid();
        _repo.Setup(r => r.EventAlreadyLoggedAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repo.Setup(r => r.AppendAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuditLog l, CancellationToken _) => l);

        var cmd = new AppendAuditLogCommand(eventId, "User", Guid.NewGuid(), "Create", null, "New", null, null, null, null, "IdentityService", DateTimeOffset.UtcNow);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.EventId.Should().Be(eventId);
        _repo.Verify(r => r.AppendAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AppendAuditLog_DuplicateEvent_SkipsRepository()
    {
        var logger = new Mock<ILogger<AppendAuditLogHandler>>();
        var handler = new AppendAuditLogHandler(_repo.Object, logger.Object);

        var eventId = Guid.NewGuid();
        _repo.Setup(r => r.EventAlreadyLoggedAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cmd = new AppendAuditLogCommand(eventId, "User", Guid.NewGuid(), "Create", null, "New", null, null, null, null, "IdentityService", DateTimeOffset.UtcNow);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().BeEmpty(); // Empty Guid since it's skipped
        _repo.Verify(r => r.AppendAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region AuditQueryTests

[TestFixture]
public class AuditQueryTests
{
    private Mock<IAuditLogRepository> _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IAuditLogRepository>();
    }

    [Test]
    public async Task GetAuditLogs_ReturnsPagedResult()
    {
        var logs = new List<AuditLog> { new() { Id = Guid.NewGuid(), EventId = Guid.NewGuid(), EntityType = "User", Operation = "Update", ServiceName = "IdentityService" } };
        _repo.Setup(r => r.GetPagedAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((logs, 1));

        var handler = new GetAuditLogsHandler(_repo.Object);
        var result = await handler.Handle(new GetAuditLogsQuery(Page: 1, PageSize: 10), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Test]
    public async Task GetAuditLogById_Found_ReturnsDto()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new AuditLog { Id = id });

        var handler = new GetAuditLogByIdHandler(_repo.Object);
        var result = await handler.Handle(new GetAuditLogByIdQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }

    [Test]
    public async Task GetAuditLogById_NotFound_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuditLog?)null);

        var handler = new GetAuditLogByIdHandler(_repo.Object);
        var act = () => handler.Handle(new GetAuditLogByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ExportAuditLog_ReturnsAllMatching()
    {
        var logs = new List<AuditLog> { new() { Id = Guid.NewGuid() }, new() { Id = Guid.NewGuid() } };
        _repo.Setup(r => r.ExportAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var handler = new ExportAuditLogHandler(_repo.Object);
        var result = await handler.Handle(new ExportAuditLogQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}

#endregion
