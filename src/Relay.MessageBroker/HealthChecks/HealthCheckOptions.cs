namespace Relay.MessageBroker.HealthChecks;

/// <summary>
/// Configuration options for message broker health checks.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Gets or sets the interval between health checks.
    /// Minimum value is 5 seconds.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for connectivity checks.
    /// Default is 2 seconds.
    /// </summary>
    public TimeSpan ConnectivityTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets whether to include circuit breaker state in health checks.
    /// </summary>
    public bool IncludeCircuitBreakerState { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include connection pool metrics in health checks.
    /// </summary>
    public bool IncludeConnectionPoolMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the health check.
    /// </summary>
    public string Name { get; set; } = "MessageBroker";

    /// <summary>
    /// Gets or sets the tags for the health check.
    /// </summary>
    public string[] Tags { get; set; } = { "messagebroker", "ready" };

    /// <summary>
    /// Validates the options.
    /// </summary>
    public void Validate()
    {
        if (Interval < TimeSpan.FromSeconds(5))
        {
            throw new ArgumentException("Health check interval must be at least 5 seconds.", nameof(Interval));
        }

        if (ConnectivityTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Connectivity timeout must be greater than zero.", nameof(ConnectivityTimeout));
        }
    }
}
