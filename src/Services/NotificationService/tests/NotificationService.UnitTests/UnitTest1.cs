using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Commands;
using NotificationService.Application.Queries;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.UnitTests;

#region NotificationCommandTests

[TestFixture]
public class NotificationCommandTests
{
    private Mock<INotificationLogRepository> _logRepo = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<ISmsService> _smsService = null!;

    [SetUp]
    public void SetUp()
    {
        _logRepo = new Mock<INotificationLogRepository>();
        _emailService = new Mock<IEmailService>();
        _smsService = new Mock<ISmsService>();
    }

    [Test]
    public async Task SendNotification_Email_Success_UpdatesStatusToSent()
    {
        var logger = new Mock<ILogger<SendNotificationHandler>>();
        var handler = new SendNotificationHandler(_logRepo.Object, _emailService.Object, _smsService.Object, logger.Object);

        _logRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificationLog l, CancellationToken _) => l);
        _logRepo.Setup(r => r.UpdateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _emailService.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var cmd = new SendNotificationCommand(Guid.NewGuid(), "test@test.com", null, NotificationChannel.Email, "Subject", "Body", "TestEvent");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be("Sent");
        _emailService.Verify(e => e.SendAsync("test@test.com", "Subject", "Body", It.IsAny<CancellationToken>()), Times.Once);
        _logRepo.Verify(r => r.UpdateAsync(It.Is<NotificationLog>(l => l.Status == NotificationStatus.Sent), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendNotification_Sms_Failure_UpdatesStatusToFailed()
    {
        var logger = new Mock<ILogger<SendNotificationHandler>>();
        var handler = new SendNotificationHandler(_logRepo.Object, _emailService.Object, _smsService.Object, logger.Object);

        _smsService.Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Twilio Error"));

        var cmd = new SendNotificationCommand(Guid.NewGuid(), null, "1234567890", NotificationChannel.SMS, null, "Body", "TestEvent");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Status.Should().Be("Failed");
        result.FailureReason.Should().Be("Twilio Error");
        _logRepo.Verify(r => r.UpdateAsync(It.Is<NotificationLog>(l => l.Status == NotificationStatus.Failed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task MarkNotificationRead_ValidId_SetsIsRead()
    {
        var id = Guid.NewGuid();
        var log = new NotificationLog { Id = id, IsRead = false };
        _logRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(log);

        var handler = new MarkNotificationReadHandler(_logRepo.Object);
        await handler.Handle(new MarkNotificationReadCommand(id, Guid.NewGuid()), CancellationToken.None);

        log.IsRead.Should().BeTrue();
        _logRepo.Verify(r => r.UpdateAsync(log, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region SubscriptionTests

[TestFixture]
public class SubscriptionTests
{
    private Mock<IPriceAlertSubscriptionRepository> _subRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _subRepo = new Mock<IPriceAlertSubscriptionRepository>();
    }

    [Test]
    public async Task SubscribePriceAlert_Valid_AddsSubscription()
    {
        var handler = new SubscribePriceAlertHandler(_subRepo.Object);
        var cmd = new SubscribePriceAlertCommand(Guid.NewGuid(), Guid.NewGuid(), "PriceDrop", 100m, "Email");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AlertType.Should().Be("PriceDrop");
        _subRepo.Verify(r => r.AddAsync(It.IsAny<PriceAlertSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UnsubscribePriceAlert_Found_RemovesSubscription()
    {
        var id = Guid.NewGuid();
        var sub = new PriceAlertSubscription { Id = id };
        _subRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(sub);

        var handler = new UnsubscribePriceAlertHandler(_subRepo.Object);
        await handler.Handle(new UnsubscribePriceAlertCommand(id, Guid.NewGuid()), CancellationToken.None);

        _subRepo.Verify(r => r.RemoveAsync(sub, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region NotificationQueryTests

[TestFixture]
public class NotificationQueryTests
{
    private Mock<INotificationLogRepository> _logRepo = null!;

    [SetUp]
    public void SetUp()
    {
        _logRepo = new Mock<INotificationLogRepository>();
    }

    [Test]
    public async Task GetInAppNotifications_ReturnsPagedList()
    {
        var userId = Guid.NewGuid();
        var logs = new List<NotificationLog> { new() { Id = Guid.NewGuid(), RecipientUserId = userId, Channel = NotificationChannel.InApp, Status = NotificationStatus.Sent } };
        
        _logRepo.Setup(r => r.GetByUserAsync(userId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((logs, 1));

        var handler = new GetInAppNotificationsHandler(_logRepo.Object);
        var result = await handler.Handle(new GetInAppNotificationsQuery(userId, null, 1, 10), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }
}

#endregion
