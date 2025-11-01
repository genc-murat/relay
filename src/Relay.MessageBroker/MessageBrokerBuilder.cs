using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Extensions;
using Relay.MessageBroker.Backpressure;
using Relay.MessageBroker.Batch;
using Relay.MessageBroker.Bulkhead;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.Deduplication;
using Relay.MessageBroker.DistributedTracing;
using Relay.MessageBroker.HealthChecks;
using Relay.MessageBroker.Inbox;
using Relay.MessageBroker.Metrics;
using Relay.MessageBroker.Outbox;
using Relay.MessageBroker.PoisonMessage;
using Relay.MessageBroker.RateLimit;
using Relay.MessageBroker.Security;

namespace Relay.MessageBroker;

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
