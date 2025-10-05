using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// System load metrics for optimization decisions.
    /// </summary>
    public sealed class SystemLoadMetrics
    {
        public double CpuUtilization { get; init; }
        public double MemoryUtilization { get; init; }
        public long AvailableMemory { get; init; }
        public int ActiveRequestCount { get; init; }
        public int QueuedRequestCount { get; init; }
        public double ThroughputPerSecond { get; init; }
        public TimeSpan AverageResponseTime { get; init; }
        public double ErrorRate { get; init; }
        public DateTime Timestamp { get; init; }
        
        /// <summary>
        /// Number of active connections
        /// </summary>
        public int ActiveConnections { get; init; }
        
        /// <summary>
        /// Database connection pool utilization
        /// </summary>
        public double DatabasePoolUtilization { get; init; }
        
        /// <summary>
        /// Thread pool utilization
        /// </summary>
        public double ThreadPoolUtilization { get; init; }
    }
}