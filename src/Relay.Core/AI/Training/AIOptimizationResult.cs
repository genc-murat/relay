using System;

namespace Relay.Core.AI
{
    public class AIOptimizationResult
    {
        public OptimizationStrategy Strategy { get; init; }
        public TimeSpan ExecutionTime { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public double PerformanceImprovement { get; init; }
        public DateTime Timestamp { get; init; }
    }
}