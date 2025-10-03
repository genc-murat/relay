using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker.AzureServiceBus;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.Kafka;
using Relay.MessageBroker.Nats;
using Relay.MessageBroker.RabbitMQ;
using Relay.MessageBroker.RedisStreams;

namespace Relay.MessageBroker;

/// <summary>
/// Extension methods for configuring message broker services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for message broker options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBroker(
        this IServiceCollection services,
        Action<MessageBrokerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // Register the appropriate message broker based on configuration
        services.AddSingleton<IMessageBroker>(sp =>
        {
            var options = Microsoft.Extensions.Options.Options.Create(new MessageBrokerOptions());
            configure(options.Value);

            return options.Value.BrokerType switch
            {
                MessageBrokerType.RabbitMQ => new RabbitMQMessageBroker(
                    options,
                    sp.GetRequiredService<ILogger<RabbitMQMessageBroker>>()),
                MessageBrokerType.Kafka => new KafkaMessageBroker(
                    options,
                    sp.GetRequiredService<ILogger<KafkaMessageBroker>>()),
                MessageBrokerType.AzureServiceBus => new AzureServiceBusMessageBroker(
                    options.Value,
                    sp.GetService<ILogger<AzureServiceBusMessageBroker>>()),
                MessageBrokerType.AwsSqsSns => new AwsSqsSnsMessageBroker(
                    options.Value,
                    sp.GetService<ILogger<AwsSqsSnsMessageBroker>>()),
                MessageBrokerType.Nats => new NatsMessageBroker(
                    options.Value,
                    sp.GetService<ILogger<NatsMessageBroker>>()),
                MessageBrokerType.RedisStreams => new RedisStreamsMessageBroker(
                    options.Value,
                    sp.GetService<ILogger<RedisStreamsMessageBroker>>()),
                _ => throw new NotSupportedException($"Message broker type {options.Value.BrokerType} is not supported.")
            };
        });

        return services;
    }

    /// <summary>
    /// Adds RabbitMQ message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for RabbitMQ options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        Action<RabbitMQOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions();
            configure?.Invoke(options.RabbitMQ);
        });

        return services;
    }

    /// <summary>
    /// Adds Kafka message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Kafka options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafka(
        this IServiceCollection services,
        Action<KafkaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.Kafka;
            options.Kafka = new KafkaOptions();
            configure?.Invoke(options.Kafka);
        });

        return services;
    }

    /// <summary>
    /// Adds Azure Service Bus message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Azure Service Bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureServiceBus(
        this IServiceCollection services,
        Action<AzureServiceBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.AzureServiceBus;
            options.AzureServiceBus = new AzureServiceBusOptions();
            configure?.Invoke(options.AzureServiceBus);
        });

        return services;
    }

    /// <summary>
    /// Adds AWS SQS/SNS message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for AWS SQS/SNS options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAwsSqsSns(
        this IServiceCollection services,
        Action<AwsSqsSnsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.AwsSqsSns;
            options.AwsSqsSns = new AwsSqsSnsOptions();
            configure?.Invoke(options.AwsSqsSns);
        });

        return services;
    }

    /// <summary>
    /// Adds NATS message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for NATS options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNats(
        this IServiceCollection services,
        Action<NatsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.Nats;
            options.Nats = new NatsOptions();
            configure?.Invoke(options.Nats);
        });

        return services;
    }

    /// <summary>
    /// Adds Redis Streams message broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Redis Streams options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisStreams(
        this IServiceCollection services,
        Action<RedisStreamsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RedisStreams;
            options.RedisStreams = new RedisStreamsOptions();
            configure?.Invoke(options.RedisStreams);
        });

        return services;
    }

    /// <summary>
    /// Adds message broker hosted service to automatically start/stop the broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerHostedService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<MessageBrokerHostedService>();

        return services;
    }
}
