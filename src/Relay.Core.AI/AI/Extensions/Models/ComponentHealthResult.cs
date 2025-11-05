using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{

    /// <summary>
    /// Health check result for an individual component.
    /// </summary>
    public class ComponentHealthResult
    {
        /// <summary>
        /// Name of the component
        /// </summary>
        public string ComponentName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the component is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Health status description
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description or reason
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Health score (0.0 to 1.0)
        /// </summary>
        public double HealthScore { get; set; }

        /// <summary>
        /// Warning messages
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Error messages
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Duration of the component health check
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Additional data specific to this component
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// Exception if component check failed
        /// </summary>
        public Exception? Exception { get; set; }
    }
}
