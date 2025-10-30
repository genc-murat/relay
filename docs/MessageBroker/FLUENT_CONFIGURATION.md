# Fluent Configuration API

The Relay.MessageBroker provides a fluent configuration API that makes it easy to configure all patterns and features in a clean, readable way.

## Table of Contents

- [Basic Usage](#basic-usage)
- [Configuration Profiles](#configuration-profiles)
- [Individual Features](#individual-features)
- [Complete Examples](#complete-examples)

## Basic Usage

### Simple Configuration

```csharp
services.AddMessageBrokerWithPatterns(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };
})
.WithOutbox()
.WithInbox()
.WithHealthChecks()
.Build();
```

## Configuration Profiles

The fluent API provides pre-configured profiles for common scenarios:

### Development Profile

Minimal features with in-memory stores, suitable for local development:

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Development,
    options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        options.RabbitMQ = new RabbitMQOptions
        {
            HostName = "localhost",
            Port = 5672
        };
    });
```

**Includes:**
- Connection pooling (1-5 connections)
- Health checks
- Metrics

### Production Profile

All reliability and observability features enabled:

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Production,
    options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        options.RabbitMQ = new RabbitMQOptions
        {
            HostName = "rabbitmq.production.com",
            Port = 5672
        };
    });
```

**Includes:**
- Outbox pattern (5s polling, batch size 100)
- Inbox pattern (7 day retention)
- Connection pooling (5-50 connections)
- Message deduplication (5 minute window)
- Health checks
- Metrics
- Distributed tracing
- Bulkhead pattern (100 concurrent operations)
- Poison message handling (5 failure threshold)
- Backpressure management

### High Throughput Profile

Optimized for maximum performance:

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.HighThroughput,
    options =>
    {
        options.BrokerType = MessageBrokerType.Kafka;
        options.Kafka = new KafkaOptions
        {
            BootstrapServers = "kafka:9092"
        };
    });
```

**Includes:**
- Connection pooling (10-100 connections)
- Batch processing (1000 messages, 50ms flush)
- Message deduplication (1 minute window, 100k cache)
- Health checks
- Metrics
- Backpressure management (2s latency threshold)

### High Reliability Profile

All resilience patterns enabled:

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.HighReliability,
    options =>
    {
        options.BrokerType = MessageBrokerType.AzureServiceBus;
        options.AzureServiceBus = new AzureServiceBusOptions
        {
            ConnectionString = configuration["AzureServiceBus:ConnectionString"]
        };
    });
```

**Includes:**
- Outbox pattern (2s polling, 5 max retries)
- Inbox pattern (30 day retention)
- Connection pooling (5-50 connections, 15s validation)
- Message deduplication (10 minute window)
- Health checks
- Metrics
- Distributed tracing
- Rate limiting (1000 req/s, token bucket)
- Bulkhead pattern (50 concurrent, 500 queued)
- Poison message handling (3 failure threshold, 30 day retention)
- Backpressure management (10s latency threshold)

## Individual Features

### Outbox Pattern

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithOutbox(options =>
    {
        options.Enabled = true;
        options.PollingInterval = TimeSpan.FromSeconds(5);
        options.BatchSize = 100;
        options.MaxRetryAttempts = 3;
    })
    .Build();
```

### Inbox Pattern

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithInbox(options =>
    {
        options.Enabled = true;
        options.RetentionPeriod = TimeSpan.FromDays(7);
        options.CleanupInterval = TimeSpan.FromHours(1);
    })
    .Build();
```

### Connection Pooling

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithConnectionPool(options =>
    {
        options.Enabled = true;
        options.MinPoolSize = 5;
        options.MaxPoolSize = 50;
        options.ConnectionTimeout = TimeSpan.FromSeconds(5);
        options.ValidationInterval = TimeSpan.FromSeconds(30);
    })
    .Build();
```

### Batch Processing

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithBatching(options =>
    {
        options.Enabled = true;
        options.MaxBatchSize = 100;
        options.FlushInterval = TimeSpan.FromMilliseconds(100);
        options.EnableCompression = true;
        options.PartialRetry = true;
    })
    .Build();
```

### Message Deduplication

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithDeduplication(options =>
    {
        options.Enabled = true;
        options.Window = TimeSpan.FromMinutes(5);
        options.MaxCacheSize = 100_000;
        options.Strategy = DeduplicationStrategy.ContentHash;
    })
    .Build();
```

### Health Checks

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithHealthChecks(options =>
    {
        options.Interval = TimeSpan.FromSeconds(30);
        options.ConnectivityTimeout = TimeSpan.FromSeconds(2);
        options.IncludeCircuitBreakerState = true;
        options.IncludeConnectionPoolMetrics = true;
    })
    .Build();
```

### Metrics and Telemetry

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithMetrics()
    .Build();
```

### Distributed Tracing

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithDistributedTracing(options =>
    {
        options.EnableTracing = true;
        options.ServiceName = "MyService";
        options.SamplingRate = 1.0;
        options.OtlpExporter = new OtlpExporterOptions
        {
            Enabled = true,
            Endpoint = "http://localhost:4317"
        };
    })
    .Build();
```

### Message Encryption

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithEncryption(options =>
    {
        options.EnableEncryption = true;
        options.EncryptionAlgorithm = "AES256";
        options.KeyVaultUrl = "https://myvault.vault.azure.net/"
    })
    .Build();
```

### Authentication and Authorization

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithAuthentication(
        authOptions =>
        {
            authOptions.EnableAuthentication = true;
            authOptions.JwtIssuer = "https://myissuer.com";
            authOptions.JwtAudience = "myapi";
        },
        authzOptions =>
        {
            authzOptions.EnableAuthorization = true;
            authzOptions.RolePermissions = new Dictionary<string, string[]>
            {
                ["Publisher"] = new[] { "publish" },
                ["Consumer"] = new[] { "subscribe" },
                ["Admin"] = new[] { "publish", "subscribe" }
            };
        })
    .Build();
```

### Rate Limiting

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithRateLimit(options =>
    {
        options.Enabled = true;
        options.RequestsPerSecond = 1000;
        options.Strategy = RateLimitStrategy.TokenBucket;
        options.EnablePerTenantLimits = true;
    })
    .Build();
```

### Bulkhead Pattern

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithBulkhead(options =>
    {
        options.Enabled = true;
        options.MaxConcurrentOperations = 100;
        options.MaxQueuedOperations = 1000;
        options.AcquisitionTimeout = TimeSpan.FromSeconds(30);
    })
    .Build();
```

### Poison Message Handling

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithPoisonMessageHandling(options =>
    {
        options.Enabled = true;
        options.FailureThreshold = 5;
        options.RetentionPeriod = TimeSpan.FromDays(7);
        options.CleanupInterval = TimeSpan.FromHours(1);
    })
    .Build();
```

### Backpressure Management

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithBackpressure(options =>
    {
        options.Enabled = true;
        options.LatencyThreshold = TimeSpan.FromSeconds(5);
        options.QueueDepthThreshold = 10000;
        options.RecoveryLatencyThreshold = TimeSpan.FromSeconds(2);
    })
    .Build();
```

## Complete Examples

### Microservice with Full Observability

```csharp
services.AddMessageBrokerWithPatterns(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = configuration["RabbitMQ:HostName"],
        Port = 5672,
        UserName = configuration["RabbitMQ:UserName"],
        Password = configuration["RabbitMQ:Password"]
    };
})
.WithOutbox()
.WithInbox()
.WithConnectionPool()
.WithHealthChecks()
.WithMetrics()
.WithDistributedTracing(options =>
{
    options.ServiceName = "OrderService";
    options.OtlpExporter = new OtlpExporterOptions
    {
        Enabled = true,
        Endpoint = "http://jaeger:4317"
    };
})
.Build();
```

### High-Volume Event Processing

```csharp
services.AddMessageBrokerWithPatterns(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.Kafka = new KafkaOptions
    {
        BootstrapServers = "kafka:9092",
        ConsumerGroupId = "event-processor"
    };
})
.WithConnectionPool(options =>
{
    options.MinPoolSize = 10;
    options.MaxPoolSize = 100;
})
.WithBatching(options =>
{
    options.MaxBatchSize = 1000;
    options.FlushInterval = TimeSpan.FromMilliseconds(50);
    options.EnableCompression = true;
})
.WithDeduplication(options =>
{
    options.Window = TimeSpan.FromMinutes(1);
    options.MaxCacheSize = 100_000;
})
.WithBackpressure()
.WithHealthChecks()
.WithMetrics()
.Build();
```

### Secure Multi-Tenant System

```csharp
services.AddMessageBrokerWithPatterns(options =>
{
    options.BrokerType = MessageBrokerType.AzureServiceBus;
    options.AzureServiceBus = new AzureServiceBusOptions
    {
        ConnectionString = configuration["AzureServiceBus:ConnectionString"]
    };
})
.WithOutbox()
.WithInbox()
.WithEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVaultUrl = configuration["KeyVault:Url"];
})
.WithAuthentication(
    authOptions =>
    {
        authOptions.EnableAuthentication = true;
        authOptions.JwtIssuer = configuration["Auth:Issuer"];
        authOptions.JwtAudience = configuration["Auth:Audience"];
    },
    authzOptions =>
    {
        authzOptions.EnableAuthorization = true;
    })
.WithRateLimit(options =>
{
    options.EnablePerTenantLimits = true;
    options.DefaultTenantLimit = 100;
    options.TenantLimits = new Dictionary<string, int>
    {
        ["premium-tenant"] = 1000,
        ["standard-tenant"] = 100
    };
})
.WithHealthChecks()
.WithMetrics()
.WithDistributedTracing()
.Build();
```

### Mission-Critical System

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.HighReliability,
    options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        options.RabbitMQ = new RabbitMQOptions
        {
            HostName = configuration["RabbitMQ:HostName"],
            Port = 5672
        };
        
        // Enable circuit breaker
        options.CircuitBreaker = new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromSeconds(30)
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
```

## Validation

All options are automatically validated when `Build()` is called. If any configuration is invalid, an `InvalidOperationException` will be thrown with details about the validation failure.

```csharp
try
{
    services.AddMessageBrokerWithPatterns(options => { /* ... */ })
        .WithOutbox(options =>
        {
            options.PollingInterval = TimeSpan.FromMilliseconds(50); // Invalid: < 100ms
        })
        .Build();
}
catch (InvalidOperationException ex)
{
    // Handle validation error
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

## Best Practices

1. **Use Profiles for Common Scenarios**: Start with a profile and customize as needed
2. **Enable Features Incrementally**: Add patterns one at a time in development
3. **Validate Early**: Call `Build()` during application startup to catch configuration errors
4. **Monitor in Production**: Always enable health checks and metrics in production
5. **Secure Sensitive Data**: Use encryption and authentication for production workloads
6. **Plan for Failures**: Enable outbox, inbox, and poison message handling for reliability
7. **Optimize for Your Workload**: Use high throughput profile for event processing, high reliability for critical systems

## Migration from Legacy Configuration

If you're migrating from the legacy configuration approach:

### Before

```csharp
services.AddMessageBroker(options => { /* ... */ });
services.AddOutboxPattern();
services.DecorateMessageBrokerWithOutbox();
services.AddInboxPattern();
services.DecorateMessageBrokerWithInbox();
services.AddMessageBrokerHealthChecks();
```

### After

```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithOutbox()
    .WithInbox()
    .WithHealthChecks()
    .Build();
```

The fluent API automatically handles service registration and decorator ordering.
