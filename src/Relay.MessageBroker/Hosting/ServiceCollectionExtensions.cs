using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.ContractValidation;
using Relay.Core.Extensions;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.AzureServiceBus;
using Relay.MessageBroker.ConnectionPool;
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
        ServiceRegistrationHelper.ValidateServicesAndConfiguration(services, configure);

        // Validate options immediately
        var options = new MessageBrokerOptions();
        configure(options);
        ValidateMessageBrokerOptions(options);

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
                    sp.GetRequiredService<ILogger<AwsSqsSnsMessageBroker>>(),
                    null,
                    contractValidator),
                MessageBrokerType.Nats => new NatsMessageBroker(
                    brokerOptions,
                    sp.GetRequiredService<ILogger<NatsMessageBroker>>(),
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

    /// <summary>
    /// Adds connection pooling for message brokers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for connection pool options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerConnectionPool(
        this IServiceCollection services,
        Action<ConnectionPoolOptions>? configure = null)
    {
        var poolOptions = new ConnectionPoolOptions();
        configure?.Invoke(poolOptions);

        if (!poolOptions.Enabled)
        {
            return services;
        }

        // Register connection pools for each broker type
        // The actual pools will be created lazily when the broker is instantiated
        services.AddSingleton(poolOptions);

        return services;
    }

    private static void ValidateMessageBrokerOptions(MessageBrokerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Validate broker type
        if (!Enum.IsDefined(typeof(MessageBrokerType), options.BrokerType))
            throw new InvalidOperationException("Unsupported broker type.");

        // Validate common options
        if (string.IsNullOrWhiteSpace(options.DefaultExchange))
            throw new ArgumentException("DefaultExchange cannot be null or empty.", nameof(options.DefaultExchange));

        if (string.IsNullOrWhiteSpace(options.DefaultRoutingKeyPattern))
            throw new ArgumentException("DefaultRoutingKeyPattern cannot be null or empty.", nameof(options.DefaultRoutingKeyPattern));

        // Validate broker-specific options
        switch (options.BrokerType)
        {
            case MessageBrokerType.RabbitMQ:
                ValidateRabbitMQOptions(options.RabbitMQ);
                break;
            case MessageBrokerType.Kafka:
                ValidateKafkaOptions(options.Kafka);
                break;
            case MessageBrokerType.AzureServiceBus:
                ValidateAzureServiceBusOptions(options.AzureServiceBus);
                break;
            case MessageBrokerType.AwsSqsSns:
                ValidateAwsSqsSnsOptions(options.AwsSqsSns);
                break;
            case MessageBrokerType.Nats:
                ValidateNatsOptions(options.Nats);
                break;
            case MessageBrokerType.RedisStreams:
                ValidateRedisStreamsOptions(options.RedisStreams);
                break;
        }

        // Validate serialization settings
        if (!Enum.IsDefined(typeof(MessageSerializerType), options.SerializerType))
            throw new InvalidOperationException("Unsupported serializer type.");

        if (!options.EnableSerialization && options.SerializerType != MessageSerializerType.Json)
            throw new InvalidOperationException("Serialization is disabled but SerializerType is set to a non-default value.");

        // Validate other options
        if (options.RetryPolicy != null)
            ValidateRetryPolicy(options.RetryPolicy);

        if (options.CircuitBreaker != null)
            ValidateCircuitBreakerOptions(options.CircuitBreaker);

        if (options.Compression != null)
            ValidateCompressionOptions(options.Compression);
    }

    private static void ValidateRabbitMQOptions(RabbitMQOptions? options)
    {
        if (options == null) return;

        if (string.IsNullOrWhiteSpace(options.HostName))
            throw new ArgumentException("HostName cannot be null or empty.", nameof(options.HostName));

        if (options.Port <= 0 || options.Port > 65535)
            throw new ArgumentOutOfRangeException(nameof(options.Port), "Port must be between 1 and 65535.");

        if (string.IsNullOrWhiteSpace(options.UserName))
            throw new ArgumentException("UserName cannot be null or empty.", nameof(options.UserName));

        if (string.IsNullOrWhiteSpace(options.Password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(options.Password));

        if (string.IsNullOrWhiteSpace(options.VirtualHost))
            throw new ArgumentException("VirtualHost cannot be null or empty.", nameof(options.VirtualHost));

        if (options.PrefetchCount == 0)
            throw new ArgumentException("PrefetchCount must be greater than 0.", nameof(options.PrefetchCount));

        if (string.IsNullOrWhiteSpace(options.ExchangeType))
            throw new ArgumentException("ExchangeType cannot be null or empty.", nameof(options.ExchangeType));
    }

    private static void ValidateKafkaOptions(KafkaOptions? options)
    {
        if (options == null) return;

        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
            throw new ArgumentException("BootstrapServers cannot be null or empty.", nameof(options.BootstrapServers));

        if (string.IsNullOrWhiteSpace(options.ConsumerGroupId))
            throw new ArgumentException("ConsumerGroupId cannot be null or empty.", nameof(options.ConsumerGroupId));
    }

    private static void ValidateAzureServiceBusOptions(AzureServiceBusOptions? options)
    {
        if (options == null) return;

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("ConnectionString cannot be null or empty.", nameof(options.ConnectionString));

        if (options.EntityType == AzureEntityType.Topic && string.IsNullOrWhiteSpace(options.SubscriptionName))
            throw new ArgumentException("SubscriptionName is required when EntityType is Topic.", nameof(options.SubscriptionName));
    }

    private static void ValidateAwsSqsSnsOptions(AwsSqsSnsOptions? options)
    {
        if (options == null) return;

        if (string.IsNullOrWhiteSpace(options.Region))
            throw new ArgumentException("Region cannot be null or empty.", nameof(options.Region));
    }

    private static void ValidateNatsOptions(NatsOptions? options)
    {
        if (options == null) return;

        if (options.Servers == null || options.Servers.Length == 0)
            throw new ArgumentException("Servers cannot be null or empty.", nameof(options.Servers));
    }

    private static void ValidateRedisStreamsOptions(RedisStreamsOptions? options)
    {
        if (options == null) return;

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("ConnectionString cannot be null or empty.", nameof(options.ConnectionString));

        if (string.IsNullOrWhiteSpace(options.DefaultStreamName))
            throw new ArgumentException("DefaultStreamName cannot be null or empty.", nameof(options.DefaultStreamName));

        if (string.IsNullOrWhiteSpace(options.ConsumerGroupName))
            throw new ArgumentException("ConsumerGroupName cannot be null or empty.", nameof(options.ConsumerGroupName));

        if (string.IsNullOrWhiteSpace(options.ConsumerName))
            throw new ArgumentException("ConsumerName cannot be null or empty.", nameof(options.ConsumerName));
    }

    private static void ValidateRetryPolicy(RetryPolicy retryPolicy)
    {
        if (retryPolicy.MaxAttempts < 0)
            throw new ArgumentOutOfRangeException(nameof(retryPolicy.MaxAttempts), "MaxAttempts cannot be negative.");

        if (retryPolicy.InitialDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retryPolicy.InitialDelay), "InitialDelay must be greater than zero.");

        if (retryPolicy.MaxDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retryPolicy.MaxDelay), "MaxDelay must be greater than zero.");

        if (retryPolicy.BackoffMultiplier <= 0)
            throw new ArgumentException("BackoffMultiplier must be greater than zero.", nameof(retryPolicy.BackoffMultiplier));
    }

    private static void ValidateCircuitBreakerOptions(CircuitBreaker.CircuitBreakerOptions circuitBreakerOptions)
    {
        if (circuitBreakerOptions.FailureThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.FailureThreshold), "FailureThreshold must be greater than 0.");

        if (circuitBreakerOptions.SuccessThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.SuccessThreshold), "SuccessThreshold must be greater than 0.");

        if (circuitBreakerOptions.Timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.Timeout), "Timeout must be greater than zero.");

        if (circuitBreakerOptions.SamplingDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.SamplingDuration), "SamplingDuration must be greater than zero.");

        if (circuitBreakerOptions.MinimumThroughput <= 0)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.MinimumThroughput), "MinimumThroughput must be greater than 0.");

        if (circuitBreakerOptions.FailureRateThreshold <= 0 || circuitBreakerOptions.FailureRateThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.FailureRateThreshold), "FailureRateThreshold must be between 0 and 1.");

        if (circuitBreakerOptions.HalfOpenDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.HalfOpenDuration), "HalfOpenDuration must be greater than zero.");

        if (circuitBreakerOptions.SlowCallDurationThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.SlowCallDurationThreshold), "SlowCallDurationThreshold must be greater than zero.");

        if (circuitBreakerOptions.SlowCallRateThreshold <= 0 || circuitBreakerOptions.SlowCallRateThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(circuitBreakerOptions.SlowCallRateThreshold), "SlowCallRateThreshold must be between 0 and 1.");
    }

    private static void ValidateCompressionOptions(Compression.CompressionOptions compressionOptions)
    {
        // Validate algorithm
        if (!Enum.IsDefined(typeof(Relay.Core.Caching.Compression.CompressionAlgorithm), compressionOptions.Algorithm))
            throw new InvalidOperationException("Unsupported compression algorithm.");

        if (compressionOptions.Level < 0 || compressionOptions.Level > 9)
            throw new ArgumentException("Level must be between 0 and 9.", nameof(compressionOptions.Level));

        if (compressionOptions.MinimumSizeBytes < 0)
            throw new ArgumentException("MinimumSizeBytes cannot be negative.", nameof(compressionOptions.MinimumSizeBytes));

        if (compressionOptions.ExpectedCompressionRatio <= 0 || compressionOptions.ExpectedCompressionRatio > 1)
            throw new ArgumentException("ExpectedCompressionRatio must be between 0 and 1.", nameof(compressionOptions.ExpectedCompressionRatio));
    }

    #region Fluent Configuration API

    /// <summary>
    /// Provides a fluent API for configuring message broker with all patterns and features.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for message broker options.</param>
    /// <returns>A fluent configuration builder.</returns>
    public static IMessageBrokerBuilder AddMessageBrokerWithPatterns(
        this IServiceCollection services,
        Action<MessageBrokerOptions> configure)
    {
        // Add base message broker
        services.AddMessageBroker(configure);

        return new MessageBrokerBuilder(services);
    }

    /// <summary>
    /// Applies a default configuration profile to the message broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="profile">The configuration profile to apply.</param>
    /// <param name="configure">Configuration action for message broker options.</param>
    /// <returns>A fluent configuration builder.</returns>
    public static IMessageBrokerBuilder AddMessageBrokerWithProfile(
        this IServiceCollection services,
        MessageBrokerProfile profile,
        Action<MessageBrokerOptions> configure)
    {
        // Add base message broker
        services.AddMessageBroker(configure);

        var builder = new MessageBrokerBuilder(services);

        // Apply profile
        switch (profile)
        {
            case MessageBrokerProfile.Development:
                builder.ApplyDevelopmentProfile();
                break;
            case MessageBrokerProfile.Production:
                builder.ApplyProductionProfile();
                break;
            case MessageBrokerProfile.HighThroughput:
                builder.ApplyHighThroughputProfile();
                break;
            case MessageBrokerProfile.HighReliability:
                builder.ApplyHighReliabilityProfile();
                break;
            default:
                throw new ArgumentException($"Unknown profile: {profile}", nameof(profile));
        }

        return builder;
    }

    #endregion
}
