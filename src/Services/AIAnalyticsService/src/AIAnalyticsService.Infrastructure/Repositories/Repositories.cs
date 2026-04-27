using AIAnalyticsService.Domain.Entities;
using AIAnalyticsService.Domain.Interfaces;
using AIAnalyticsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIAnalyticsService.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AnalyticsDbContext _db;

    public ConversationRepository(AnalyticsDbContext db) => _db = db;

    public async Task<List<ConversationMessage>> GetSessionHistoryAsync(
        string sessionId, int limit, CancellationToken ct)
    {
        return await _db.ConversationHistory
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt) // chronological for Gemini context
            .ToListAsync(ct);
    }

    public async Task<List<ConversationMessage>> GetUserHistoryAsync(
        Guid userId, string? sessionId, CancellationToken ct)
    {
        var query = _db.ConversationHistory.Where(m => m.UserId == userId);

        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(m => m.SessionId == sessionId);

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(100)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddMessageAsync(ConversationMessage message, CancellationToken ct)
    {
        _db.ConversationHistory.Add(message);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearHistoryAsync(Guid userId, string? sessionId, CancellationToken ct)
    {
        var query = _db.ConversationHistory.Where(m => m.UserId == userId);

        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(m => m.SessionId == sessionId);

        _db.ConversationHistory.RemoveRange(await query.ToListAsync(ct));
        await _db.SaveChangesAsync(ct);
    }
}

public class QueryLogRepository : IQueryLogRepository
{
    private readonly AnalyticsDbContext _db;

    public QueryLogRepository(AnalyticsDbContext db) => _db = db;

    public async Task LogAsync(QueryLog entry, CancellationToken ct)
    {
        _db.QueryLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
