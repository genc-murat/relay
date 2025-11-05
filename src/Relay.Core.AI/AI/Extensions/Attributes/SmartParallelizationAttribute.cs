namespace Relay.Core.AI
{
    /// <summary>
    /// Enables AI-powered parallel processing optimization.
    /// </summary>
    public sealed class SmartParallelizationAttribute : SmartAttributeBase
    {
        /// <summary>
        /// Gets or sets the minimum collection size for parallelization consideration.
        /// </summary>
        public int MinCollectionSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = -1; // Use system default

        /// <summary>
        /// Gets or sets the parallelization strategy.
        /// </summary>
        public ParallelizationStrategy Strategy { get; set; } = ParallelizationStrategy.Dynamic;

        /// <summary>
        /// Gets or sets whether to enable work stealing.
        /// </summary>
        public bool EnableWorkStealing { get; set; } = true;
    }
}