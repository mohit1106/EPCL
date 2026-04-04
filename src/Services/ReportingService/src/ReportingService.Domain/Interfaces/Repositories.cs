using ReportingService.Domain.Entities;

namespace ReportingService.Domain.Interfaces;

public interface IDailySalesSummaryRepository
{
    Task UpsertAsync(Guid stationId, Guid fuelTypeId, DateOnly date, decimal litres, decimal revenue, CancellationToken ct = default);
    Task<IReadOnlyList<DailySalesSummary>> GetAsync(Guid? stationId, Guid? fuelTypeId,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct = default);
}

public interface IMonthlyStationReportRepository
{
    Task<IReadOnlyList<MonthlyStationReport>> GetAsync(Guid? stationId, int? year, int? month, CancellationToken ct = default);
}

public interface IGeneratedReportRepository
{
    Task<GeneratedReport> AddAsync(GeneratedReport report, CancellationToken ct = default);
    Task UpdateAsync(GeneratedReport report, CancellationToken ct = default);
    Task<GeneratedReport?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public interface IScheduledReportRepository
{
    Task<ScheduledReport> AddAsync(ScheduledReport sched, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledReport>> GetAllActiveAsync(CancellationToken ct = default);
    Task<ScheduledReport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task RemoveAsync(ScheduledReport sched, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
