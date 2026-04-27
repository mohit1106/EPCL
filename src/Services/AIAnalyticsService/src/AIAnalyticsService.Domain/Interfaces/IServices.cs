using AIAnalyticsService.Domain.Entities;

namespace AIAnalyticsService.Domain.Interfaces;

/// <summary>
/// Communicates with Google Gemini API to generate SQL from natural language
/// and format query results into human-readable answers.
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Sends a user question to Gemini with the database schema context,
    /// and receives a SQL query in response.
    /// </summary>
    Task<GeminiResponse> GenerateSqlAsync(string userQuestion, string userRole,
        List<ConversationMessage> history, CancellationToken ct);

    /// <summary>
    /// Sends the raw query results to Gemini and receives a natural-language
    /// formatted answer, optionally with chart type suggestion.
    /// </summary>
    Task<GeminiResponse> FormatAnswerAsync(string userQuestion, string sqlResult, CancellationToken ct);
}

/// <summary>
/// Executes read-only SQL queries against the EPCL_Analytics database views.
/// </summary>
public interface IAnalyticsQueryService
{
    /// <summary>
    /// Executes a SELECT-only SQL query and returns tabular results.
    /// </summary>
    Task<QueryResult> ExecuteReadOnlySqlAsync(string sql, CancellationToken ct);

    /// <summary>
    /// Validates that a SQL string contains only SELECT statements.
    /// Returns false for INSERT, UPDATE, DELETE, DROP, CREATE, ALTER, EXEC, etc.
    /// </summary>
    bool IsSqlSafe(string sql);
}

/// <summary>
/// Response from the Gemini AI service.
/// </summary>
public class GeminiResponse
{
    public bool Success { get; set; }
    public string? Sql { get; set; }
    public string? Answer { get; set; }
    public bool SuggestChart { get; set; }
    public string? ChartType { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Tabular result from a read-only SQL query.
/// </summary>
public class QueryResult
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    public string ToJson()
    {
        if (Rows.Count == 0) return "No data found.";

        var lines = new List<string>();
        lines.Add(string.Join(" | ", Columns));
        lines.Add(new string('-', lines[0].Length));

        foreach (var row in Rows.Take(50)) // limit for Gemini context
        {
            lines.Add(string.Join(" | ", Columns.Select(c => row.GetValueOrDefault(c)?.ToString() ?? "NULL")));
        }

        if (Rows.Count > 50) lines.Add($"... and {Rows.Count - 50} more rows");
        return string.Join("\n", lines);
    }
}
