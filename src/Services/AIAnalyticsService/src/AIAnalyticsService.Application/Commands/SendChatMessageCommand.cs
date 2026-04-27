using System.Diagnostics;
using AIAnalyticsService.Application.DTOs;
using AIAnalyticsService.Domain.Entities;
using AIAnalyticsService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AIAnalyticsService.Application.Commands;

public record SendChatMessageCommand(
    string Message,
    string SessionId,
    Guid UserId,
    string UserRole,
    Guid? StationId
) : IRequest<ChatResponseDto>;

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatResponseDto>
{
    private readonly IGeminiService _geminiService;
    private readonly IAnalyticsQueryService _queryService;
    private readonly IConversationRepository _convRepo;
    private readonly IQueryLogRepository _queryLogRepo;
    private readonly ILogger<SendChatMessageCommandHandler> _logger;

    public SendChatMessageCommandHandler(
        IGeminiService geminiService,
        IAnalyticsQueryService queryService,
        IConversationRepository convRepo,
        IQueryLogRepository queryLogRepo,
        ILogger<SendChatMessageCommandHandler> logger)
    {
        _geminiService = geminiService;
        _queryService = queryService;
        _convRepo = convRepo;
        _queryLogRepo = queryLogRepo;
        _logger = logger;
    }

    public async Task<ChatResponseDto> Handle(SendChatMessageCommand cmd, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Load conversation history for this session (last 10 messages)
        var history = await _convRepo.GetSessionHistoryAsync(cmd.SessionId, limit: 10, ct);

        // 2. Save user message
        await _convRepo.AddMessageAsync(new ConversationMessage
        {
            UserId = cmd.UserId,
            SessionId = cmd.SessionId,
            Role = "user",
            Content = cmd.Message,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);

        // 3. Ask Gemini to generate SQL
        var geminiSqlResponse = await _geminiService.GenerateSqlAsync(
            cmd.Message, cmd.UserRole, history, ct);

        if (!geminiSqlResponse.Success || string.IsNullOrEmpty(geminiSqlResponse.Sql))
        {
            var errorAnswer = geminiSqlResponse.Error
                ?? "I couldn't understand that question. Could you rephrase it?";

            await SaveAssistantResponse(cmd, errorAnswer, null, 0, stopwatch, ct);
            return new ChatResponseDto(errorAnswer, cmd.SessionId, null, null, false, null, 0, null);
        }

        // 4. Safety check — reject any non-SELECT SQL
        if (!_queryService.IsSqlSafe(geminiSqlResponse.Sql))
        {
            _logger.LogWarning("Gemini generated unsafe SQL for user {UserId}: {Sql}",
                cmd.UserId, geminiSqlResponse.Sql);

            const string safetyMsg = "I can only answer read-only questions about the data.";
            await SaveAssistantResponse(cmd, safetyMsg, geminiSqlResponse.Sql, 0, stopwatch, ct);

            await _queryLogRepo.LogAsync(new QueryLog
            {
                UserId = cmd.UserId, UserRole = cmd.UserRole,
                Question = cmd.Message, GeneratedSql = geminiSqlResponse.Sql,
                WasSqlValid = false, TotalMs = (int)stopwatch.ElapsedMilliseconds,
                WasSuccessful = false, ErrorMessage = "Unsafe SQL rejected"
            }, ct);

            return new ChatResponseDto(safetyMsg, cmd.SessionId, null, null, false, null, 0, null);
        }

        // 5. Scope query for Dealer role (force stationId filter)
        var finalSql = cmd.UserRole == "Dealer" && cmd.StationId.HasValue
            ? ScopeQueryForDealer(geminiSqlResponse.Sql, cmd.StationId.Value)
            : geminiSqlResponse.Sql;

        // 6. Execute query
        Domain.Interfaces.QueryResult queryResult;
        try
        {
            queryResult = await _queryService.ExecuteReadOnlySqlAsync(finalSql, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL execution failed for user {UserId}: {Sql}", cmd.UserId, finalSql);

            const string execError = "I encountered an error running that query. Please try rephrasing your question.";
            await SaveAssistantResponse(cmd, execError, finalSql, 0, stopwatch, ct);

            await _queryLogRepo.LogAsync(new QueryLog
            {
                UserId = cmd.UserId, UserRole = cmd.UserRole,
                Question = cmd.Message, GeneratedSql = finalSql,
                WasSqlValid = true, TotalMs = (int)stopwatch.ElapsedMilliseconds,
                WasSuccessful = false, ErrorMessage = ex.Message
            }, ct);

            return new ChatResponseDto(execError, cmd.SessionId, null, null, false, null, 0, null);
        }

        // 7. Ask Gemini to format the answer in natural language
        var answerResponse = await _geminiService.FormatAnswerAsync(
            cmd.Message, queryResult.ToJson(), ct);

        stopwatch.Stop();

        var answer = answerResponse.Answer ?? "Here are the results:";

        // 8. Save assistant message and query log
        await SaveAssistantResponse(cmd, answer, finalSql, queryResult.Rows.Count, stopwatch, ct);

        await _queryLogRepo.LogAsync(new QueryLog
        {
            UserId = cmd.UserId, UserRole = cmd.UserRole,
            Question = cmd.Message, GeneratedSql = finalSql,
            WasSqlValid = true, RowsReturned = queryResult.Rows.Count,
            TotalMs = (int)stopwatch.ElapsedMilliseconds, WasSuccessful = true
        }, ct);

        return new ChatResponseDto(
            Answer: answer,
            SessionId: cmd.SessionId,
            TableData: queryResult.Rows.Count > 0 ? queryResult.Rows : null,
            ColumnNames: queryResult.Rows.Count > 0 ? queryResult.Columns : null,
            HasChartData: answerResponse.SuggestChart && queryResult.Rows.Count > 0,
            ChartType: answerResponse.ChartType,
            RowsReturned: queryResult.Rows.Count,
            GeneratedSql: null // never expose in production
        );
    }

    private async Task SaveAssistantResponse(SendChatMessageCommand cmd, string answer,
        string? sql, int rows, Stopwatch sw, CancellationToken ct)
    {
        await _convRepo.AddMessageAsync(new ConversationMessage
        {
            UserId = cmd.UserId,
            SessionId = cmd.SessionId,
            Role = "assistant",
            Content = answer,
            GeneratedSql = sql,
            RowsReturned = rows,
            ExecutionMs = (int)sw.ElapsedMilliseconds,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }

    /// <summary>
    /// Injects a WHERE StationId = 'xxx' filter into the SQL for Dealer role scoping.
    /// If the query already references StationId, appends AND condition.
    /// If not, wraps the query in a CTE and adds the filter.
    /// </summary>
    private static string ScopeQueryForDealer(string sql, Guid stationId)
    {
        var stationFilter = $"StationId = '{stationId}'";

        // If the SQL already has WHERE, add AND
        if (sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            // Find the last WHERE and add our filter
            var whereIdx = sql.LastIndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            var insertPos = whereIdx + "WHERE".Length;
            return sql.Insert(insertPos, $" {stationFilter} AND");
        }

        // If no WHERE, wrap in CTE
        return $"WITH scoped AS ({sql}) SELECT * FROM scoped WHERE {stationFilter}";
    }
}
