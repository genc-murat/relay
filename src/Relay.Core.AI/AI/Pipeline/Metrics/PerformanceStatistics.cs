using System;

namespace Relay.Core.AI.Pipeline.Metrics
{
    /// <summary>
    /// Performance statistics for a request type.
    /// </summary>
    public class PerformanceStatistics
    {
        public long TotalCount { get; set; }
        public long SuccessCount { get; set; }
        public long ErrorCount { get; set; }
        public double SuccessRate { get; set; }
        public double ErrorRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan P50 { get; set; }
        public TimeSpan P95 { get; set; }
        public TimeSpan P99 { get; set; }
    }
}
