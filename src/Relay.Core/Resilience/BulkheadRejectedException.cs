using System;

namespace Relay.Core.Resilience
{
    /// <summary>
    /// Exception thrown when bulkhead rejects a request due to concurrency limits.
    /// </summary>
    public class BulkheadRejectedException : Exception
    {
        public string RequestType { get; }
        public int MaxConcurrency { get; }

        public BulkheadRejectedException(string requestType, int maxConcurrency)
            : base($"Bulkhead rejected request type '{requestType}': maximum concurrency of {maxConcurrency} exceeded")
        {
            RequestType = requestType;
            MaxConcurrency = maxConcurrency;
        }
    }
}