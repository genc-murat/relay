using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.ContractValidation;
using Relay.Core.Extensions;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.AzureServiceBus;
using Relay.MessageBroker.Backpressure;
using Relay.MessageBroker.Batch;
using Relay.MessageBroker.Bulkhead;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.Deduplication;
using Relay.MessageBroker.DistributedTracing;
using Relay.MessageBroker.HealthChecks;
using Relay.MessageBroker.Inbox;
using Relay.MessageBroker.Kafka;
using Relay.MessageBroker.Metrics;
using Relay.MessageBroker.Nats;
using Relay.MessageBroker.Outbox;
using Relay.MessageBroker.PoisonMessage;
using Relay.MessageBroker.RabbitMQ;
using Relay.MessageBroker.RateLimit;
using Relay.MessageBroker.RedisStreams;
using Relay.MessageBroker.Security;

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

/// <summary>
/// Configuration profiles for message broker.
/// </summary>
public enum MessageBrokerProfile
{
    /// <summary>
    /// Development profile with in-memory stores and minimal features.
    /// </summary>
    Development,

    /// <summary>
    /// Production profile with all reliability and observability features enabled.
    /// </summary>
    Production,

    /// <summary>
    /// High throughput profile optimized for performance.
    /// </summary>
    HighThroughput,

    /// <summary>
    /// High reliability profile with all resilience patterns enabled.
    /// </summary>
    HighReliability
}

/// <summary>
/// Fluent builder interface for configuring message broker patterns and features.
/// </summary>
public interface IMessageBrokerBuilder
{
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds the Outbox pattern for reliable message publishing.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithOutbox(Action<OutboxOptions>? configure = null);

    /// <summary>
    /// Adds the Inbox pattern for idempotent message processing.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithInbox(Action<InboxOptions>? configure = null);

    /// <summary>
    /// Adds connection pooling for improved performance.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithConnectionPool(Action<ConnectionPoolOptions>? configure = null);

    /// <summary>
    /// Adds batch processing for high-volume scenarios.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithBatching(Action<BatchOptions>? configure = null);

    /// <summary>
    /// Adds message deduplication.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithDeduplication(Action<DeduplicationOptions>? configure = null);

    /// <summary>
    /// Adds health checks for monitoring.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithHealthChecks(Action<HealthCheckOptions>? configure = null);

    /// <summary>
    /// Adds metrics and telemetry.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithMetrics();

    /// <summary>
    /// Adds distributed tracing.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithDistributedTracing(Action<DistributedTracingOptions>? configure = null);

    /// <summary>
    /// Adds message encryption.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithEncryption(Action<SecurityOptions>? configure = null);

    /// <summary>
    /// Adds authentication and authorization.
    /// </summary>
    /// <param name="configureAuth">Optional authentication configuration action.</param>
    /// <param name="configureAuthz">Optional authorization configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithAuthentication(
        Action<AuthenticationOptions>? configureAuth = null,
        Action<AuthorizationOptions>? configureAuthz = null);

    /// <summary>
    /// Adds rate limiting.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithRateLimit(Action<RateLimitOptions>? configure = null);

    /// <summary>
    /// Adds bulkhead pattern for resource isolation.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithBulkhead(Action<BulkheadOptions>? configure = null);

    /// <summary>
    /// Adds poison message handling.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithPoisonMessageHandling(Action<PoisonMessageOptions>? configure = null);

    /// <summary>
    /// Adds backpressure management.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    IMessageBrokerBuilder WithBackpressure(Action<BackpressureOptions>? configure = null);

    /// <summary>
    /// Builds and registers all configured components.
    /// </summary>
    /// <returns>The service collection.</returns>
    IServiceCollection Build();
}

/// <summary>
/// Implementation of the fluent message broker builder.
/// </summary>
internal sealed class MessageBrokerBuilder : IMessageBrokerBuilder
{
    private readonly List<Action<IServiceCollection>> _configurations = new();

