namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Circuit breaker metrics.
/// </summary>
public sealed class CircuitBreakerMetrics
{
    /// <summary>
    /// Gets the total number of calls.
    /// </summary>
    public long TotalCalls { get; init; }

    /// <summary>
    /// Gets the number of successful calls.
    /// </summary>
    public long SuccessfulCalls { get; init; }

    /// <summary>
    /// Gets the number of failed calls.
    /// </summary>
    public long FailedCalls { get; init; }

    /// <summary>
    /// Gets the number of slow calls.
    /// </summary>
    public long SlowCalls { get; init; }

    /// <summary>
    /// Gets the failure rate (0.0 to 1.0).
    /// </summary>
    public double FailureRate => TotalCalls > 0 ? (double)FailedCalls / TotalCalls : 0;

    /// <summary>
    /// Gets the slow call rate (0.0 to 1.0).
    /// </summary>
    public double SlowCallRate => TotalCalls > 0 ? (double)SlowCalls / TotalCalls : 0;
}
