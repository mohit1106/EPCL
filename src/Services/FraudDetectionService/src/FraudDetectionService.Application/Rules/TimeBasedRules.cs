using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FraudDetectionService.Domain.Enums;
// No ValueObjects; using FraudDetectionService.Domain.Enums;
namespace FraudDetectionService.Application.Rules
{
    public interface ITimeBasedFraudRule
    {
        string RuleId { get; }
        string RuleName { get; }
        string Description { get; }
        AlertSeverity Severity { get; }

        // Returns a list of anomalies found across all stations
        Task<List<AnomalyResult>> EvaluateAsync(IServiceProvider serviceProvider);
    }

    public class AnomalyResult
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
    }
}
