using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.Batch;

/// <summary>
/// Extension methods for registering batch processing services.
/// </summary>
public static class BatchServiceCollectionExtensions
{
    /// <summary>
    /// Adds batch processing capabilities to the message broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for batch options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerBatching(
        this IServiceCollection services,
        Action<BatchOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register batch options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<BatchOptions>(options =>
            {
                options.Enabled = true;
            });
        }

        // Decorate IMessageBroker with BatchMessageBrokerDecorator
        services.Decorate<IMessageBroker, BatchMessageBrokerDecorator>();

        return services;
    }

    /// <summary>
    /// Adds batch processing capabilities to the message broker with explicit options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The batch options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerBatching(
        this IServiceCollection services,
        BatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        services.Configure<BatchOptions>(opt =>
        {
            opt.Enabled = options.Enabled;
            opt.MaxBatchSize = options.MaxBatchSize;
            opt.FlushInterval = options.FlushInterval;
            opt.EnableCompression = options.EnableCompression;
            opt.PartialRetry = options.PartialRetry;
        });

        services.Decorate<IMessageBroker, BatchMessageBrokerDecorator>();

        return services;
    }
}
