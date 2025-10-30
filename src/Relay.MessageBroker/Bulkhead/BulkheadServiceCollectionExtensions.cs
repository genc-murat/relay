using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Extension methods for registering bulkhead pattern services.
/// </summary>
public static class BulkheadServiceCollectionExtensions
{
    /// <summary>
    /// Adds bulkhead pattern support to the message broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure bulkhead options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerBulkhead(
        this IServiceCollection services,
        Action<BulkheadOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<BulkheadOptions>(options => { });
        }

        // Register separate bulkheads for publish and subscribe operations
        services.TryAddSingleton<IBulkhead>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BulkheadOptions>>();
            var logger = sp.GetRequiredService<ILogger<Bulkhead>>();
            return new Bulkhead(options, logger, "publish");
        });

        // Register named bulkhead for subscribe operations
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BulkheadOptions>>();
            var logger = sp.GetRequiredService<ILogger<Bulkhead>>();
            return new Bulkhead(options, logger, "subscribe");
        });

        return services;
    }

    /// <summary>
    /// Decorates the message broker with bulkhead pattern support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateMessageBrokerWithBulkhead(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Decorate<IMessageBroker>((inner, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<BulkheadOptions>>();
            var logger = sp.GetRequiredService<ILogger<BulkheadMessageBrokerDecorator>>();

            // Create separate bulkheads for publish and subscribe
            var publishBulkhead = new Bulkhead(
                options,
                sp.GetRequiredService<ILogger<Bulkhead>>(),
                "publish");

            var subscribeBulkhead = new Bulkhead(
                options,
                sp.GetRequiredService<ILogger<Bulkhead>>(),
                "subscribe");

            return new BulkheadMessageBrokerDecorator(
                inner,
                publishBulkhead,
                subscribeBulkhead,
                options,
                logger);
        });

        return services;
    }
}
