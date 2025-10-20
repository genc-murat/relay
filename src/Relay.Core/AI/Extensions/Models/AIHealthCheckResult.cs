using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Overall AI health check result.
    /// </summary>
    public class AIHealthCheckResult
    {
        /// <summary>
        /// Whether the overall AI system is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Timestamp when the health check was performed
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Duration of the health check
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Results from individual component health checks
        /// </summary>
        public List<ComponentHealthResult> ComponentResults { get; set; } = new();

        /// <summary>
        /// Summary of health status
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Exception if health check failed
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Additional metadata about the health check
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
