using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Extensions;
using System;

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
        return services.RegisterCoreServices(svc =>
        {
            // Register the default metrics provider
            ServiceRegistrationHelper.TryAddSingleton<IMetricsProvider, DefaultMetricsProvider>(svc);

        // Register the telemetry provider with optional configuration
        ServiceRegistrationHelper.TryAddSingleton<ITelemetryProvider>(svc, provider =>
        {
            var telemetryProvider = new DefaultTelemetryProvider();
            configureTelemetry?.Invoke(telemetryProvider);
            return telemetryProvider;
        });

        // Decorate existing dispatchers with telemetry (only if they are registered)
        ServiceRegistrationHelper.TryDecorateService<IRequestDispatcher, TelemetryRequestDispatcher>(svc);
        ServiceRegistrationHelper.TryDecorateService<IStreamDispatcher, TelemetryStreamDispatcher>(svc);
        ServiceRegistrationHelper.TryDecorateService<INotificationDispatcher, TelemetryNotificationDispatcher>(svc);

        // Decorate the main Relay interface with telemetry
        ServiceRegistrationHelper.TryDecorateService<IRelay, TelemetryRelay>(svc);
        });
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
        return services.RegisterCoreServices(svc =>
        {
        ServiceRegistrationHelper.TryAddSingleton<ITelemetryProvider, TTelemetryProvider>(svc);

        // Decorate existing dispatchers with telemetry (only if they are registered)
        ServiceRegistrationHelper.TryDecorateService<IRequestDispatcher, TelemetryRequestDispatcher>(svc);
        ServiceRegistrationHelper.TryDecorateService<IStreamDispatcher, TelemetryStreamDispatcher>(svc);
        ServiceRegistrationHelper.TryDecorateService<INotificationDispatcher, TelemetryNotificationDispatcher>(svc);

        // Decorate the main Relay interface with telemetry
        ServiceRegistrationHelper.TryDecorateService<IRelay, TelemetryRelay>(svc);
        });
    }

    /// <summary>
    /// Adds a custom telemetry provider with factory
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="factory">Factory function to create the telemetry provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayTelemetry(this IServiceCollection services, Func<IServiceProvider, ITelemetryProvider> factory)
    {
        return services.RegisterCoreServices(svc =>
        {
        ServiceRegistrationHelper.TryAddSingleton<IMetricsProvider, DefaultMetricsProvider>(svc);
        ServiceRegistrationHelper.TryAddSingleton(svc, factory);

        // Decorate existing dispatchers with telemetry (only if they are registered)
        ServiceRegistrationHelper.TryDecorateService<IRequestDispatcher, TelemetryRequestDispatcher>(svc);
        ServiceRegistrationHelper.TryDecorateService<IStreamDispatcher, TelemetryStreamDispatcher>(svc);
        ServiceRegistrationHelper.TryDecorateService<INotificationDispatcher, TelemetryNotificationDispatcher>(svc);

        // Decorate the main Relay interface with telemetry
        ServiceRegistrationHelper.TryDecorateService<IRelay, TelemetryRelay>(svc);
        });
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
        return ServiceRegistrationHelper.TryAddSingleton<IMetricsProvider, TMetricsProvider>(services);
    }

    /// <summary>
    /// Adds a custom metrics provider with factory
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="factory">Factory function to create the metrics provider</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRelayMetrics(this IServiceCollection services, Func<IServiceProvider, IMetricsProvider> factory)
    {
        return ServiceRegistrationHelper.TryAddSingleton(services, factory);
    }
}