    public MessageBrokerBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }

    public IMessageBrokerBuilder WithOutbox(Action<OutboxOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddOutboxPattern(configure);
            services.DecorateMessageBrokerWithOutbox();
        });
        return this;
    }

    public IMessageBrokerBuilder WithInbox(Action<InboxOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddInboxPattern(configure);
            services.DecorateMessageBrokerWithInbox();
        });
        return this;
    }

    public IMessageBrokerBuilder WithConnectionPool(Action<ConnectionPoolOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddMessageBrokerConnectionPool(configure);
        });
        return this;
    }

    public IMessageBrokerBuilder WithBatching(Action<BatchOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<BatchOptions>(options => options.Enabled = true);
            }

            services.AddSingleton(typeof(IBatchProcessor<>), typeof(BatchProcessor<>));
            services.Decorate<IMessageBroker, BatchMessageBrokerDecorator>();
        });
        return this;
    }

    public IMessageBrokerBuilder WithDeduplication(Action<DeduplicationOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<DeduplicationOptions>(options => options.Enabled = true);
            }

            services.AddSingleton<IDeduplicationCache, DeduplicationCache>();
            services.Decorate<IMessageBroker, DeduplicationMessageBrokerDecorator>();
        });
        return this;
    }

    public IMessageBrokerBuilder WithHealthChecks(Action<HealthCheckOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddHealthChecks()
                .AddMessageBrokerHealthChecks(configureOptions: configure);
        });
        return this;
    }

    public IMessageBrokerBuilder WithMetrics()
    {
        _configurations.Add(services =>
        {
            services.AddSingleton<MessageBrokerMetrics>();
            services.AddSingleton<ConnectionPoolMetricsCollector>();
        });
        return this;
    }

    public IMessageBrokerBuilder WithDistributedTracing(Action<DistributedTracingOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddMessageBrokerOpenTelemetry(configure);
        });
        return this;
    }

    public IMessageBrokerBuilder WithEncryption(Action<SecurityOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddMessageEncryption(configure);
            services.DecorateWithEncryption();
        });
        return this;
    }

    public IMessageBrokerBuilder WithAuthentication(
        Action<AuthenticationOptions>? configureAuth = null,
        Action<AuthorizationOptions>? configureAuthz = null)
    {
        _configurations.Add(services =>
        {
            services.AddMessageAuthentication(configureAuth, configureAuthz);
            services.DecorateWithSecurity();
        });
        return this;
    }

    public IMessageBrokerBuilder WithRateLimit(Action<RateLimitOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            services.AddMessageBrokerRateLimit(configure);
            services.DecorateMessageBrokerWithRateLimit();
        });
        return this;
    }

    public IMessageBrokerBuilder WithBulkhead(Action<BulkheadOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<BulkheadOptions>(options => options.Enabled = true);
            }

            services.AddSingleton<IBulkhead, Bulkhead.Bulkhead>();
            services.Decorate<IMessageBroker, BulkheadMessageBrokerDecorator>();
        });
        return this;
    }

    public IMessageBrokerBuilder WithPoisonMessageHandling(Action<PoisonMessageOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<PoisonMessageOptions>(options => options.Enabled = true);
            }

            services.AddSingleton<IPoisonMessageHandler, PoisonMessageHandler>();
            services.AddHostedService<PoisonMessageCleanupWorker>();
        });
        return this;
    }

    public IMessageBrokerBuilder WithBackpressure(Action<BackpressureOptions>? configure = null)
    {
        _configurations.Add(services =>
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<BackpressureOptions>(options => options.Enabled = true);
            }

            services.AddSingleton<IBackpressureController, BackpressureController>();
        });
        return this;
    }

    public IServiceCollection Build()
    {
        // Apply all configurations
        foreach (var configuration in _configurations)
        {
            configuration(Services);
        }

        // Validate all registered options
        ValidateAllOptions(Services);

        return Services;
    }

    internal void ApplyDevelopmentProfile()
    {
        // Development profile: minimal features, in-memory stores, verbose logging
        WithConnectionPool(options =>
        {
            options.Enabled = true;
            options.MinPoolSize = 1;
            options.MaxPoolSize = 5;
        });

        WithHealthChecks();
        WithMetrics();
    }

    internal void ApplyProductionProfile()
    {
        // Production profile: all reliability and observability features
        WithOutbox(options =>
        {
            options.Enabled = true;
            options.PollingInterval = TimeSpan.FromSeconds(5);
            options.BatchSize = 100;
        });

        WithInbox(options =>
        {
            options.Enabled = true;
            options.RetentionPeriod = TimeSpan.FromDays(7);
        });

        WithConnectionPool(options =>
        {
            options.Enabled = true;
            options.MinPoolSize = 5;
            options.MaxPoolSize = 50;
        });

        WithDeduplication(options =>
        {
            options.Enabled = true;
            options.Window = TimeSpan.FromMinutes(5);
        });

        WithHealthChecks();
        WithMetrics();
        WithDistributedTracing();

        WithBulkhead(options =>
        {
            options.Enabled = true;
            options.MaxConcurrentOperations = 100;
        });

        WithPoisonMessageHandling(options =>
        {
            options.Enabled = true;
            options.FailureThreshold = 5;
        });

        WithBackpressure(options =>
        {
            options.Enabled = true;
        });
    }

    internal void ApplyHighThroughputProfile()
    {
        // High throughput profile: optimized for performance
        WithConnectionPool(options =>
        {
            options.Enabled = true;
            options.MinPoolSize = 10;
            options.MaxPoolSize = 100;
        });

        WithBatching(options =>
        {
            options.Enabled = true;
            options.MaxBatchSize = 1000;
            options.FlushInterval = TimeSpan.FromMilliseconds(50);
            options.EnableCompression = true;
        });

        WithDeduplication(options =>
        {
            options.Enabled = true;
            options.Window = TimeSpan.FromMinutes(1);
            options.MaxCacheSize = 100_000;
        });

        WithHealthChecks();
        WithMetrics();

        WithBackpressure(options =>
        {
            options.Enabled = true;
            options.LatencyThreshold = TimeSpan.FromSeconds(2);
        });
    }

    internal void ApplyHighReliabilityProfile()
    {
        // High reliability profile: all resilience patterns enabled
        WithOutbox(options =>
        {
            options.Enabled = true;
            options.PollingInterval = TimeSpan.FromSeconds(2);
            options.MaxRetryAttempts = 5;
        });

        WithInbox(options =>
        {
            options.Enabled = true;
            options.RetentionPeriod = TimeSpan.FromDays(30);
        });

        WithConnectionPool(options =>
        {
            options.Enabled = true;
            options.MinPoolSize = 5;
            options.MaxPoolSize = 50;
            options.ValidationInterval = TimeSpan.FromSeconds(15);
        });

        WithDeduplication(options =>
        {
            options.Enabled = true;
            options.Window = TimeSpan.FromMinutes(10);
        });

        WithHealthChecks();
        WithMetrics();
        WithDistributedTracing();

        WithRateLimit(options =>
        {
            options.Enabled = true;
            options.RequestsPerSecond = 1000;
            options.Strategy = RateLimitStrategy.TokenBucket;
        });

        WithBulkhead(options =>
        {
            options.Enabled = true;
            options.MaxConcurrentOperations = 50;
            options.MaxQueuedOperations = 500;
        });

        WithPoisonMessageHandling(options =>
        {
            options.Enabled = true;
            options.FailureThreshold = 3;
            options.RetentionPeriod = TimeSpan.FromDays(30);
        });

        WithBackpressure(options =>
        {
            options.Enabled = true;
            options.LatencyThreshold = TimeSpan.FromSeconds(10);
        });
    }

    private static void ValidateAllOptions(IServiceCollection services)
    {
        // Build a temporary service provider to validate options
        using var serviceProvider = services.BuildServiceProvider();

        // Validate each options type
        ValidateOptions<OutboxOptions>(serviceProvider);
        ValidateOptions<InboxOptions>(serviceProvider);
        ValidateOptions<ConnectionPoolOptions>(serviceProvider);
        ValidateOptions<BatchOptions>(serviceProvider);
        ValidateOptions<DeduplicationOptions>(serviceProvider);
        ValidateOptions<BulkheadOptions>(serviceProvider);
        ValidateOptions<PoisonMessageOptions>(serviceProvider);
        ValidateOptions<BackpressureOptions>(serviceProvider);
        ValidateOptions<RateLimitOptions>(serviceProvider);
    }

    private static void ValidateOptions<TOptions>(IServiceProvider serviceProvider) where TOptions : class
    {
        try
        {
            var options = serviceProvider.GetService<IOptions<TOptions>>();
            if (options?.Value != null)
            {
                // Try to call Validate method if it exists
                var validateMethod = typeof(TOptions).GetMethod("Validate");
                validateMethod?.Invoke(options.Value, null);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Validation failed for {typeof(TOptions).Name}: {ex.Message}", ex);
        }
    }
}
