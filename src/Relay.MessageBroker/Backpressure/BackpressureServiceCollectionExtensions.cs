using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.Backpressure;

/// <summary>
/// Extension methods for configuring backpressure management in the service collection.
/// </summary>
public static class BackpressureServiceCollectionExtensions
{
    /// <summary>
    /// Adds backpressure management services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for backpressure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackpressureManagement(
        this IServiceCollection services,
        Action<BackpressureOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IBackpressureController, BackpressureController>();

        return services;
    }
}
