namespace Relay.MessageBroker.Metrics;

/// <summary>
/// Configuration options for message broker metrics.
/// </summary>
public sealed class MetricsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether metrics collection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the meter name for message broker metrics.
    /// </summary>
    public string MeterName { get; set; } = "Relay.MessageBroker";

    /// <summary>
    /// Gets or sets the meter version.
    /// </summary>
    public string? MeterVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether connection pool metrics are enabled.
    /// </summary>
    public bool EnableConnectionPoolMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Prometheus export is enabled.
    /// </summary>
    public bool EnablePrometheusExport { get; set; } = false;

    /// <summary>
    /// Gets or sets the Prometheus scrape endpoint path.
    /// </summary>
    public string PrometheusEndpointPath { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the default tenant ID to use when none is specified.
    /// </summary>
    public string? DefaultTenantId { get; set; }

    /// <summary>
    /// Gets or sets the broker type identifier.
    /// </summary>
    public string? BrokerType { get; set; }
}
