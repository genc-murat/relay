using System;

namespace Relay.Core.AI.Optimization.Batching
{
    /// <summary>
    /// Metadata for batch coordinators.
    /// </summary>
    internal sealed class BatchCoordinatorMetadata
    {
        public int BatchSize { get; init; }
        public TimeSpan BatchWindow { get; init; }
        public TimeSpan MaxWaitTime { get; init; }
        public BatchingStrategy Strategy { get; init; }
        public DateTime CreatedAt { get; init; }
        public long RequestCount { get; set; }
        public DateTime LastUsed { get; set; }
        public double AverageWaitTime { get; set; }
        public double AverageBatchSize { get; set; }
        public double BatchingRate { get; set; }
        public double AverageEfficiency { get; set; }
    }
}
