namespace ReportingService.Domain.Entities;

public class DailySalesSummary
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public Guid FuelTypeId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalLitresSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MonthlyStationReport
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalLitresSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PetrolLitres { get; set; }
    public decimal DieselLitres { get; set; }
    public decimal CngLitres { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class GeneratedReport
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public Guid GeneratedByUserId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? StationId { get; set; }
    public string Format { get; set; } = "PDF";
    public string FilePath { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? GeneratedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ScheduledReport
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public Guid? StationId { get; set; }
    public string Format { get; set; } = "PDF";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
