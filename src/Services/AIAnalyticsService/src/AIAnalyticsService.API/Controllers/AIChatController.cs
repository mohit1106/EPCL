using System.Security.Claims;
using AIAnalyticsService.Application.Commands;
using AIAnalyticsService.Application.DTOs;
using AIAnalyticsService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAnalyticsService.API.Controllers;

/// <summary>
/// AI-powered analytics chat interface using Google Gemini.
/// Converts natural language questions into SQL queries, executes them
/// against EPCL read-only views, and returns human-readable answers.
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public class AIChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public AIChatController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Send a natural language question and receive an AI-generated data answer.
    /// </summary>
    /// <remarks>
    /// The AI converts the question to a SQL query, executes it against read-only views,
    /// and formats the result. Responses may include table data and chart suggestions.
    /// 
    /// Rate limited: 20 requests per minute per user (enforced at gateway).
    /// </remarks>
    /// <param name="request">The chat message to send</param>
    /// <returns>AI response with optional table data and chart type</returns>
    /// <response code="200">AI response generated successfully</response>
    /// <response code="401">JWT token missing or expired</response>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
    {
        var userId = Guid.Parse(User.FindFirst("userId")!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;
        var stationId = User.FindFirst("stationId") is { Value: { } stationValue }
            ? Guid.Parse(stationValue) : (Guid?)null;

        var command = new SendChatMessageCommand(
            request.Message,
            request.SessionId ?? Guid.NewGuid().ToString("N")[..12],
            userId, userRole, stationId);

        return Ok(await _mediator.Send(command));
    }

    /// <summary>
    /// Get role-appropriate suggested questions for the AI chatbot.
    /// </summary>
    /// <returns>List of 4-5 suggested questions based on user's role</returns>
    /// <response code="200">Suggestions returned</response>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(SuggestedQuestionsDto), StatusCodes.Status200OK)]
    public IActionResult GetSuggestions()
    {
        var role = User.FindFirst(ClaimTypes.Role)!.Value;
        string[] suggestions = role switch
        {
            "Admin" or "SuperAdmin" =>
            [
                "Which station had the highest revenue today?",
                "How many fraud alerts were triggered this week?",
                "Which fuel type sells the most across all stations?",
                "List stations with stock below 20% capacity",
                "What is the average transaction value per station this month?"
            ],
            "Dealer" =>
            [
                "How does my revenue today compare to yesterday?",
                "Which pump sold the most fuel this week?",
                "What is my average transaction value this month?",
                "How many transactions were processed per hour today?",
                "What is the current stock percentage for each of my tanks?"
            ],
            "Customer" =>
            [
                "How much fuel have I purchased this month?",
                "What is my total spending on petrol this year?",
                "Which station do I visit most frequently?",
                "Show my loyalty points earned per transaction"
            ],
            _ => []
        };

        return Ok(new SuggestedQuestionsDto(suggestions.ToList()));
    }

    /// <summary>
    /// Get conversation history for the authenticated user.
    /// </summary>
    /// <param name="sessionId">Optional session ID to filter by</param>
    /// <returns>List of conversation messages</returns>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] string? sessionId)
    {
        var userId = Guid.Parse(User.FindFirst("userId")!.Value);
        return Ok(await _mediator.Send(new GetChatHistoryQuery(userId, sessionId)));
    }

    /// <summary>
    /// Clear conversation history for the authenticated user.
    /// </summary>
    /// <param name="sessionId">Optional session ID to clear. If omitted, clears all history.</param>
    [HttpDelete("history")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearHistory([FromQuery] string? sessionId)
    {
        var userId = Guid.Parse(User.FindFirst("userId")!.Value);
        await _mediator.Send(new ClearChatHistoryCommand(userId, sessionId));
        return NoContent();
    }
}
