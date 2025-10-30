using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.ConnectionPool;

/// <summary>
/// Extension methods for registering connection pool services.
/// </summary>
public static class ConnectionPoolServiceCollectionExtensions
{
    /// <summary>
    /// Adds a connection pool to the service collection.
    /// </summary>
    /// <typeparam name="TConnection">The type of connection to pool.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create new connections.</param>
    /// <param name="options">Connection pool options.</param>
    /// <param name="connectionValidator">Optional validator function to check connection health.</param>
    /// <param name="connectionDisposer">Optional disposer function to clean up connections.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConnectionPool<TConnection>(
        this IServiceCollection services,
        Func<CancellationToken, ValueTask<TConnection>> connectionFactory,
        ConnectionPoolOptions? options = null,
        Func<TConnection, ValueTask<bool>>? connectionValidator = null,
        Func<TConnection, ValueTask>? connectionDisposer = null)
    {
        options ??= new ConnectionPoolOptions();

        services.AddSingleton<IConnectionPool<TConnection>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConnectionPoolManager<TConnection>>>();
            return new ConnectionPoolManager<TConnection>(
                connectionFactory,
                options,
                logger,
                connectionValidator,
                connectionDisposer);
        });

        return services;
    }

    /// <summary>
    /// Adds a connection pool to the service collection with a factory that uses the service provider.
    /// </summary>
    /// <typeparam name="TConnection">The type of connection to pool.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create new connections using the service provider.</param>
    /// <param name="options">Connection pool options.</param>
    /// <param name="connectionValidator">Optional validator function to check connection health.</param>
    /// <param name="connectionDisposer">Optional disposer function to clean up connections.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConnectionPool<TConnection>(
        this IServiceCollection services,
        Func<IServiceProvider, CancellationToken, ValueTask<TConnection>> connectionFactory,
        ConnectionPoolOptions? options = null,
        Func<TConnection, ValueTask<bool>>? connectionValidator = null,
        Func<TConnection, ValueTask>? connectionDisposer = null)
    {
        options ??= new ConnectionPoolOptions();

        services.AddSingleton<IConnectionPool<TConnection>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConnectionPoolManager<TConnection>>>();
            return new ConnectionPoolManager<TConnection>(
                ct => connectionFactory(sp, ct),
                options,
                logger,
                connectionValidator,
                connectionDisposer);
        });

        return services;
    }
}
