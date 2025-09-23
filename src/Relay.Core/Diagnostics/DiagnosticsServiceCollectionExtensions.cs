using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Extension methods for registering Relay diagnostics services
/// </summary>
public static class DiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Relay diagnostics services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration for diagnostics options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayDiagnostics(
        this IServiceCollection services,
        Action<DiagnosticsOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<DiagnosticsOptions>(options => { });
        }

        // Register core diagnostic services
        services.TryAddSingleton<IRequestTracer, RequestTracer>();
        services.TryAddSingleton<IRelayDiagnostics, DefaultRelayDiagnostics>();
        services.TryAddSingleton<RelayDiagnosticsService>();

        return services;
    }

    /// <summary>
    /// Adds Relay diagnostics services with request tracing enabled
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="traceBufferSize">Maximum number of traces to keep in memory</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayDiagnosticsWithTracing(
        this IServiceCollection services,
        int traceBufferSize = 1000)
    {
        return services.AddRelayDiagnostics(options =>
        {
            options.EnableRequestTracing = true;
            options.TraceBufferSize = traceBufferSize;
        });
    }

    /// <summary>
    /// Adds Relay diagnostics services with performance metrics enabled
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="metricsRetentionPeriod">How long to retain metrics</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayDiagnosticsWithMetrics(
        this IServiceCollection services,
        TimeSpan? metricsRetentionPeriod = null)
    {
        return services.AddRelayDiagnostics(options =>
        {
            options.EnablePerformanceMetrics = true;
            options.MetricsRetentionPeriod = metricsRetentionPeriod ?? TimeSpan.FromHours(1);
        });
    }

    /// <summary>
    /// Adds Relay diagnostics services with both tracing and metrics enabled
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="traceBufferSize">Maximum number of traces to keep in memory</param>
    /// <param name="metricsRetentionPeriod">How long to retain metrics</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayDiagnosticsWithTracingAndMetrics(
        this IServiceCollection services,
        int traceBufferSize = 1000,
        TimeSpan? metricsRetentionPeriod = null)
    {
        return services.AddRelayDiagnostics(options =>
        {
            options.EnableRequestTracing = true;
            options.EnablePerformanceMetrics = true;
            options.TraceBufferSize = traceBufferSize;
            options.MetricsRetentionPeriod = metricsRetentionPeriod ?? TimeSpan.FromHours(1);
        });
    }

    /// <summary>
    /// Adds Relay diagnostics services with diagnostic endpoints enabled
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="basePath">Base path for diagnostic endpoints</param>
    /// <param name="requireAuthentication">Whether endpoints require authentication</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayDiagnosticsWithEndpoints(
        this IServiceCollection services,
        string basePath = "/relay",
        bool requireAuthentication = true)
    {
        return services.AddRelayDiagnostics(options =>
        {
            options.EnableDiagnosticEndpoints = true;
            options.DiagnosticEndpointBasePath = basePath;
            options.RequireAuthentication = requireAuthentication;
        });
    }

    /// <summary>
    /// Adds full Relay diagnostics services with all features enabled
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional additional configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFullRelayDiagnostics(
        this IServiceCollection services,
        Action<DiagnosticsOptions>? configureOptions = null)
    {
        return services.AddRelayDiagnostics(options =>
        {
            options.EnableRequestTracing = true;
            options.EnablePerformanceMetrics = true;
            options.EnableDiagnosticEndpoints = true;
            options.TraceBufferSize = 1000;
            options.MetricsRetentionPeriod = TimeSpan.FromHours(1);
            options.DiagnosticEndpointBasePath = "/relay";
            options.RequireAuthentication = true;

            configureOptions?.Invoke(options);
        });
    }
}