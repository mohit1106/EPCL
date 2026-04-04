namespace ReportingService.Application.DTOs;

public record DailySalesSummaryDto(Guid Id, Guid StationId, Guid FuelTypeId, DateOnly Date,
    int TotalTransactions, decimal TotalLitresSold, decimal TotalRevenue);

public record MonthlyStationReportDto(Guid Id, Guid StationId, int Year, int Month,
    int TotalTransactions, decimal TotalLitresSold, decimal TotalRevenue,
    decimal PetrolLitres, decimal DieselLitres, decimal CngLitres);

public record GeneratedReportDto(Guid Id, string ReportType, string Format, string Status,
    DateOnly? DateFrom, DateOnly? DateTo, Guid? StationId,
    long? FileSize, DateTimeOffset? GeneratedAt, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);

public record ScheduledReportDto(Guid Id, string ReportType, string CronExpression,
    Guid? StationId, string Format, bool IsActive, DateTimeOffset CreatedAt);

public record AdminKpiDto(int TotalStations, int TotalTransactionsToday, decimal TotalRevenueToday,
    decimal TotalLitresToday, int FraudAlertsToday, int ActiveDealers);

public record DealerKpiDto(Guid StationId, int TransactionsToday, decimal RevenueToday,
    decimal LitresToday, int TransactionsThisMonth, decimal RevenueThisMonth);

public record ExportReportRequest(string ReportType, DateOnly? DateFrom, DateOnly? DateTo, Guid? StationId);

public record CreateScheduledReportRequest(string ReportType, string CronExpression, Guid? StationId, string Format);

public record MessageResponseDto(string Message);
