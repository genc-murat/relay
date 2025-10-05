using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents access patterns for caching recommendations.
    /// </summary>
    public sealed class AccessPattern
    {
        public DateTime Timestamp { get; init; }
        public string RequestKey { get; init; } = string.Empty;
        public int AccessCount { get; init; }
        public TimeSpan TimeSinceLastAccess { get; init; }
        public bool WasCacheHit { get; init; }
        public TimeSpan ExecutionTime { get; init; }

        /// <summary>
        /// Geographic region of the request
        /// </summary>
        public string Region { get; init; } = string.Empty;

        /// <summary>
        /// User context for personalized caching
        /// </summary>
        public string UserContext { get; init; } = string.Empty;

        /// <summary>
        /// Type of request being accessed
        /// </summary>
        public Type? RequestType { get; init; }

        /// <summary>
        /// Access frequency (requests per second)
        /// </summary>
        public double AccessFrequency { get; init; }

        /// <summary>
        /// Average execution time for this pattern
        /// </summary>
        public TimeSpan AverageExecutionTime { get; init; }

        /// <summary>
        /// Data volatility indicator (0.0 = stable, 1.0 = highly volatile)
        /// </summary>
        public double DataVolatility { get; init; }

        /// <summary>
        /// Time-of-day access pattern
        /// </summary>
        public TimeOfDayPattern TimeOfDayPattern { get; init; }

        /// <summary>
        /// Number of samples used for this pattern
        /// </summary>
        public long SampleSize { get; init; }
    }
}