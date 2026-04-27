using AIAnalyticsService.Domain.Entities;

namespace AIAnalyticsService.Domain.Interfaces;

public interface IConversationRepository
{
    Task<List<ConversationMessage>> GetSessionHistoryAsync(string sessionId, int limit, CancellationToken ct);
    Task<List<ConversationMessage>> GetUserHistoryAsync(Guid userId, string? sessionId, CancellationToken ct);
    Task AddMessageAsync(ConversationMessage message, CancellationToken ct);
    Task ClearHistoryAsync(Guid userId, string? sessionId, CancellationToken ct);
}

public interface IQueryLogRepository
{
    Task LogAsync(QueryLog entry, CancellationToken ct);
}
