namespace Relay.Core.AI
{
    /// <summary>
    /// Configures AI-powered database query optimization.
    /// </summary>
    public sealed class SmartDatabaseOptimizationAttribute : SmartAttributeBase
    {
        /// <summary>
        /// Gets or sets whether to enable query batching.
        /// </summary>
        public bool EnableQueryBatching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable connection pooling optimization.
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of database calls before optimization suggestion.
        /// </summary>
        public int MaxDatabaseCalls { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to enable read replica usage for read operations.
        /// </summary>
        public bool PreferReadReplicas { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable query result caching.
        /// </summary>
        public bool EnableQueryCaching { get; set; } = true;
    }
}