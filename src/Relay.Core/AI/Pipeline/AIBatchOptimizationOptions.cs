using System;
using Relay.Core.AI.Optimization.Batching;

namespace Relay.Core.AI
{
    /// <summary>
    /// Configuration options for AIBatchOptimizationBehavior.
    /// </summary>
    public class AIBatchOptimizationOptions
    {
        /// <summary>
        /// Gets or sets whether batching is enabled.
        /// </summary>
        public bool EnableBatching { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum request rate (requests/second) to trigger batching.
        /// </summary>
        public double MinimumRequestRateForBatching { get; set; } = 10.0;

        /// <summary>
        /// Gets or sets the default batch size.
        /// </summary>
        public int DefaultBatchSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets the default batch window.
        /// </summary>
        public TimeSpan DefaultBatchWindow { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the default maximum wait time.
        /// </summary>
        public TimeSpan DefaultMaxWaitTime { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets the default batching strategy.
        /// </summary>
        public BatchingStrategy DefaultStrategy { get; set; } = BatchingStrategy.Dynamic;
    }
}
