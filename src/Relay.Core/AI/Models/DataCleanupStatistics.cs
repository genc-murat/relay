using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Statistics for data cleanup operations.
    /// </summary>
    internal class DataCleanupStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CutoffTime { get; set; }

        public int RequestAnalyticsRemoved { get; set; }
        public int CachingAnalyticsRemoved { get; set; }
        public int PredictionResultsRemoved { get; set; }
        public int ExecutionTimesRemoved { get; set; }
        public int OptimizationResultsRemoved { get; set; }
        public int InternalDataItemsRemoved { get; set; }
        public int CachingDataItemsRemoved { get; set; }

        public long EstimatedMemoryFreed { get; set; }

        public int TotalItemsRemoved =>
            RequestAnalyticsRemoved +
            CachingAnalyticsRemoved +
            PredictionResultsRemoved +
            ExecutionTimesRemoved +
            OptimizationResultsRemoved +
            InternalDataItemsRemoved +
            CachingDataItemsRemoved;
    }
}
