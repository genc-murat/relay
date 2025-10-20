namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Configuration options for circuit breaker.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets whether circuit breaker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure threshold before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the success threshold to close the circuit from half-open state.
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;

    /// <summary>
    /// Gets or sets the timeout duration before attempting to close the circuit.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the sampling duration for failure rate calculation.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the minimum throughput before evaluating failure rate.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets the failure rate threshold (0.0 to 1.0).
    /// </summary>
    public double FailureRateThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the duration to wait in half-open state before allowing another request.
    /// </summary>
    public TimeSpan HalfOpenDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether to track slow calls as failures.
    /// </summary>
    public bool TrackSlowCalls { get; set; } = true;

    /// <summary>
    /// Gets or sets the slow call duration threshold.
    /// </summary>
    public TimeSpan SlowCallDurationThreshold { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the slow call rate threshold (0.0 to 1.0).
    /// </summary>
    public double SlowCallRateThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the action to execute when circuit opens.
    /// </summary>
    public Action<CircuitBreakerStateChangedEventArgs>? OnStateChanged { get; set; }

    /// <summary>
    /// Gets or sets the action to execute when circuit rejects a request.
    /// </summary>
    public Action<CircuitBreakerRejectedEventArgs>? OnRejected { get; set; }

    /// <summary>
    /// Gets or sets the exception types that should be ignored and not counted as failures.
    /// </summary>
    public Type[]? IgnoredExceptionTypes { get; set; }

    /// <summary>
    /// Gets or sets a predicate to determine if an exception should be counted as a failure.
    /// If set, this takes precedence over IgnoredExceptionTypes.
    /// </summary>
    public Func<Exception, bool>? ExceptionPredicate { get; set; }
}
