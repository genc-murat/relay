using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Metrics for request execution.
    /// </summary>
    public sealed class RequestExecutionMetrics
    {
        public TimeSpan AverageExecutionTime { get; init; }
        public TimeSpan MedianExecutionTime { get; init; }
        public TimeSpan P95ExecutionTime { get; init; }
        public TimeSpan P99ExecutionTime { get; init; }
        public long TotalExecutions { get; init; }
        public long SuccessfulExecutions { get; init; }
        public long FailedExecutions { get; init; }
        public double SuccessRate { get; init; }
        public long MemoryAllocated { get; init; }
        public int ConcurrentExecutions { get; init; }
        public DateTime LastExecution { get; init; }
        public TimeSpan SamplePeriod { get; init; }
        
        /// <summary>
        /// CPU usage during request execution (0.0 to 1.0)
        /// </summary>
        public double CpuUsage { get; init; }
        
        /// <summary>
        /// Memory usage in bytes
        /// </summary>
        public long MemoryUsage { get; init; }
        
        /// <summary>
        /// Number of database calls made
        /// </summary>
        public int DatabaseCalls { get; init; }
        
        /// <summary>
        /// Number of external API calls made
        /// </summary>
        public int ExternalApiCalls { get; init; }
    }
}