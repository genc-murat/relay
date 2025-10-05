using System;

namespace Relay.Core.AI
{
    public class OptimizationValidationResult
    {
        public bool WasSuccessful { get; set; }
        public double OverallImprovement { get; set; }
        public ValidationOptimizationResult[] StrategyResults { get; set; } = Array.Empty<ValidationOptimizationResult>();
        public DateTime ValidationTime { get; set; }
        public RequestExecutionMetrics BeforeMetrics { get; set; } = null!;
        public RequestExecutionMetrics AfterMetrics { get; set; } = null!;
    }
}