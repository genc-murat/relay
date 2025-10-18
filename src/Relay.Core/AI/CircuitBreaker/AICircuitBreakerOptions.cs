using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Configuration options for AI Circuit Breaker
    /// </summary>
    public class AICircuitBreakerOptions
    {
        /// <summary>
        /// Number of consecutive failures before opening the circuit. Default is 5.
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Number of consecutive successes required to close the circuit from half-open state. Default is 3.
        /// </summary>
        public int SuccessThreshold { get; set; } = 3;

        /// <summary>
        /// Timeout for individual operations. Default is 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Duration to wait before transitioning from Open to HalfOpen. Default is 1 minute.
        /// </summary>
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Maximum number of calls allowed in HalfOpen state. Default is 3.
        /// </summary>
        public int HalfOpenMaxCalls { get; set; } = 3;

        /// <summary>
        /// Slow call duration threshold (as fraction of timeout). Default is 0.8 (80% of timeout).
        /// </summary>
        public double SlowCallThreshold { get; set; } = 0.8;

        /// <summary>
        /// Whether to enable detailed metrics collection. Default is true.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Whether to enable telemetry events. Default is true.
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Circuit breaker policy type. Default is Standard.
        /// </summary>
        public CircuitBreakerPolicy Policy { get; set; } = CircuitBreakerPolicy.Standard;

        /// <summary>
        /// Name identifier for the circuit breaker instance.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        public void Validate()
        {
            if (FailureThreshold <= 0)
                throw new ArgumentException("FailureThreshold must be greater than 0", nameof(FailureThreshold));

            if (SuccessThreshold <= 0)
                throw new ArgumentException("SuccessThreshold must be greater than 0", nameof(SuccessThreshold));

            if (Timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout must be greater than zero", nameof(Timeout));

            if (BreakDuration <= TimeSpan.Zero)
                throw new ArgumentException("BreakDuration must be greater than zero", nameof(BreakDuration));

            if (HalfOpenMaxCalls <= 0)
                throw new ArgumentException("HalfOpenMaxCalls must be greater than 0", nameof(HalfOpenMaxCalls));

            if (SlowCallThreshold <= 0 || SlowCallThreshold > 1)
                throw new ArgumentException("SlowCallThreshold must be between 0 and 1", nameof(SlowCallThreshold));
        }
    }

    /// <summary>
    /// Circuit breaker policy types
    /// </summary>
    public enum CircuitBreakerPolicy
    {
        /// <summary>
        /// Standard circuit breaker with consecutive failure counting
        /// </summary>
        Standard,

        /// <summary>
        /// Percentage-based circuit breaker using failure rates
        /// </summary>
        PercentageBased,

        /// <summary>
        /// Adaptive circuit breaker that adjusts thresholds based on load
        /// </summary>
        Adaptive
    }
}