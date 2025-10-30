using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker.ConnectionPool;

namespace Relay.MessageBroker.Kafka;

/// <summary>
/// Integration of connection pooling for Kafka producers.
/// </summary>
public static class KafkaConnectionPoolIntegration
{
    /// <summary>
    /// Creates a producer factory for Kafka that can be used with connection pooling.
    /// </summary>
    /// <param name="options">Message broker options.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A function that creates Kafka producers.</returns>
    public static Func<CancellationToken, ValueTask<IProducer<string, byte[]>>> CreateProducerFactory(
        MessageBrokerOptions options,
        ILogger? logger = null)
    {
        return (cancellationToken) =>
        {
            var kafkaOptions = options.Kafka ?? new KafkaOptions();
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                CompressionType = Enum.Parse<CompressionType>(kafkaOptions.CompressionType, true),
                EnableIdempotence = true,
                MaxInFlight = 5,
                Acks = Acks.All
            };

            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                config.BootstrapServers = options.ConnectionString;
            }

            var producer = new ProducerBuilder<string, byte[]>(config).Build();
            logger?.LogDebug("Created new Kafka producer for {BootstrapServers}", config.BootstrapServers);
            return ValueTask.FromResult(producer);
        };
    }

    /// <summary>
    /// Creates a producer validator for Kafka producers.
    /// </summary>
    /// <returns>A function that validates Kafka producers.</returns>
    public static Func<IProducer<string, byte[]>, ValueTask<bool>> CreateProducerValidator()
    {
        return (producer) =>
        {
            // Kafka producers don't have a simple IsConnected property
            // We consider them valid if they're not null
            var isValid = producer != null;
            return ValueTask.FromResult(isValid);
        };
    }

    /// <summary>
    /// Creates a producer disposer for Kafka producers.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <returns>A function that disposes Kafka producers.</returns>
    public static Func<IProducer<string, byte[]>, ValueTask> CreateProducerDisposer(ILogger? logger = null)
    {
        return (producer) =>
        {
            if (producer != null)
            {
                producer.Flush(TimeSpan.FromSeconds(5));
                producer.Dispose();
                logger?.LogDebug("Disposed Kafka producer");
            }
            return ValueTask.CompletedTask;
        };
    }
}
