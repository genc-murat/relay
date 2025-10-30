using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Extension methods for configuring poison message handling services.
/// </summary>
public static class PoisonMessageServiceCollectionExtensions
{
    /// <summary>
    /// Adds poison message handling services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPoisonMessageHandling(
        this IServiceCollection services,
        Action<PoisonMessageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Register store (default to in-memory)
        services.TryAddSingleton<IPoisonMessageStore, InMemoryPoisonMessageStore>();

        // Register handler
        services.TryAddSingleton<IPoisonMessageHandler, PoisonMessageHandler>();

        // Register cleanup worker
        services.AddHostedService<PoisonMessageCleanupWorker>();

        return services;
    }

    /// <summary>
    /// Adds poison message handling services with a custom store implementation.
    /// </summary>
    /// <typeparam name="TStore">The type of the poison message store.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPoisonMessageHandling<TStore>(
        this IServiceCollection services,
        Action<PoisonMessageOptions>? configure = null)
        where TStore : class, IPoisonMessageStore
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Register custom store
        services.TryAddSingleton<IPoisonMessageStore, TStore>();

        // Register handler
        services.TryAddSingleton<IPoisonMessageHandler, PoisonMessageHandler>();

        // Register cleanup worker
        services.AddHostedService<PoisonMessageCleanupWorker>();

        return services;
    }
}
