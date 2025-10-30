using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Extension methods for registering Inbox pattern services.
/// </summary>
public static class InboxServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Inbox pattern with in-memory storage for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInboxPattern(
        this IServiceCollection services,
        Action<InboxOptions>? configure = null)
    {
        // Register options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<InboxOptions>(options => options.Enabled = true);
        }

        // Register in-memory store by default
        services.TryAddSingleton<IInboxStore, InMemoryInboxStore>();

        // Register the inbox cleanup worker
        services.AddHostedService<InboxCleanupWorker>();

        return services;
    }

    /// <summary>
    /// Adds the Inbox pattern with SQL storage using Entity Framework Core.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Action to configure the database context.</param>
    /// <param name="configure">Optional configuration action for inbox options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInboxPatternWithSql(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<InboxOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        // Register options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<InboxOptions>(options => options.Enabled = true);
        }

        // Register DbContext with factory
        services.AddDbContextFactory<InboxDbContext>(configureDbContext);

        // Register SQL store
        services.TryAddSingleton<IInboxStore, SqlInboxStore>();

        // Register the inbox cleanup worker
        services.AddHostedService<InboxCleanupWorker>();

        return services;
    }

    /// <summary>
    /// Decorates the message broker with the Inbox pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection DecorateMessageBrokerWithInbox(this IServiceCollection services)
    {
        services.Decorate<IMessageBroker, InboxMessageBrokerDecorator>();
        return services;
    }
}
