using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.Deduplication;

/// <summary>
/// Extension methods for registering deduplication services.
/// </summary>
public static class DeduplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds message deduplication services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageDeduplication(
        this IServiceCollection services,
        Action<DeduplicationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register deduplication options
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Register deduplication cache as singleton
        services.TryAddSingleton<IDeduplicationCache, DeduplicationCache>();

        return services;
    }

    /// <summary>
    /// Decorates the IMessageBroker with deduplication capabilities.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateWithDeduplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Decorate<IMessageBroker, DeduplicationMessageBrokerDecorator>();

        return services;
    }
}
