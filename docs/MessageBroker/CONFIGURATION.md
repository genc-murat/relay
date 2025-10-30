# Configuration Guide

This guide provides detailed information about configuring all Relay.MessageBroker enhancements.

## Table of Contents

- [Outbox Pattern Configuration](#outbox-pattern-configuration)
- [Inbox Pattern Configuration](#inbox-pattern-configuration)
- [Connection Pool Configuration](#connection-pool-configuration)
- [Batch Processing Configuration](#batch-processing-configuration)
- [Deduplication Configuration](#deduplication-configuration)
- [Health Checks Configuration](#health-checks-configuration)
- [Metrics Configuration](#metrics-configuration)
- [Distributed Tracing Configuration](#distributed-tracing-configuration)
- [Encryption Configuration](#encryption-configuration)
- [Authentication Configuration](#authentication-configuration)
- [Rate Limiting Configuration](#rate-limiting-configuration)
- [Bulkhead Configuration](#bulkhead-configuration)
- [Poison Message Configuration](#poison-message-configuration)
- [Backpressure Configuration](#backpressure-configuration)
- [Configuration Profiles](#configuration-profiles)

## Outbox Pattern Configuration

### OutboxOptions

```csharp
public class OutboxOptions
{
    // Enable/disable the outbox pattern
    public bool Enabled { get; set; } = false;
    
    // How often to poll the outbox for pending messages
    // Minimum: 100ms, Default: 5 seconds
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    
    // Number of messages to process in each batch
    // Range: 1-10000, Default: 100
    public int BatchSize { get; set; } = 100;
    
    // Maximum number of retry attempts for failed messages
    // Range: 0-10, Default: 3
    public int MaxRetryAttempts { get; set; } = 3;
    
    // Delay between retry attempts (exponential backoff)
    // Default: 2 seconds
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    
    // Use exponential backoff for retries
    // Default: true
    public bool UseExponentialBackoff { get; set; } = true;
}
```

### Configuration Example

```csharp
builder.Services.AddOutboxPattern(options =>
{
    options.Enabled = true;
    options.PollingInterval = TimeSpan.FromSeconds(10);
    options.BatchSize = 50;
    options.MaxRetryAttempts = 5;
    options.RetryDelay = TimeSpan.FromSeconds(3);
    options.UseExponentialBackoff = true;
});
```

### Database Configuration

**SQL Server:**

```csharp
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IOutboxStore, SqlOutboxStore>();
```

**PostgreSQL:**

```csharp
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5))));

builder.Services.AddScoped<IOutboxStore, SqlOutboxStore>();
```

**In-Memory (Testing):**

```csharp
builder.Services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
```

## Inbox Pattern Configuration

### InboxOptions

```csharp
public class InboxOptions
{
    // Enable/disable the inbox pattern
    public bool Enabled { get; set; } = false;
    
    // How long to retain processed message IDs
    // Minimum: 24 hours, Default: 7 days
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
    
    // How often to clean up expired entries
    // Minimum: 1 hour, Default: 1 hour
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    
    // Consumer name for tracking
    // Default: null (uses machine name)
    public string? ConsumerName { get; set; }
}
```

### Configuration Example

```csharp
builder.Services.AddInboxPattern(options =>
{
    options.Enabled = true;
    options.RetentionPeriod = TimeSpan.FromDays(14);
    options.CleanupInterval = TimeSpan.FromHours(6);
    options.ConsumerName = "OrderService";
});
```

### Database Configuration

Similar to Outbox pattern, supports SQL Server, PostgreSQL, and In-Memory stores.

## Connection Pool Configuration

### ConnectionPoolOptions

```csharp
public class ConnectionPoolOptions
{
    // Minimum number of connections to maintain
    // Range: 1-100, Default: 5
    public int MinPoolSize { get; set; } = 5;
    
    // Maximum number of connections allowed
    // Range: 1-1000, Default: 50
    public int MaxPoolSize { get; set; } = 50;
    
    // Timeout for acquiring a connection
    // Default: 5 seconds
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    // How often to validate idle connections
    // Default: 30 seconds
    public TimeSpan ValidationInterval { get; set; } = TimeSpan.FromSeconds(30);
    
    // How long before idle connections are removed
    // Default: 5 minutes
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
    
    // Enable connection validation
    // Default: true
    public bool EnableValidation { get; set; } = true;
}
```

### Configuration Example

```csharp
builder.Services.AddConnectionPooling<IConnection>(options =>
{
    options.MinPoolSize = 10;
    options.MaxPoolSize = 100;
    options.ConnectionTimeout = TimeSpan.FromSeconds(10);
    options.ValidationInterval = TimeSpan.FromSeconds(60);
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.EnableValidation = true;
});
```

## Batch Processing Configuration

### BatchOptions

```csharp
public class BatchOptions
{
    // Enable/disable batch processing
    public bool Enabled { get; set; } = false;
    
    // Maximum number of messages per batch
    // Range: 1-10000, Default: 100
    public int MaxBatchSize { get; set; } = 100;
    
    // Maximum time to wait before flushing batch
    // Default: 100ms
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    
    // Enable compression for batches
    // Default: true
    public bool EnableCompression { get; set; } = true;
    
    // Compression algorithm (GZip, Brotli, Deflate)
    // Default: Brotli
    public CompressionAlgorithm CompressionAlgorithm { get; set; } = CompressionAlgorithm.Brotli;
    
    // Compression level (0-11 for Brotli, 0-9 for others)
    // Default: 6
    public int CompressionLevel { get; set; } = 6;
    
    // Enable partial retry for failed messages
    // Default: true
    public bool EnablePartialRetry { get; set; } = true;
}
```

### Configuration Example

```csharp
builder.Services.AddBatchProcessing(options =>
{
    options.Enabled = true;
    options.MaxBatchSize = 200;
    options.FlushInterval = TimeSpan.FromMilliseconds(50);
    options.EnableCompression = true;
    options.CompressionAlgorithm = CompressionAlgorithm.Brotli;
    options.CompressionLevel = 9; // Maximum compression
    options.EnablePartialRetry = true;
});
```

## Deduplication Configuration

### DeduplicationOptions

```csharp
public class DeduplicationOptions
{
    // Enable/disable deduplication
    public bool Enabled { get; set; } = false;
    
    // Time window for duplicate detection
    // Range: 1 minute - 24 hours, Default: 5 minutes
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(5);
    
    // Maximum cache size
    // Range: 1000-1000000, Default: 100000
    public int MaxCacheSize { get; set; } = 100000;
    
    // Deduplication strategy
    // Options: ContentHash, MessageId, Custom
    // Default: ContentHash
    public DeduplicationStrategy Strategy { get; set; } = DeduplicationStrategy.ContentHash;
    
    // Custom hash function (for Custom strategy)
    public Func<object, string>? CustomHashFunction { get; set; }
}
```

### Configuration Example

```csharp
builder.Services.AddDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(10);
    options.MaxCacheSize = 200000;
    options.Strategy = DeduplicationStrategy.ContentHash;
});

// Custom strategy example
builder.Services.AddDeduplication(options =>
{
    options.Strategy = DeduplicationStrategy.Custom;
    options.CustomHashFunction = message =>
    {
        // Custom logic to generate hash
        if (message is OrderCreatedEvent order)
        {
            return $"{order.OrderId}:{order.CreatedAt:yyyyMMddHHmmss}";
        }
        return message.GetHashCode().ToString();
    };
});
```

## Health Checks Configuration

### HealthCheckOptions

```csharp
public class HealthCheckOptions
{
    // Health check interval
    // Minimum: 5 seconds, Default: 30 seconds
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    
    // Timeout for health check operations
    // Default: 2 seconds
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    
    // Include detailed diagnostics
    // Default: true
    public bool IncludeDiagnostics { get; set; } = true;
    
    // Check broker connectivity
    // Default: true
    public bool CheckBrokerConnectivity { get; set; } = true;
    
    // Check circuit breaker state
    // Default: true
    public bool CheckCircuitBreaker { get; set; } = true;
    
    // Check connection pool metrics
    // Default: true
    public bool CheckConnectionPool { get; set; } = true;
}
```

### Configuration Example

```csharp
builder.Services.AddMessageBrokerHealthChecks(options =>
{
    options.CheckInterval = TimeSpan.FromSeconds(60);
    options.Timeout = TimeSpan.FromSeconds(5);
    options.IncludeDiagnostics = true;
    options.CheckBrokerConnectivity = true;
    options.CheckCircuitBreaker = true;
    options.CheckConnectionPool = true;
});

// Add health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteResponse
});
```

## Metrics Configuration

### MetricsOptions

```csharp
public class MetricsOptions
{
    // Enable metrics collection
    // Default: true
    public bool EnableMetrics { get; set; } = true;
    
    // Enable Prometheus exporter
    // Default: false
    public bool EnablePrometheusExporter { get; set; } = false;
    
    // Prometheus endpoint path
    // Default: "/metrics"
    public string PrometheusEndpoint { get; set; } = "/metrics";
    
    // Metric labels
    public Dictionary<string, string> Labels { get; set; } = new();
    
    // Histogram buckets for latency metrics (milliseconds)
    public double[] LatencyBuckets { get; set; } = new[]
    {
        1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000
    };
}
```

### Configuration Example

```csharp
builder.Services.AddMessageBrokerMetrics(options =>
{
    options.EnableMetrics = true;
    options.EnablePrometheusExporter = true;
    options.PrometheusEndpoint = "/metrics";
    options.Labels = new Dictionary<string, string>
    {
        ["environment"] = "production",
        ["service"] = "order-service",
        ["version"] = "1.0.0"
    };
    options.LatencyBuckets = new[] { 1.0, 10.0, 50.0, 100.0, 500.0, 1000.0 };
});

// Add Prometheus endpoint
app.MapPrometheusScrapingEndpoint(options.PrometheusEndpoint);
```

## Distributed Tracing Configuration

### DistributedTracingOptions

```csharp
public class DistributedTracingOptions
{
    // Service name for tracing
    // Required
    public string ServiceName { get; set; } = "MessageBroker";
    
    // Service version
    public string? ServiceVersion { get; set; }
    
    // Enable tracing
    // Default: true
    public bool EnableTracing { get; set; } = true;
    
    // Sampling rate (0.0 - 1.0)
    // Default: 1.0 (100%)
    public double SamplingRate { get; set; } = 1.0;
    
    // Capture message payloads in spans
    // Default: false (for security)
    public bool CaptureMessagePayloads { get; set; } = false;
    
    // Capture message headers in spans
    // Default: true
    public bool CaptureMessageHeaders { get; set; } = true;
    
    // Excluded header keys (for security)
    public List<string> ExcludedHeaderKeys { get; set; } = new()
    {
        "Authorization",
        "X-API-Key",
        "Password"
    };
    
    // Exporters to enable
    public TracingExporter[] Exporters { get; set; } = Array.Empty<TracingExporter>();
    
    // OTLP exporter endpoint
    public string? OtlpEndpoint { get; set; }
    
    // Jaeger exporter configuration
    public string? JaegerAgentHost { get; set; }
    public int JaegerAgentPort { get; set; } = 6831;
    
    // Zipkin exporter endpoint
    public string? ZipkinEndpoint { get; set; }
}
```

### Configuration Example

```csharp
builder.Services.AddDistributedTracing(options =>
{
    options.ServiceName = "OrderService";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.SamplingRate = 0.1; // 10% sampling in production
    options.CaptureMessagePayloads = false; // Don't capture sensitive data
    options.CaptureMessageHeaders = true;
    options.ExcludedHeaderKeys = new List<string>
    {
        "Authorization",
        "X-API-Key",
        "Password",
        "CreditCard"
    };
    options.Exporters = new[]
    {
        TracingExporter.OTLP,
        TracingExporter.Jaeger,
        TracingExporter.Zipkin
    };
    options.OtlpEndpoint = "http://otel-collector:4317";
    options.JaegerAgentHost = "jaeger";
    options.JaegerAgentPort = 6831;
    options.ZipkinEndpoint = "http://zipkin:9411/api/v2/spans";
});
```

## Encryption Configuration

### SecurityOptions (Encryption)

```csharp
public class SecurityOptions
{
    // Enable encryption
    // Default: false
    public bool EnableEncryption { get; set; } = false;
    
    // Encryption algorithm
    // Options: AES256, AES128
    // Default: AES256
    public string EncryptionAlgorithm { get; set; } = "AES256";
    
    // Key provider type
    // Options: EnvironmentVariable, AzureKeyVault, Custom
    public KeyProviderType KeyProvider { get; set; } = KeyProviderType.EnvironmentVariable;
    
    // Environment variable name for key (when using EnvironmentVariable provider)
    public string KeyEnvironmentVariable { get; set; } = "MESSAGE_ENCRYPTION_KEY";
    
    // Azure Key Vault URL (when using AzureKeyVault provider)
    public string? KeyVaultUrl { get; set; }
    
    // Key name in Key Vault
    public string? KeyName { get; set; }
    
    // Enable key rotation
    // Default: true
    public bool EnableKeyRotation { get; set; } = true;
    
    // Key rotation grace period
    // Default: 24 hours
    public TimeSpan KeyRotationGracePeriod { get; set; } = TimeSpan.FromHours(24);
}
```

### Configuration Example

**Environment Variable:**

```csharp
builder.Services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.EncryptionAlgorithm = "AES256";
    options.KeyProvider = KeyProviderType.EnvironmentVariable;
    options.KeyEnvironmentVariable = "MESSAGE_ENCRYPTION_KEY";
    options.EnableKeyRotation = true;
    options.KeyRotationGracePeriod = TimeSpan.FromHours(48);
});
```

**Azure Key Vault:**

```csharp
builder.Services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.EncryptionAlgorithm = "AES256";
    options.KeyProvider = KeyProviderType.AzureKeyVault;
    options.KeyVaultUrl = "https://myvault.vault.azure.net/";
    options.KeyName = "message-encryption-key";
    options.EnableKeyRotation = true;
});
```

## Authentication Configuration

### AuthenticationOptions

```csharp
public class AuthenticationOptions
{
    // Enable authentication
    // Default: false
    public bool EnableAuthentication { get; set; } = false;
    
    // JWT issuer
    public string? JwtIssuer { get; set; }
    
    // JWT audience
    public string? JwtAudience { get; set; }
    
    // JWT signing key
    public string? JwtSigningKey { get; set; }
    
    // Identity provider URL
    public string? IdentityProviderUrl { get; set; }
    
    // Token cache TTL
    // Default: 5 minutes
    public TimeSpan TokenCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
    
    // Enable authorization
    // Default: false
    public bool EnableAuthorization { get; set; } = false;
    
    // Role-based permissions
    public Dictionary<string, string[]> Roles { get; set; } = new();
}
```

### Configuration Example

```csharp
builder.Services.AddMessageBrokerSecurity(options =>
{
    options.EnableAuthentication = true;
    options.JwtIssuer = "https://auth.example.com";
    options.JwtAudience = "message-broker";
    options.JwtSigningKey = builder.Configuration["Jwt:SigningKey"];
    options.TokenCacheTtl = TimeSpan.FromMinutes(10);
    
    options.EnableAuthorization = true;
    options.Roles = new Dictionary<string, string[]>
    {
        ["admin"] = new[] { "publish:*", "subscribe:*", "manage:*" },
        ["publisher"] = new[] { "publish:orders.*", "publish:inventory.*" },
        ["consumer"] = new[] { "subscribe:orders.*", "subscribe:inventory.*" },
        ["readonly"] = new[] { "subscribe:*" }
    };
});
```

## Rate Limiting Configuration

### RateLimitOptions

```csharp
public class RateLimitOptions
{
    // Enable rate limiting
    // Default: false
    public bool Enabled { get; set; } = false;
    
    // Requests per second
    // Default: 1000
    public int RequestsPerSecond { get; set; } = 1000;
    
    // Rate limiting strategy
    // Options: FixedWindow, SlidingWindow, TokenBucket
    // Default: TokenBucket
    public RateLimitStrategy Strategy { get; set; } = RateLimitStrategy.TokenBucket;
    
    // Enable per-tenant rate limits
    // Default: false
    public bool EnablePerTenantLimits { get; set; } = false;
    
    // Per-tenant rate limits
    public Dictionary<string, int> TenantLimits { get; set; } = new();
    
    // Default tenant limit (when not specified)
    public int DefaultTenantLimit { get; set; } = 100;
    
    // Burst size (for TokenBucket strategy)
    public int BurstSize { get; set; } = 100;
}
```

### Configuration Example

```csharp
builder.Services.AddRateLimiting(options =>
{
    options.Enabled = true;
    options.RequestsPerSecond = 5000;
    options.Strategy = RateLimitStrategy.TokenBucket;
    options.BurstSize = 500;
    
    options.EnablePerTenantLimits = true;
    options.TenantLimits = new Dictionary<string, int>
    {
        ["tenant-premium"] = 10000,
        ["tenant-standard"] = 1000,
        ["tenant-basic"] = 100
    };
    options.DefaultTenantLimit = 50;
});
```

## Bulkhead Configuration

### BulkheadOptions

```csharp
public class BulkheadOptions
{
    // Enable bulkhead pattern
    // Default: false
    public bool Enabled { get; set; } = false;
    
    // Maximum concurrent operations
    // Default: 100
    public int MaxConcurrentOperations { get; set; } = 100;
    
    // Maximum queued operations
    // Default: 1000
    public int MaxQueuedOperations { get; set; } = 1000;
    
    // Queue timeout
    // Default: 30 seconds
    public TimeSpan QueueTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### Configuration Example

```csharp
builder.Services.AddBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 200;
    options.MaxQueuedOperations = 2000;
    options.QueueTimeout = TimeSpan.FromSeconds(60);
});
```

## Poison Message Configuration

### PoisonMessageOptions

```csharp
public class PoisonMessageOptions
{
    // Enable poison message handling
    // Default: false
    public bool Enabled { get; set; } = false;
    
    // Failure threshold before marking as poison
    // Default: 5
    public int FailureThreshold { get; set; } = 5;
    
    // Retention period for poison messages
    // Default: 7 days
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);
    
    // Enable automatic reprocessing
    // Default: false
    public bool EnableAutoReprocess { get; set; } = false;
    
    // Reprocess delay
    // Default: 1 hour
    public TimeSpan ReprocessDelay { get; set; } = TimeSpan.FromHours(1);
}
```

### Configuration Example

```csharp
builder.Services.AddPoisonMessageHandling(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 3;
    options.RetentionPeriod = TimeSpan.FromDays(14);
    options.EnableAutoReprocess = false;
});
```

## Backpressure Configuration

### BackpressureOptions

```csharp
public class BackpressureOptions
{
    // Enable backpressure management
    // Default: false
    public bool Enabled { get; set; } = false;
    
    // Latency threshold for triggering backpressure
    // Default: 5 seconds
    public TimeSpan LatencyThreshold { get; set; } = TimeSpan.FromSeconds(5);
    
    // Queue depth threshold
    // Default: 10000
    public int QueueDepthThreshold { get; set; } = 10000;
    
    // Recovery latency threshold
    // Default: 2 seconds
    public TimeSpan RecoveryLatencyThreshold { get; set; } = TimeSpan.FromSeconds(2);
    
    // Throttle percentage (0.0 - 1.0)
    // Default: 0.5 (50% reduction)
    public double ThrottlePercentage { get; set; } = 0.5;
}
```

### Configuration Example

```csharp
builder.Services.AddBackpressure(options =>
{
    options.Enabled = true;
    options.LatencyThreshold = TimeSpan.FromSeconds(10);
    options.QueueDepthThreshold = 20000;
    options.RecoveryLatencyThreshold = TimeSpan.FromSeconds(3);
    options.ThrottlePercentage = 0.3; // 30% reduction
});
```

## Configuration Profiles

### Development Profile

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddMessageBrokerEnhancements(options =>
    {
        // Outbox - In-memory for fast development
        options.Outbox.Enabled = true;
        options.Outbox.PollingInterval = TimeSpan.FromSeconds(1);
        
        // Inbox - In-memory
        options.Inbox.Enabled = true;
        
        // Metrics - Console exporter
        options.Metrics.EnableMetrics = true;
        options.Metrics.EnablePrometheusExporter = false;
        
        // Tracing - 100% sampling
        options.Tracing.EnableTracing = true;
        options.Tracing.SamplingRate = 1.0;
        options.Tracing.CaptureMessagePayloads = true; // OK in dev
        
        // Security - Disabled
        options.Security.EnableEncryption = false;
        options.Security.EnableAuthentication = false;
        
        // Rate Limiting - Disabled
        options.RateLimit.Enabled = false;
    });
}
```

### Production Profile

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddMessageBrokerEnhancements(options =>
    {
        // Outbox - SQL Server with optimized settings
        options.Outbox.Enabled = true;
        options.Outbox.PollingInterval = TimeSpan.FromSeconds(5);
        options.Outbox.BatchSize = 200;
        options.Outbox.MaxRetryAttempts = 5;
        
        // Inbox - SQL Server
        options.Inbox.Enabled = true;
        options.Inbox.RetentionPeriod = TimeSpan.FromDays(30);
        
        // Connection Pool - Optimized
        options.ConnectionPool.MinPoolSize = 20;
        options.ConnectionPool.MaxPoolSize = 200;
        
        // Batch Processing - Enabled
        options.Batch.Enabled = true;
        options.Batch.MaxBatchSize = 500;
        options.Batch.EnableCompression = true;
        
        // Deduplication - Enabled
        options.Deduplication.Enabled = true;
        options.Deduplication.Window = TimeSpan.FromMinutes(10);
        
        // Metrics - Prometheus
        options.Metrics.EnableMetrics = true;
        options.Metrics.EnablePrometheusExporter = true;
        
        // Tracing - 10% sampling
        options.Tracing.EnableTracing = true;
        options.Tracing.SamplingRate = 0.1;
        options.Tracing.CaptureMessagePayloads = false; // Security
        
        // Security - Enabled
        options.Security.EnableEncryption = true;
        options.Security.KeyProvider = KeyProviderType.AzureKeyVault;
        options.Security.EnableAuthentication = true;
        
        // Rate Limiting - Enabled
        options.RateLimit.Enabled = true;
        options.RateLimit.RequestsPerSecond = 10000;
        options.RateLimit.EnablePerTenantLimits = true;
        
        // Bulkhead - Enabled
        options.Bulkhead.Enabled = true;
        options.Bulkhead.MaxConcurrentOperations = 500;
        
        // Poison Messages - Enabled
        options.PoisonMessage.Enabled = true;
        options.PoisonMessage.FailureThreshold = 5;
        
        // Backpressure - Enabled
        options.Backpressure.Enabled = true;
    });
}
```

## Environment Variables

All configuration options can be overridden using environment variables:

```bash
# Outbox
MESSAGEBROKER__OUTBOX__ENABLED=true
MESSAGEBROKER__OUTBOX__POLLINGINTERVAL=00:00:05
MESSAGEBROKER__OUTBOX__BATCHSIZE=100

# Encryption
MESSAGEBROKER__SECURITY__ENABLEENCRYPTION=true
MESSAGE_ENCRYPTION_KEY=your-base64-encoded-key

# Rate Limiting
MESSAGEBROKER__RATELIMIT__ENABLED=true
MESSAGEBROKER__RATELIMIT__REQUESTSPERSECOND=1000

# Tracing
MESSAGEBROKER__TRACING__SERVICENAME=OrderService
MESSAGEBROKER__TRACING__SAMPLINGRATE=0.1
```

## Configuration Validation

Enable configuration validation to catch errors at startup:

```csharp
builder.Services.AddOptions<OutboxOptions>()
    .Bind(builder.Configuration.GetSection("MessageBroker:Outbox"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Next Steps

- [Best Practices](./BEST_PRACTICES.md) - Production deployment guidelines
- [Troubleshooting](./TROUBLESHOOTING.md) - Common configuration issues
- [Examples](./examples/) - Complete configuration examples
