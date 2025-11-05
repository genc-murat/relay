namespace Relay.Core.AI.Optimization.Models
{
    internal class OptimizationStrategyData
    {
        public float ExecutionTime { get; set; }
        public float RepeatRate { get; set; }
        public float ConcurrencyLevel { get; set; }
        public float MemoryPressure { get; set; }
        public float ErrorRate { get; set; }
        public bool ShouldOptimize { get; set; } // Label
    }
}
