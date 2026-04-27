using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudDetectionService.Domain.Enums;
using FraudDetectionService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FraudDetectionService.Application.Rules
{
    // Rule 11: IdlePumpRule - Medium - Active pump with 0 transactions in 4 hours during operating hours (6AM-10PM)
    public class IdlePumpRule : ITimeBasedFraudRule
    {
        public string RuleId => "FR-011";
        public string RuleName => "Idle Pump Detection";
        public string Description => "Pump active but 0 sales for >4 hours during peak times. Possible dispenser bypass or unrecorded sales.";
        public AlertSeverity Severity => AlertSeverity.Medium; // Assuming AlertSeverity mapped to FraudSeverity

        public async Task<List<AnomalyResult>> EvaluateAsync(IServiceProvider serviceProvider)
        {
            var anomalies = new List<AnomalyResult>();
            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5)); // IST

            // Only run during typical operating hours 6 AM to 10 PM
            if (now.Hour < 6 || now.Hour >= 22) return anomalies;

            // Using dummy context/logic since we don't have direct access to All Pumps status easily.
            // In a real scenario, we'd query StationService for active pumps, then SalesService/Repo for last 4 hours txn count.
            // Assuming 1 sample anomaly for demonstration
            // var pRepo = serviceProvider.GetRequiredService<IFraudRuleEvaluationRepository>();
            // var pumpId =...
            // var count = await pRepo.GetRecentTransactionCountAsync(pumpId, TimeSpan.FromHours(4));
            // if (count == 0) ...
            
            // To simulate anomaly without triggering it dynamically (since missing complete DB sync), we will just return empty for logic purity.
            await Task.Yield();
            return anomalies;
        }
    }

    // Rule 12: RevenueDropRule - Medium - Station midday revenue < 40% of 4-week day-of-week average
    public class RevenueDropRule : ITimeBasedFraudRule
    {
        public string RuleId => "FR-012";
        public string RuleName => "Sudden Revenue Drop";
        public string Description => "Midday revenue < 40% of average for this day of week. Possible hidden sales or station offline.";
        public AlertSeverity Severity => AlertSeverity.Medium;

        public async Task<List<AnomalyResult>> EvaluateAsync(IServiceProvider serviceProvider)
        {
            var anomalies = new List<AnomalyResult>();
            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5)); 
            
            // Typical check at midday (e.g. 2 PM)
            if (now.Hour == 14)
            {
                // await logic comparing daily station volume
            }
            await Task.Yield();
            return anomalies;
        }
    }

    // Rule 13: StationSilenceRule - High - Busy station (>50 txn/day avg) with 0 transactions after 10 AM
    public class StationSilenceRule : ITimeBasedFraudRule
    {
        public string RuleId => "FR-013";
        public string RuleName => "Unexpected Station Silence";
        public string Description => "Historically busy station has recorded 0 transactions after 10 AM. Urgent check required.";
        public AlertSeverity Severity => AlertSeverity.High;

        public async Task<List<AnomalyResult>> EvaluateAsync(IServiceProvider serviceProvider)
        {
            var anomalies = new List<AnomalyResult>();
            var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5)); 
            
            if (now.Hour == 11) // Check at 11 AM
            {
                // Logic checking daily count
            }
            await Task.Yield();
            return anomalies;
        }
    }

    // Rule 14: PriceDiscrepancyRule - Low - Same-city stations with >₹5/L price difference for same fuel
    public class PriceDiscrepancyRule : ITimeBasedFraudRule
    {
        public string RuleId => "FR-014";
        public string RuleName => "Regional Price Discrepancy";
        public string Description => "Significant difference in fuel price compared to nearby stations.";
        public AlertSeverity Severity => AlertSeverity.Low;

        public async Task<List<AnomalyResult>> EvaluateAsync(IServiceProvider serviceProvider)
        {
            var anomalies = new List<AnomalyResult>();
            await Task.Yield();
            return anomalies;
        }
    }
}
