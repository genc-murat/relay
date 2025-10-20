using System;

namespace Relay.Core.Resilience.CircuitBreaker;

/// <summary>
/// Configuration options for circuit breaker.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Failure threshold percentage (0.0 to 1.0).
    /// </summary>
    public double FailureThreshold { get; set; } = 0.5; // 50%

    /// <summary>
    /// Minimum number of requests before circuit breaker activates.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Duration to keep circuit open before attempting half-open.
    /// </summary>
    public TimeSpan OpenCircuitDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Time window for calculating failure rate.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);
}
