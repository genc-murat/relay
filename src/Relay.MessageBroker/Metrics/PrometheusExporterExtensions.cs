using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Relay.MessageBroker.Metrics;

/// <summary>
/// Extension methods for configuring Prometheus metrics export.
/// </summary>
public static class PrometheusExporterExtensions
{
    /// <summary>
    /// Adds Prometheus exporter for message broker metrics.
    /// </summary>
    /// <param name="builder">The metrics builder.</param>
    /// <returns>The metrics builder for chaining.</returns>
    public static MeterProviderBuilder AddPrometheusExporterForMessageBroker(
        this MeterProviderBuilder builder)
    {
        return builder.AddPrometheusExporter();
    }

    /// <summary>
    /// Maps the Prometheus scrape endpoint for message broker metrics.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="pattern">The endpoint pattern (default: "/metrics").</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UsePrometheusScrapingEndpoint(
        this IApplicationBuilder app,
        string pattern = "/metrics")
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint(pattern);
        return app;
    }
}
