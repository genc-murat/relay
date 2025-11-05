using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    public class ValidationRules
    {
        public double MinConfidence { get; set; }
        public RiskLevel MaxRisk { get; set; }
        public string[] RequiredParameters { get; set; } = Array.Empty<string>();
        public Func<OptimizationRecommendation, Type, CancellationToken, ValueTask<StrategyValidationResult>>? CustomValidation { get; set; }
    }
}