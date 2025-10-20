using System;

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
}
