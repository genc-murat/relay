using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.MessageBroker;
using Relay.MessageBroker.Backpressure;
using Relay.MessageBroker.Batch;
using Relay.MessageBroker.Bulkhead;
using Relay.MessageBroker.Deduplication;
using Relay.MessageBroker.DistributedTracing;
using Relay.MessageBroker.Inbox;
using Relay.MessageBroker.Outbox;
using Relay.MessageBroker.PoisonMessage;
using Relay.MessageBroker.RabbitMQ;
using Relay.MessageBroker.RateLimit;
using Relay.MessageBroker.Security;

namespace Relay.MessageBroker.Examples;

/// <summary>
/// Examples demonstrating the fluent configuration API for message broker.
/// </summary>
public static class FluentConfigurationExample
{
    /// <summary>
    /// Example 1: Development configuration with minimal features.
    /// </summary>
    public static void ConfigureForDevelopment(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessageBrokerWithProfile(
            MessageBrokerProfile.Development,
            options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672,
                    UserName = "guest",
                    Password = "guest",
                    VirtualHost = "/"
                };
            });
    }

    /// <summary>
    /// Example 2: Production configuration with all reliability features.
    /// </summary>
    public static void ConfigureForProduction(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessageBrokerWithProfile(
            MessageBrokerProfile.Production,
            options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = configuration["RabbitMQ:HostName"] ?? "rabbitmq",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
                };

                // Enable circuit breaker
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 2,
                    Timeout = TimeSpan.FromSeconds(30),
                    SamplingDuration = TimeSpan.FromSeconds(60)
                };

                // Enable retry policy
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromSeconds(30),
                    BackoffMultiplier = 2.0
                };
            });
    }

    /// <summary>
    /// Example 3: Custom configuration with specific features.
    /// </summary>
    public static void ConfigureCustom(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessageBrokerWithPatterns(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
        })
        .WithOutbox(options =>
        {
            options.Enabled = true;
            options.PollingInterval = TimeSpan.FromSeconds(5);
            options.BatchSize = 100;
            options.MaxRetryAttempts = 3;
        })
        .WithInbox(options =>
        {
            options.Enabled = true;
            options.RetentionPeriod = TimeSpan.FromDays(7);
            options.CleanupInterval = TimeSpan.FromHours(1);
        })
        .WithConnectionPool(options =>
        {
            options.Enabled = true;
            options.MinPoolSize = 5;
            options.MaxPoolSize = 50;
            options.ConnectionTimeout = TimeSpan.FromSeconds(5);
        })
        .WithHealthChecks()
        .WithMetrics()
        .WithDistributedTracing(options =>
        {
            options.EnableTracing = true;
            options.ServiceName = "MyService";
            options.SamplingRate = 1.0;
        })
        .Build();
    }

    /// <summary>
    /// Example 4: High throughput configuration for event processing.
    /// </summary>
    public static void ConfigureHighThroughput(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessageBrokerWithPatterns(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                PrefetchCount = 1000 // High prefetch for throughput
            };
        })
        .WithConnectionPool(options =>
        {
            options.Enabled = true;
            options.MinPoolSize = 10;
            options.MaxPoolSize = 100;
        })
        .WithBatching(options =>
        {
            options.Enabled = true;
            options.MaxBatchSize = 1000;
            options.FlushInterval = TimeSpan.FromMilliseconds(50);
            options.EnableCompression = true;
            options.PartialRetry = true;
        })
        .WithDeduplication(options =>
        {
            options.Enabled = true;
            options.Window = TimeSpan.FromMinutes(1);
            options.MaxCacheSize = 100_000;
            options.Strategy = DeduplicationStrategy.ContentHash;
        })
        .WithBackpressure(options =>
        {
            options.Enabled = true;
            options.LatencyThreshold = TimeSpan.FromSeconds(2);
            options.QueueDepthThreshold = 10000;
        })
        .WithHealthChecks()
        .WithMetrics()
        .Build();
    }

    /// <summary>
    /// Example 5: Secure multi-tenant configuration.
    /// </summary>
    public static void ConfigureSecureMultiTenant(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessageBrokerWithPatterns(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = 5672,
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };
        })
        .WithOutbox()
        .WithInbox()
        .WithEncryption(options =>
        {
            options.EnableEncryption = true;
            options.EncryptionAlgorithm = "AES256";
            // Key will be loaded from environment variable or Key Vault
        })
        .WithAuthentication(
            authOptions =>
            {
                authOptions.EnableAuthentication = true;
                authOptions.JwtIssuer = configuration["Auth:Issuer"];
                authOptions.JwtAudience = configuration["Auth:Audience"];
                authOptions.TokenCacheTtl = TimeSpan.FromMinutes(5);
            },
            authzOptions =>
            {
                authzOptions.EnableAuthorization = true;
                authzOptions.RolePermissions = new Dictionary<string, string[]>
                {
                    ["Publisher"] = new[] { "publish" },
                    ["Consumer"] = new[] { "subscribe" },
                    ["Admin"] = new[] { "publish", "subscribe", "manage" }
                };
            })
        .WithRateLimit(options =>
        {
            options.Enabled = true;
            options.Strategy = RateLimitStrategy.TokenBucket;
            options.EnablePerTenantLimits = true;
            options.DefaultTenantLimit = 100;
            options.TenantLimits = new Dictionary<string, int>
            {
                ["premium-tenant"] = 1000,
                ["standard-tenant"] = 100,
                ["free-tenant"] = 10
            };
        })
        .WithHealthChecks()
        .WithMetrics()
        .WithDistributedTracing()
        .Build();
    }

    /// <summary>
    /// Example 6: Mission-critical system with all resilience patterns.
    /// </summary>
    public static void ConfigureMissionCritical(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMessageBrokerWithProfile(
            MessageBrokerProfile.HighReliability,
            options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                    Port = 5672,
                    UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest"
                };

                // Aggressive circuit breaker settings
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 3,
                    SuccessThreshold = 2,
                    Timeout = TimeSpan.FromSeconds(30),
                    SamplingDuration = TimeSpan.FromSeconds(60),
                    MinimumThroughput = 10,
                    FailureRateThreshold = 0.5
                };

                // Aggressive retry policy
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialDelay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromMinutes(1),
                    BackoffMultiplier = 2.0
                };
            });
    }

    /// <summary>
    /// Example 7: Incremental feature adoption.
    /// </summary>
    public static void ConfigureIncremental(IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddMessageBrokerWithPatterns(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };
        });

        // Start with basic features
        builder.WithHealthChecks();
        builder.WithMetrics();

        // Add reliability patterns based on feature flags
        if (configuration.GetValue<bool>("Features:EnableOutbox"))
        {
            builder.WithOutbox();
        }

        if (configuration.GetValue<bool>("Features:EnableInbox"))
        {
            builder.WithInbox();
        }

        // Add performance optimizations
        if (configuration.GetValue<bool>("Features:EnableBatching"))
        {
            builder.WithBatching(options =>
            {
                options.MaxBatchSize = configuration.GetValue<int>("Batching:MaxSize", 100);
                options.FlushInterval = TimeSpan.FromMilliseconds(
                    configuration.GetValue<int>("Batching:FlushMs", 100));
            });
        }

        // Add security features
        if (configuration.GetValue<bool>("Features:EnableEncryption"))
        {
            builder.WithEncryption();
        }

        if (configuration.GetValue<bool>("Features:EnableAuthentication"))
        {
            builder.WithAuthentication();
        }

        // Build the configuration
        builder.Build();
    }

    /// <summary>
    /// Example 8: Complete ASP.NET Core application setup.
    /// </summary>
    public static void ConfigureCompleteApplication(IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        // Add message broker with production profile
        builder.Services.AddMessageBrokerWithProfile(
            MessageBrokerProfile.Production,
            options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest",
                    VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
                };
            });

        // Add message broker hosted service to start/stop automatically
        builder.Services.AddMessageBrokerHostedService();

        // Configure health check endpoint
        builder.Services.AddHealthChecks();

        // The application is now ready to use the message broker
    }
}
