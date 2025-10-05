using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Options for configuring AI health checks.
    /// </summary>
    public class AIHealthCheckOptions
    {
        /// <summary>
        /// Minimum acceptable model accuracy score (0.0 to 1.0)
        /// </summary>
        public double MinAccuracyScore { get; set; } = 0.70;

        /// <summary>
        /// Minimum acceptable model F1 score (0.0 to 1.0)
        /// </summary>
        public double MinF1Score { get; set; } = 0.65;

        /// <summary>
        /// Minimum acceptable model confidence (0.0 to 1.0)
        /// </summary>
        public double MinConfidence { get; set; } = 0.60;

        /// <summary>
        /// Maximum acceptable average prediction time in milliseconds
        /// </summary>
        public double MaxPredictionTimeMs { get; set; } = 100.0;

        /// <summary>
        /// Maximum days since last model retraining before warning
        /// </summary>
        public int MaxDaysSinceRetraining { get; set; } = 30;

        /// <summary>
        /// Minimum system health score (0.0 to 1.0)
        /// </summary>
        public double MinSystemHealthScore { get; set; } = 0.70;

        /// <summary>
        /// Maximum acceptable error rate (0.0 to 1.0)
        /// </summary>
        public double MaxErrorRate { get; set; } = 0.05;

        /// <summary>
        /// Maximum circuit breaker failure rate before unhealthy (0.0 to 1.0)
        /// </summary>
        public double MaxCircuitBreakerFailureRate { get; set; } = 0.25;

        /// <summary>
        /// Timeout for health check operations
        /// </summary>
        public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }

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
