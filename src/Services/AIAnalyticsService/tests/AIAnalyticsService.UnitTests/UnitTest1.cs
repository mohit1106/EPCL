using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using AIAnalyticsService.Application.Commands;
using AIAnalyticsService.Application.DTOs;
using AIAnalyticsService.Domain.Entities;
using AIAnalyticsService.Domain.Interfaces;
using System.Diagnostics;
using NUnit.Framework;

namespace AIAnalyticsService.UnitTests;

#region ChatCommandTests

[TestFixture]
public class ChatCommandTests
{
    private Mock<IGeminiService> _gemini = null!;
    private Mock<IAnalyticsQueryService> _querySvc = null!;
    private Mock<IConversationRepository> _convRepo = null!;
    private Mock<IQueryLogRepository> _logRepo = null!;
    private Mock<ILogger<SendChatMessageCommandHandler>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _gemini = new Mock<IGeminiService>();
        _querySvc = new Mock<IAnalyticsQueryService>();
        _convRepo = new Mock<IConversationRepository>();
        _logRepo = new Mock<IQueryLogRepository>();
        _logger = new Mock<ILogger<SendChatMessageCommandHandler>>();
    }

    [Test]
    public async Task SendChatMessage_ValidSql_ReturnsChatResponseDto()
    {
        var handler = new SendChatMessageCommandHandler(_gemini.Object, _querySvc.Object, _convRepo.Object, _logRepo.Object, _logger.Object);

        _convRepo.Setup(r => r.GetSessionHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationMessage>());

        _gemini.Setup(g => g.GenerateSqlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ConversationMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeminiResponse { Success = true, Sql = "SELECT * FROM Sales" });

        _querySvc.Setup(q => q.IsSqlSafe(It.IsAny<string>())).Returns(true);

        var queryResult = new QueryResult 
        { 
            Columns = new List<string> { "Col1" }, 
            Rows = new List<Dictionary<string, object?>> { new() { { "Col1", "Val1" } } } 
        };
        _querySvc.Setup(q => q.ExecuteReadOnlySqlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        _gemini.Setup(g => g.FormatAnswerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeminiResponse { Answer = "The result is Val1", SuggestChart = false });

        var cmd = new SendChatMessageCommand("What are the sales?", "session123", Guid.NewGuid(), "Admin", null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Answer.Should().Be("The result is Val1");
        result.RowsReturned.Should().Be(1);
    }

    [Test]
    public async Task SendChatMessage_UnsafeSql_ReturnsSafetyMessage()
    {
        var handler = new SendChatMessageCommandHandler(_gemini.Object, _querySvc.Object, _convRepo.Object, _logRepo.Object, _logger.Object);

        _convRepo.Setup(r => r.GetSessionHistoryAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationMessage>());

        _gemini.Setup(g => g.GenerateSqlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<ConversationMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeminiResponse { Success = true, Sql = "DELETE FROM Sales" });

        _querySvc.Setup(q => q.IsSqlSafe(It.IsAny<string>())).Returns(false);

        var cmd = new SendChatMessageCommand("Delete the sales", "session123", Guid.NewGuid(), "Admin", null);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Answer.Should().Contain("only answer read-only");
        result.RowsReturned.Should().Be(0);
        _querySvc.Verify(q => q.ExecuteReadOnlySqlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion
