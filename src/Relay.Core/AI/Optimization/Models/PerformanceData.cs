namespace Relay.Core.AI
{
    // ML.NET Data Classes
    internal class PerformanceData
    {
        public float ExecutionTime { get; set; }
        public float ConcurrencyLevel { get; set; }
        public float MemoryUsage { get; set; }
        public float DatabaseCalls { get; set; }
        public float ExternalApiCalls { get; set; }
        public float OptimizationGain { get; set; } // Label
    }
}
