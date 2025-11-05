using System;

namespace Relay.Core.AI
{
    public class ValidationOptimizationResult
    {
        public OptimizationStrategy Strategy { get; set; }
        public bool WasSuccessful { get; set; }
        public TimeSpan ActualImprovement { get; set; }
        public double PerformanceGain { get; set; }
        public DateTime ValidationTime { get; set; }
    }
}