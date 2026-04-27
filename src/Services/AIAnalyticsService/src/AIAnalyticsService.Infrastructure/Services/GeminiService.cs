using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIAnalyticsService.Domain.Entities;
using AIAnalyticsService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIAnalyticsService.Infrastructure.Services;

/// <summary>
/// Configuration settings for the Google Gemini API.
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.1;
}

/// <summary>
/// Communicates with Google Gemini API to generate SQL from natural language
/// and format query results into human-readable answers.
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;

    /// <summary>
    /// System prompt that provides Gemini with the EPCL database schema context.
    /// This is the core of the AI's ability to generate correct SQL queries.
    /// </summary>
    private const string SqlSystemPrompt = """
        You are a data analyst for EPCL (Eleven Petroleum Corporation Limited), 
        an enterprise fuel management platform in India.
        
        You have access to these SQL Server views in the EPCL_Analytics database:
        
        vw_Transactions: Id, ReceiptNumber, StationId, PumpId, FuelTypeId, DealerUserId, 
          CustomerUserId, VehicleNumber, QuantityLitres, PricePerLitre, TotalAmount, 
          PaymentMethod, Status, Timestamp, LoyaltyPointsEarned
          
        vw_TankStockLevels: TankId, StationId, FuelTypeId, CurrentStockLitres, 
          CapacityLitres, MinThresholdLitres, Status, StockPercentage, LastReplenishedAt
          
        vw_DailySales: StationId, FuelTypeId, Date, TotalTransactions, 
          TotalLitresSold, TotalRevenue
          
        vw_FraudAlerts: Id, TransactionId, StationId, RuleTriggered, Severity, 
          Status, CreatedAt
          
        vw_Pumps: Id, StationId, FuelTypeId, PumpName, Status, LastServiced
        
        vw_LoyaltyAccounts: CustomerId, PointsBalance, LifetimePoints, Tier
        
        FuelTypeId values: use LIKE '%Petrol%' or LIKE '%Diesel%' as they are GUIDs 
        mapped to names in the EPCL_Stations.FuelTypes table.
        
        Amounts are in Indian Rupees (INR). Quantities are in Litres.
        Timestamps are stored as UTC, display to users as IST (UTC+5:30).
        
        RULES — you must follow all of these:
        1. Respond ONLY with valid JSON. No markdown, no explanation, just JSON.
        2. Format: {"sql": "SELECT ...", "suggestChart": true/false, "chartType": "bar|line|pie|null"}
        3. ONLY use SELECT statements. Never INSERT, UPDATE, DELETE, DROP, CREATE, ALTER, EXEC.
        4. Always add WITH (NOLOCK) on every table/view reference.
        5. Always add TOP 500 to prevent massive result sets.
        6. For date comparisons, use GETUTCDATE() and DATEADD().
        7. If the question cannot be answered from available data, return {"sql": null, "error": "reason"}.
        8. suggestChart = true only when results have numeric aggregations (SUM, COUNT, AVG).
        """;

    private const string AnswerSystemPrompt = """
        You are a helpful data analyst for EPCL (Eleven Petroleum Corporation Limited).
        The user asked a data question and the SQL query returned results.
        
        Your job:
        1. Summarize the results in 2-4 sentences of clear, natural language.
        2. Use Indian Rupee (₹) formatting for amounts.
        3. Use Indian number formatting (lakhs, crores) for large numbers.
        4. Mention specific numbers, stations, or dates from the data.
        5. If the data suggests a trend or insight, mention it briefly.
        
        Respond with JSON: {"answer": "your summary", "suggestChart": true/false, "chartType": "bar|line|pie|null"}
        suggestChart should be true only if the data has numeric columns suitable for charting.
        """;

    public GeminiService(HttpClient httpClient, IOptions<GeminiSettings> settings,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GeminiResponse> GenerateSqlAsync(string userQuestion, string userRole,
        List<ConversationMessage> history, CancellationToken ct)
    {
        try
        {
            var messages = new List<object>();

            // Add conversation history for context
            foreach (var msg in history.TakeLast(6))
            {
                messages.Add(new { role = msg.Role, parts = new[] { new { text = msg.Content } } });
            }

            // Add current question with role context
            messages.Add(new
            {
                role = "user",
                parts = new[] { new { text = $"User role: {userRole}\n\nQuestion: {userQuestion}" } }
            });

            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = SqlSystemPrompt } } },
                contents = messages,
                generationConfig = new
                {
                    temperature = _settings.Temperature,
                    maxOutputTokens = _settings.MaxTokens,
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API error {Status}: {Content}", response.StatusCode, content);
                return new GeminiResponse { Success = false, Error = "AI service temporarily unavailable." };
            }

            // Parse Gemini response
            var geminiResult = JsonSerializer.Deserialize<GeminiApiResponse>(content, JsonOpts);
            var jsonText = geminiResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(jsonText))
                return new GeminiResponse { Success = false, Error = "No response from AI." };

            var parsed = JsonSerializer.Deserialize<SqlGenerationResult>(jsonText, JsonOpts);

            if (parsed?.Sql == null)
                return new GeminiResponse { Success = false, Error = parsed?.Error ?? "Could not generate SQL." };

            return new GeminiResponse
            {
                Success = true,
                Sql = parsed.Sql,
                SuggestChart = parsed.SuggestChart,
                ChartType = parsed.ChartType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed for question: {Question}", userQuestion);
            return new GeminiResponse { Success = false, Error = "AI service error. Please try again." };
        }
    }

    public async Task<GeminiResponse> FormatAnswerAsync(string userQuestion, string sqlResult,
        CancellationToken ct)
    {
        try
        {
            var messages = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = $"Question: {userQuestion}\n\nQuery Results:\n{sqlResult}" } }
                }
            };

            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = AnswerSystemPrompt } } },
                contents = messages,
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 512,
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini answer format error {Status}: {Content}", response.StatusCode, content);
                return new GeminiResponse { Success = true, Answer = "Here are the results:" };
            }

            var geminiResult = JsonSerializer.Deserialize<GeminiApiResponse>(content, JsonOpts);
            var jsonText = geminiResult?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(jsonText))
                return new GeminiResponse { Success = true, Answer = "Here are the results:" };

            var parsed = JsonSerializer.Deserialize<AnswerFormatResult>(jsonText, JsonOpts);

            return new GeminiResponse
            {
                Success = true,
                Answer = parsed?.Answer ?? "Here are the results:",
                SuggestChart = parsed?.SuggestChart ?? false,
                ChartType = parsed?.ChartType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini answer formatting failed");
            return new GeminiResponse { Success = true, Answer = "Here are the results:" };
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #region Gemini API Response DTOs (internal)

    private class GeminiApiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class SqlGenerationResult
    {
        public string? Sql { get; set; }
        public bool SuggestChart { get; set; }
        public string? ChartType { get; set; }
        public string? Error { get; set; }
    }

    private class AnswerFormatResult
    {
        public string? Answer { get; set; }
        public bool SuggestChart { get; set; }
        public string? ChartType { get; set; }
    }

    #endregion
}
