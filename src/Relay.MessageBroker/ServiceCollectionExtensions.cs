using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker.AzureServiceBus;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.Kafka;
using Relay.MessageBroker.Nats;
using Relay.MessageBroker.RabbitMQ;
using Relay.MessageBroker.RedisStreams;
using Relay.Core.ContractValidation;
using Relay.Core.Extensions;

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
        ServiceRegistrationHelper.ValidateServicesAndConfiguration(services, configure);

        // Use standard options pattern to avoid registration conflicts
        services.Configure(configure);

        return ServiceRegistrationHelper.AddService<IMessageBroker>(services, sp =>
        {
            var brokerOptions = sp.GetRequiredService<IOptions<MessageBrokerOptions>>();
            var contractValidator = sp.GetService<IContractValidator>();

            return brokerOptions.Value.BrokerType switch
            {
                MessageBrokerType.RabbitMQ => new RabbitMQMessageBroker(
                    brokerOptions,
                    sp.GetRequiredService<ILogger<RabbitMQMessageBroker>>(),
                    null,
                    contractValidator),
                MessageBrokerType.Kafka => new KafkaMessageBroker(
                    brokerOptions,
                    sp.GetRequiredService<ILogger<KafkaMessageBroker>>(),
                    null,
                    contractValidator),
                MessageBrokerType.AzureServiceBus => new AzureServiceBusMessageBroker(
                    brokerOptions,
                    sp.GetRequiredService<ILogger<AzureServiceBusMessageBroker>>(),
                    null,
                    contractValidator),
                MessageBrokerType.AwsSqsSns => new AwsSqsSnsMessageBroker(
                    brokerOptions,
                    sp.GetService<ILogger<AwsSqsSnsMessageBroker>>(),
                    null,
                    contractValidator),
                MessageBrokerType.Nats => new NatsMessageBroker(
                    brokerOptions,
                    sp.GetService<ILogger<NatsMessageBroker>>(),
                    null,
                    contractValidator),
                MessageBrokerType.RedisStreams => new RedisStreamsMessageBroker(
                    brokerOptions,
                    sp.GetRequiredService<ILogger<RedisStreamsMessageBroker>>(),
                    null,
                    contractValidator),
                _ => throw new NotSupportedException($"Message broker type {brokerOptions.Value.BrokerType} is not supported.")
            };
        });
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
        return services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions();
            configure?.Invoke(options.RabbitMQ);
        });
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
        return services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.Kafka;
            options.Kafka = new KafkaOptions();
            configure?.Invoke(options.Kafka);
        });
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
        return services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.AzureServiceBus;
            options.AzureServiceBus = new AzureServiceBusOptions();
            configure?.Invoke(options.AzureServiceBus);
        });
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
        return services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.AwsSqsSns;
            options.AwsSqsSns = new AwsSqsSnsOptions();
            configure?.Invoke(options.AwsSqsSns);
        });
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
        return services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.Nats;
            options.Nats = new NatsOptions();
            configure?.Invoke(options.Nats);
        });
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
        return services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RedisStreams;
            options.RedisStreams = new RedisStreamsOptions();
            configure?.Invoke(options.RedisStreams);
        });
    }

    /// <summary>
    /// Adds message broker hosted service to automatically start/stop the broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerHostedService(this IServiceCollection services)
    {
        return services.RegisterCoreServices(svc => svc.AddHostedService<MessageBrokerHostedService>());
    }
}
