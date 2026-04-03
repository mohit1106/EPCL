using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;

namespace FraudDetectionService.Domain.Interfaces;

public interface IFraudAlertRepository
{
    Task<FraudAlert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FraudAlert> AddAsync(FraudAlert alert, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<FraudAlert> alerts, CancellationToken ct = default);
    Task UpdateAsync(FraudAlert alert, CancellationToken ct = default);
    Task<(IReadOnlyList<FraudAlert> Items, int Total)> GetPagedAsync(
        int page, int pageSize, AlertStatus? status = null, AlertSeverity? severity = null,
        Guid? stationId = null, DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null,
        CancellationToken ct = default);
    Task<int> GetCountByStatusAsync(AlertStatus status, Guid? stationId = null,
        DateTimeOffset? dateFrom = null, DateTimeOffset? dateTo = null, CancellationToken ct = default);
}

public interface IFraudRuleEvaluationRepository
{
    Task AddRangeAsync(IEnumerable<FraudRuleEvaluation> evaluations, CancellationToken ct = default);
    // Context queries for rules
    Task<int> GetRecentTransactionCountAsync(Guid pumpId, TimeSpan window, CancellationToken ct = default);
    Task<bool> HasDuplicateTransactionAsync(string vehicleNumber, Guid pumpId, decimal quantity, TimeSpan window, CancellationToken ct = default);
    Task<int> GetDailyVoidCountAsync(Guid dealerUserId, DateTimeOffset today, CancellationToken ct = default);
    Task<int> GetDailyTransactionCountAsync(Guid dealerUserId, DateTimeOffset today, CancellationToken ct = default);
    Task<decimal> GetDailyStationVolumeAsync(Guid stationId, DateTimeOffset today, CancellationToken ct = default);
    Task<decimal> GetAverageVolumeForDayOfWeekAsync(Guid stationId, DayOfWeek dayOfWeek, int weeksBack, CancellationToken ct = default);
    Task<bool> AreLastNTransactionsRoundNumbersAsync(Guid pumpId, int count, CancellationToken ct = default);
}

public interface IProcessedEventRepository
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string eventType, CancellationToken ct = default);
}
