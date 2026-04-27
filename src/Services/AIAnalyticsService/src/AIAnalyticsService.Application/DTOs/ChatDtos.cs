namespace AIAnalyticsService.Application.DTOs;

public record ChatRequestDto(string Message, string? SessionId);

public record ChatResponseDto(
    string Answer,
    string SessionId,
    List<Dictionary<string, object?>>? TableData,
    List<string>? ColumnNames,
    bool HasChartData,
    string? ChartType,
    int RowsReturned,
    string? GeneratedSql // only shown in dev/debug mode
);

public record SuggestedQuestionsDto(List<string> Questions);
