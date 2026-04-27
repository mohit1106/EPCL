namespace AIAnalyticsService.Domain.Entities;

/// <summary>
/// A single message in a conversation between a user and the AI assistant.
/// Stored in the ConversationHistory table.
/// </summary>
public class ConversationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// "user" for human messages, "assistant" for AI responses.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The SQL query that was generated and executed for this response (for audit purposes).
    /// Only populated for assistant messages.
    /// </summary>
    public string? GeneratedSql { get; set; }

    public int? RowsReturned { get; set; }
    public int? ExecutionMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
