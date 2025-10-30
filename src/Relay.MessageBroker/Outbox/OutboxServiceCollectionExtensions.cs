using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Extension methods for registering Outbox pattern services.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Outbox pattern with in-memory storage for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOutboxPattern(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null)
    {
        // Register options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<OutboxOptions>(options => options.Enabled = true);
        }

        // Register in-memory store by default
        services.TryAddSingleton<IOutboxStore, InMemoryOutboxStore>();

        // Register the outbox worker
        services.AddHostedService<OutboxWorker>();

        return services;
    }

    /// <summary>
    /// Adds the Outbox pattern with SQL storage using Entity Framework Core.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Action to configure the database context.</param>
    /// <param name="configure">Optional configuration action for outbox options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOutboxPatternWithSql(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<OutboxOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // Register options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<OutboxOptions>(options => options.Enabled = true);
        }

        // Register DbContext with factory
        services.AddDbContextFactory<OutboxDbContext>(configureDbContext);

        // Register SQL store
        services.TryAddSingleton<IOutboxStore, SqlOutboxStore>();

        // Register the outbox worker
        services.AddHostedService<OutboxWorker>();

        return services;
    }

    /// <summary>
    /// Decorates the message broker with the Outbox pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateMessageBrokerWithOutbox(this IServiceCollection services)
    {
        services.Decorate<IMessageBroker, OutboxMessageBrokerDecorator>();
        return services;
    }
}
