using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Relay.MessageBroker.ConnectionPool;

namespace Relay.MessageBroker.RabbitMQ;

/// <summary>
/// Integration of connection pooling for RabbitMQ connections.
/// </summary>
public static class RabbitMQConnectionPoolIntegration
{
    /// <summary>
    /// Creates a connection factory for RabbitMQ that can be used with connection pooling.
    /// </summary>
    /// <param name="options">Message broker options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A function that creates RabbitMQ connections.</returns>
    public static Func<CancellationToken, ValueTask<IConnection>> CreateConnectionFactory(
        MessageBrokerOptions options,
        ILogger? logger = null)
    {
        return async (cancellationToken) =>
        {
            var rabbitOptions = options.RabbitMQ ?? new RabbitMQOptions();
            var factory = new ConnectionFactory
            {
                HostName = rabbitOptions.HostName,
                Port = rabbitOptions.Port,
                UserName = rabbitOptions.UserName,
                Password = rabbitOptions.Password,
                VirtualHost = rabbitOptions.VirtualHost,
                RequestedConnectionTimeout = rabbitOptions.ConnectionTimeout
            };

            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                factory.Uri = new Uri(options.ConnectionString);
            }

            var connection = await factory.CreateConnectionAsync(cancellationToken);
            logger?.LogDebug("Created new RabbitMQ connection");
            return connection;
        };
    }

    /// <summary>
    /// Creates a connection validator for RabbitMQ connections.
    /// </summary>
    /// <returns>A function that validates RabbitMQ connections.</returns>
    public static Func<IConnection, ValueTask<bool>> CreateConnectionValidator()
    {
        return (connection) =>
        {
            var isValid = connection != null && connection.IsOpen;
            return ValueTask.FromResult(isValid);
        };
    }

    /// <summary>
    /// Creates a connection disposer for RabbitMQ connections.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A function that disposes RabbitMQ connections.</returns>
    public static Func<IConnection, ValueTask> CreateConnectionDisposer(ILogger? logger = null)
    {
        return async (connection) =>
        {
            if (connection != null && connection.IsOpen)
            {
                await connection.CloseAsync();
                logger?.LogDebug("Closed RabbitMQ connection");
            }
            connection?.Dispose();
        };
    }
}
