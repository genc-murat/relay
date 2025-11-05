using Relay.Core.AI.Optimization.Batching;

namespace Relay.Core.AI
{
    /// <summary>
    /// Enables intelligent request batching for high-frequency operations.
    /// </summary>
    public sealed class SmartBatchingAttribute : SmartAttributeBase
    {
        /// <summary>
        /// Gets or sets the batching algorithm to use.
        /// </summary>
        public string Algorithm { get; set; } = "AI-Predictive";

        /// <summary>
        /// Gets or sets the minimum batch size.
        /// </summary>
        public int MinBatchSize { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum batch size.
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum wait time for batch completion.
        /// </summary>
        public int MaxWaitTimeMilliseconds { get; set; } = 100;

        /// <summary>
        /// Gets or sets the batching strategy.
        /// </summary>
        public BatchingStrategy Strategy { get; set; } = BatchingStrategy.Dynamic;
    }
}