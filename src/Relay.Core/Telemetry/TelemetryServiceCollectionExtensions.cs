using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relay.Core.Telemetry;

namespace Relay.Core.Telemetry;

/// <summary>
/// Extension methods for configuring telemetry in the service collection
/// </summary>
public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds telemetry support to Relay
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureTelemetry">Optional configuration action for telemetry provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayTelemetry(this IServiceCollection services, Action<ITelemetryProvider>? configureTelemetry = null)
    {
        // Register the default metrics provider
        services.TryAddSingleton<IMetricsProvider, DefaultMetricsProvider>();

        // Register the default telemetry provider
        services.TryAddSingleton<ITelemetryProvider, DefaultTelemetryProvider>();

        // Decorate existing dispatchers with telemetry
        services.Decorate<IRequestDispatcher, TelemetryRequestDispatcher>();
        services.Decorate<IStreamDispatcher, TelemetryStreamDispatcher>();
        services.Decorate<INotificationDispatcher, TelemetryNotificationDispatcher>();

        // Decorate the main Relay interface with telemetry
        services.Decorate<IRelay, TelemetryRelay>();

        // Configure telemetry if provided
        if (configureTelemetry != null)
        {
            services.AddSingleton(provider =>
            {
                var telemetryProvider = provider.GetRequiredService<ITelemetryProvider>();
                configureTelemetry(telemetryProvider);
                return telemetryProvider;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds a custom telemetry provider
    /// </summary>
    /// <typeparam name="TTelemetryProvider">The type of the telemetry provider</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayTelemetry<TTelemetryProvider>(this IServiceCollection services)
        where TTelemetryProvider : class, ITelemetryProvider
    {
        services.TryAddSingleton<ITelemetryProvider, TTelemetryProvider>();

        // Decorate existing dispatchers with telemetry
        services.Decorate<IRequestDispatcher, TelemetryRequestDispatcher>();
        services.Decorate<IStreamDispatcher, TelemetryStreamDispatcher>();
        services.Decorate<INotificationDispatcher, TelemetryNotificationDispatcher>();

        // Decorate the main Relay interface with telemetry
        services.Decorate<IRelay, TelemetryRelay>();

        return services;
    }

    /// <summary>
    /// Adds a custom telemetry provider with factory
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="factory">Factory function to create the telemetry provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayTelemetry(this IServiceCollection services, Func<IServiceProvider, ITelemetryProvider> factory)
    {
        services.TryAddSingleton<IMetricsProvider, DefaultMetricsProvider>();
        services.TryAddSingleton(factory);

        // Decorate existing dispatchers with telemetry
        services.Decorate<IRequestDispatcher, TelemetryRequestDispatcher>();
        services.Decorate<IStreamDispatcher, TelemetryStreamDispatcher>();
        services.Decorate<INotificationDispatcher, TelemetryNotificationDispatcher>();

        // Decorate the main Relay interface with telemetry
        services.Decorate<IRelay, TelemetryRelay>();

        return services;
    }

    /// <summary>
    /// Adds a custom metrics provider
    /// </summary>
    /// <typeparam name="TMetricsProvider">The type of the metrics provider</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayMetrics<TMetricsProvider>(this IServiceCollection services)
        where TMetricsProvider : class, IMetricsProvider
    {
        services.TryAddSingleton<IMetricsProvider, TMetricsProvider>();
        return services;
    }

    /// <summary>
    /// Adds a custom metrics provider with factory
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="factory">Factory function to create the metrics provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayMetrics(this IServiceCollection services, Func<IServiceProvider, IMetricsProvider> factory)
    {
        services.TryAddSingleton(factory);
        return services;
    }
}