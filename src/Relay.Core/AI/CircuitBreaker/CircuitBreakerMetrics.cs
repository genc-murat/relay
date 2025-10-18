using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Comprehensive circuit breaker metrics with detailed statistics.
    /// </summary>
    public sealed class CircuitBreakerMetrics
    {
        /// <summary>
        /// Total number of calls made through the circuit breaker.
        /// </summary>
        public long TotalCalls { get; init; }

        /// <summary>
        /// Number of successful calls.
        /// </summary>
        public long SuccessfulCalls { get; init; }

        /// <summary>
        /// Number of failed calls.
        /// </summary>
        public long FailedCalls { get; init; }

        /// <summary>
        /// Number of calls that exceeded the slow call threshold.
        /// </summary>
        public long SlowCalls { get; init; }

        /// <summary>
        /// Number of calls that timed out.
        /// </summary>
        public long TimeoutCalls { get; init; }

        /// <summary>
        /// Number of calls rejected due to circuit being open.
        /// </summary>
        public long RejectedCalls { get; init; }

        /// <summary>
        /// Current number of consecutive failures.
        /// </summary>
        public int ConsecutiveFailures { get; init; }

        /// <summary>
        /// Current number of consecutive successes.
        /// </summary>
        public int ConsecutiveSuccesses { get; init; }

        /// <summary>
        /// Total time spent in Open state.
        /// </summary>
        public TimeSpan TotalOpenTime { get; init; }

        /// <summary>
        /// Total time spent in HalfOpen state.
        /// </summary>
        public TimeSpan TotalHalfOpenTime { get; init; }

        /// <summary>
        /// Total time spent in Closed state.
        /// </summary>
        public TimeSpan TotalClosedTime { get; init; }

        /// <summary>
        /// Timestamp of the last state change.
        /// </summary>
        public DateTime LastStateChange { get; init; }

        /// <summary>
        /// Average response time for successful calls (in milliseconds).
        /// </summary>
        public double AverageResponseTimeMs { get; init; }

        /// <summary>
        /// 95th percentile response time (in milliseconds).
        /// </summary>
        public double Percentile95ResponseTimeMs { get; init; }

        /// <summary>
        /// Failure rate (0.0 to 1.0).
        /// </summary>
        public double FailureRate => TotalCalls > 0 ? (double)FailedCalls / TotalCalls : 0.0;

        /// <summary>
        /// Success rate (0.0 to 1.0).
        /// </summary>
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls : 0.0;

        /// <summary>
        /// Slow call rate (0.0 to 1.0).
        /// </summary>
        public double SlowCallRate => TotalCalls > 0 ? (double)SlowCalls / TotalCalls : 0.0;

        /// <summary>
        /// Rejection rate (0.0 to 1.0).
        /// </summary>
        public double RejectionRate => TotalCalls > 0 ? (double)RejectedCalls / TotalCalls : 0.0;

        /// <summary>
        /// Effective calls (total calls minus rejected calls).
        /// </summary>
        public long EffectiveCalls => TotalCalls - RejectedCalls;

        /// <summary>
        /// Circuit breaker availability (0.0 to 1.0).
        /// </summary>
        public double Availability => EffectiveCalls > 0 ? (double)SuccessfulCalls / EffectiveCalls : 1.0;

        /// <summary>
        /// Creates a snapshot of current metrics.
        /// </summary>
        public CircuitBreakerMetrics Snapshot() => this;
    }
}
