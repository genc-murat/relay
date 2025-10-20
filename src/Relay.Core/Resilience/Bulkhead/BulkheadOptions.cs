using System;
using System.Collections.Concurrent;

namespace Relay.Core.Resilience.Bulkhead
{
    /// <summary>
    /// Configuration options for bulkhead isolation.
    /// </summary>
    public class BulkheadOptions
    {
        private readonly ConcurrentDictionary<string, int> _requestTypeLimits = new();

        /// <summary>
        /// Default maximum concurrency for all request types.
        /// </summary>
        public int DefaultMaxConcurrency { get; set; } = Environment.ProcessorCount * 2;

        /// <summary>
        /// Maximum time to wait for bulkhead access.
        /// </summary>
        public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Sets maximum concurrency for a specific request type.
        /// </summary>
        public BulkheadOptions SetMaxConcurrency<TRequest>(int maxConcurrency)
        {
            _requestTypeLimits[typeof(TRequest).Name] = maxConcurrency;
            return this;
        }

        /// <summary>
        /// Sets maximum concurrency for a specific request type by name.
        /// </summary>
        public BulkheadOptions SetMaxConcurrency(string requestTypeName, int maxConcurrency)
        {
            _requestTypeLimits[requestTypeName] = maxConcurrency;
            return this;
        }

        /// <summary>
        /// Gets the maximum concurrency for a request type.
        /// </summary>
        public int GetMaxConcurrency(string requestTypeName)
        {
            return _requestTypeLimits.TryGetValue(requestTypeName, out var limit) ? limit : DefaultMaxConcurrency;
        }

        /// <summary>
        /// Disables bulkhead for a specific request type.
        /// </summary>
        public BulkheadOptions DisableBulkhead<TRequest>()
        {
            _requestTypeLimits[typeof(TRequest).Name] = 0;
            return this;
        }
    }
}