using System.Text.RegularExpressions;
using AIAnalyticsService.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIAnalyticsService.Infrastructure.Services;

/// <summary>
/// Executes read-only SQL queries against the EPCL_Analytics database.
/// Uses raw ADO.NET (not EF Core) because we execute dynamically-generated SQL
/// from the Gemini AI, not predefined queries.
/// 
/// Security layers:
/// 1. IsSqlSafe() — regex-based validation rejecting non-SELECT statements
/// 2. The EPCL_Analytics DB connection uses a read-only user (in production)
/// 3. Queries are capped at 30-second timeout
/// 4. Result sets are capped at 500 rows (enforced by Gemini prompt + runtime check)
/// </summary>
public partial class AnalyticsQueryService : IAnalyticsQueryService
{
    private readonly string _connectionString;
    private readonly ILogger<AnalyticsQueryService> _logger;
    private const int MaxRows = 500;
    private const int QueryTimeoutSeconds = 30;

    /// <summary>
    /// Unsafe SQL patterns. Any match means the SQL is rejected.
    /// </summary>
    [GeneratedRegex(
        @"\b(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|TRUNCATE|MERGE|GRANT|REVOKE|DENY|BACKUP|RESTORE|SHUTDOWN|DBCC|BULK|OPENROWSET|OPENDATASOURCE|xp_|sp_)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UnsafeSqlPattern();

    public AnalyticsQueryService(IConfiguration configuration, ILogger<AnalyticsQueryService> logger)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsConnection")
            ?? throw new InvalidOperationException("AnalyticsConnection string is required");
        _logger = logger;
    }

    public bool IsSqlSafe(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return false;

        // Must start with SELECT or WITH (for CTEs)
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check for dangerous patterns
        if (UnsafeSqlPattern().IsMatch(sql))
        {
            return false;
        }

        // Reject statements with semicolons (multiple statements)
        if (sql.Contains(';'))
        {
            return false;
        }

        // Reject comments that could hide malicious SQL
        if (sql.Contains("--") || sql.Contains("/*"))
        {
            return false;
        }

        return true;
    }

    public async Task<QueryResult> ExecuteReadOnlySqlAsync(string sql, CancellationToken ct)
    {
        var result = new QueryResult();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection)
        {
            CommandTimeout = QueryTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(ct);

        // Read column names
        for (int i = 0; i < reader.FieldCount; i++)
        {
            result.Columns.Add(reader.GetName(i));
        }

        // Read rows (capped at MaxRows)
        int rowCount = 0;
        while (await reader.ReadAsync(ct) && rowCount < MaxRows)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                // Format specific types for frontend display
                row[result.Columns[i]] = value switch
                {
                    decimal d => Math.Round(d, 2),
                    DateTime dt => dt.ToString("yyyy-MM-dd HH:mm"),
                    DateTimeOffset dto => dto.ToOffset(TimeSpan.FromHours(5.5)).ToString("yyyy-MM-dd HH:mm"),
                    _ => value
                };
            }
            result.Rows.Add(row);
            rowCount++;
        }

        _logger.LogInformation("Analytics query returned {RowCount} rows with {ColCount} columns",
            result.Rows.Count, result.Columns.Count);

        return result;
    }
}
