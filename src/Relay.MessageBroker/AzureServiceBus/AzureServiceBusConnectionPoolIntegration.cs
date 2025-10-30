using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker.ConnectionPool;

namespace Relay.MessageBroker.AzureServiceBus;

/// <summary>
/// Integration of connection pooling for Azure Service Bus clients.
/// </summary>
public static class AzureServiceBusConnectionPoolIntegration
{
    /// <summary>
    /// Creates a client factory for Azure Service Bus that can be used with connection pooling.
    /// </summary>
    /// <param name="options">Message broker options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A function that creates Azure Service Bus clients.</returns>
    public static Func<CancellationToken, ValueTask<ServiceBusClient>> CreateClientFactory(
        MessageBrokerOptions options,
        ILogger? logger = null)
    {
        return (cancellationToken) =>
        {
            if (options.AzureServiceBus == null)
            {
                throw new InvalidOperationException("Azure Service Bus options are required.");
            }

            if (string.IsNullOrWhiteSpace(options.AzureServiceBus.ConnectionString))
            {
                throw new InvalidOperationException("Azure Service Bus connection string is required.");
            }

            var client = new ServiceBusClient(options.AzureServiceBus.ConnectionString);
            logger?.LogDebug("Created new Azure Service Bus client");
            return ValueTask.FromResult(client);
        };
    }

    /// <summary>
    /// Creates a client validator for Azure Service Bus clients.
    /// </summary>
    /// <returns>A function that validates Azure Service Bus clients.</returns>
    public static Func<ServiceBusClient, ValueTask<bool>> CreateClientValidator()
    {
        return (client) =>
        {
            // Azure Service Bus clients don't have a simple IsConnected property
            // We consider them valid if they're not null and not disposed
            var isValid = client != null && !client.IsClosed;
            return ValueTask.FromResult(isValid);
        };
    }

    /// <summary>
    /// Creates a client disposer for Azure Service Bus clients.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A function that disposes Azure Service Bus clients.</returns>
    public static Func<ServiceBusClient, ValueTask> CreateClientDisposer(ILogger? logger = null)
    {
        return async (client) =>
        {
            if (client != null && !client.IsClosed)
            {
                await client.DisposeAsync();
                logger?.LogDebug("Disposed Azure Service Bus client");
            }
        };
    }
}
