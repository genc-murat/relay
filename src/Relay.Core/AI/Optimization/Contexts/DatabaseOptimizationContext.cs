using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Context for database optimization operations.
    /// </summary>
    public sealed class DatabaseOptimizationContext
    {
        public bool EnableQueryOptimization { get; init; }
        public bool EnableConnectionPooling { get; init; }
        public bool EnableReadOnlyHint { get; init; }
        public bool EnableBatchingHint { get; init; }
        public bool EnableNoTracking { get; init; }
        public int MaxRetries { get; init; }
        public int RetryDelayMs { get; init; }
        public int QueryTimeoutSeconds { get; init; }
        public Type? RequestType { get; init; }
    }
}
