using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FraudDetectionService.Application.Rules;
using FraudDetectionService.Domain.Entities;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudDetectionService.Infrastructure.BackgroundServices
{
    public class TimeBasedAnomalyService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TimeBasedAnomalyService> _logger;

        public TimeBasedAnomalyService(IServiceProvider serviceProvider, ILogger<TimeBasedAnomalyService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TimeBasedAnomalyService starting.");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                await EvaluateRulesAsync(stoppingToken);
            }
        }

        private async Task EvaluateRulesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var rules = scope.ServiceProvider.GetServices<ITimeBasedFraudRule>();
            var alertRepo = scope.ServiceProvider.GetRequiredService<IFraudAlertRepository>();

            foreach (var rule in rules)
            {
                try
                {
                    var anomalies = await rule.EvaluateAsync(scope.ServiceProvider);
                    foreach (var anomaly in anomalies)
                    {
                        // Deduplication: Don't fire same rule for same entity within 4 hours
                        var recentAlertsResult = await alertRepo.GetPagedAsync(
                            page: 1, pageSize: 50, status: null, severity: null, stationId: anomaly.EntityId,
                            dateFrom: DateTimeOffset.UtcNow.AddHours(-4), dateTo: null, ct: cancellationToken);

                        if (recentAlertsResult.Items.Any(a => a.RuleTriggered == anomaly.RuleId))
                        {
                            continue;
                        }

                        var alert = new FraudAlert
                        {
                            TransactionId = Guid.Empty, // Time-based, no specific transaction
                            StationId = anomaly.EntityId,
                            RuleTriggered = anomaly.RuleId,
                            Description = anomaly.Description,
                            Severity = anomaly.Severity,
                            Status = AlertStatus.Open,
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        await alertRepo.AddAsync(alert, cancellationToken);
                        _logger.LogWarning("Time-based anomaly detected: {RuleName} at {EntityId}", anomaly.RuleName, anomaly.EntityId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating time-based rule {RuleName}", rule.RuleName);
                }
            }
        }
    }
}
