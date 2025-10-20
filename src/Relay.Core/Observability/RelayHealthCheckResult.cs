using System;

namespace Relay.Core.Observability
{
    /// <summary>
    /// Health check result for Relay operations.
    /// </summary>
    public class RelayHealthCheckResult
    {
        public bool IsHealthy { get; }
        public string Description { get; }
        public Exception? Exception { get; }

        private RelayHealthCheckResult(bool isHealthy, string description, Exception? exception = null)
        {
            IsHealthy = isHealthy;
            Description = description;
            Exception = exception;
        }

        public static RelayHealthCheckResult Healthy(string description) => new(true, description);
        public static RelayHealthCheckResult Unhealthy(string description, Exception? exception = null) => new(false, description, exception);
    }
}