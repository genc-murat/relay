using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;

namespace Relay.MessageBroker.Metrics;

/// <summary>
/// Extension methods for registering message broker metrics.
/// </summary>
public static class MetricsServiceCollectionExtensions
{
    /// <summary>
    /// Adds message broker metrics to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerMetrics(
        this IServiceCollection services,
        Action<MetricsOptions>? configure = null)
    {
        var options = new MetricsOptions();
        configure?.Invoke(options);

        if (!options.Enabled)
        {
            return services;
        }

        // Register metrics collectors as singletons
        services.TryAddSingleton(sp => new MessageBrokerMetrics(options.MeterName, options.MeterVersion));
        
        if (options.EnableConnectionPoolMetrics)
        {
            services.TryAddSingleton(sp => new ConnectionPoolMetricsCollector(
                $"{options.MeterName}.ConnectionPool", 
                options.MeterVersion));
        }

        // Register options
        services.Configure<MetricsOptions>(opts =>
        {
            opts.Enabled = options.Enabled;
            opts.MeterName = options.MeterName;
            opts.MeterVersion = options.MeterVersion;
            opts.EnableConnectionPoolMetrics = options.EnableConnectionPoolMetrics;
            opts.EnablePrometheusExport = options.EnablePrometheusExport;
            opts.PrometheusEndpointPath = options.PrometheusEndpointPath;
            opts.DefaultTenantId = options.DefaultTenantId;
            opts.BrokerType = options.BrokerType;
        });

        return services;
    }

    /// <summary>
    /// Adds message broker metrics to OpenTelemetry metrics builder.
    /// </summary>
    /// <param name="builder">The metrics builder.</param>
    /// <param name="meterName">The meter name (default: "Relay.MessageBroker").</param>
    /// <returns>The metrics builder for chaining.</returns>
    public static MeterProviderBuilder AddMessageBrokerInstrumentation(
        this MeterProviderBuilder builder,
        string meterName = "Relay.MessageBroker")
    {
        return builder
            .AddMeter(meterName)
            .AddMeter($"{meterName}.ConnectionPool");
    }
}
