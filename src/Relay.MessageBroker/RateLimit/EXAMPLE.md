# Rate Limiting Examples

This document provides comprehensive examples of using the Rate Limiting feature in Relay.MessageBroker.

## Table of Contents

- [Basic Setup](#basic-setup)
- [Token Bucket Strategy](#token-bucket-strategy)
- [Sliding Window Strategy](#sliding-window-strategy)
- [Per-Tenant Rate Limiting](#per-tenant-rate-limiting)
- [Error Handling](#error-handling)
- [Monitoring and Metrics](#monitoring-and-metrics)
- [Advanced Scenarios](#advanced-scenarios)

## Basic Setup

### Simple Rate Limiting

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.RateLimit;

var services = new ServiceCollection();

// Register RabbitMQ message broker
services.AddRabbitMQMessageBroker(options =>
{
    options.HostName = "localhost";
    options.QueueName = "orders";
});

// Add rate limiting (1000 requests per second)
services.AddMessageBrokerRateLimit(1000);

// Decorate the broker with rate limiting
services.DecorateMessageBrokerWithRateLimit();

var serviceProvider = services.BuildServiceProvider();
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
```

### Custom Configuration

```csharp
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.TokenBucket;
    options.RequestsPerSecond = 1000;
    options.BucketCapacity = 2000; // Allow bursts
    options.CleanupInterval = TimeSpan.FromMinutes(5);
});

services.DecorateMessageBrokerWithRateLimit();
```

## Token Bucket Strategy

### Basic Token Bucket

```csharp
// Configure token bucket with burst handling
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.TokenBucket;
    options.RequestsPerSecond = 100;
    options.BucketCapacity = 200; // Allow bursts up to 2x
});

services.DecorateMessageBrokerWithRateLimit();

// Usage
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();

// This will allow a burst of 200 messages initially
for (int i = 0; i < 200; i++)
{
    await messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = i });
}

// After the burst, rate is limited to 100 messages per second
```

### High-Throughput Configuration

```csharp
// For high-throughput scenarios
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.TokenBucket;
    options.RequestsPerSecond = 10000;
    options.BucketCapacity = 20000;
    options.CleanupInterval = TimeSpan.FromMinutes(1);
});

services.DecorateMessageBrokerWithRateLimit();
```

## Sliding Window Strategy

### Basic Sliding Window

```csharp
// Configure sliding window for strict rate enforcement
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.SlidingWindow;
    options.RequestsPerSecond = 100;
    options.WindowSize = TimeSpan.FromSeconds(1);
});

services.DecorateMessageBrokerWithRateLimit();

// Usage - strictly enforces 100 requests per second
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();

for (int i = 0; i < 150; i++)
{
    try
    {
        await messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = i });
    }
    catch (RateLimitExceededException ex)
    {
        Console.WriteLine($"Rate limit hit at message {i}");
        await Task.Delay(ex.RetryAfter);
        i--; // Retry this message
    }
}
```

### Custom Window Size

```csharp
// Use a larger window for smoother rate limiting
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.SlidingWindow;
    options.RequestsPerSecond = 1000;
    options.WindowSize = TimeSpan.FromSeconds(10); // 10-second window
});

services.DecorateMessageBrokerWithRateLimit();
```

## Per-Tenant Rate Limiting

### Basic Per-Tenant Setup

```csharp
// Configure different limits for different tenants
services.AddMessageBrokerPerTenantRateLimit(
    defaultTenantLimit: 100,
    tenantLimits: new Dictionary<string, int>
    {
        ["premium-tenant"] = 1000,
        ["standard-tenant"] = 500,
        ["basic-tenant"] = 100
    });

services.DecorateMessageBrokerWithRateLimit();
```

### Publishing with Tenant ID

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;

    public OrderService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public async Task CreateOrderAsync(Order order)
    {
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["TenantId"] = order.TenantId
            }
        };

        await _messageBroker.PublishAsync(
            new OrderCreatedEvent { Order = order },
            options);
    }
}
```

### Multi-Tenant Application

```csharp
public class MultiTenantOrderProcessor
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<MultiTenantOrderProcessor> _logger;

    public MultiTenantOrderProcessor(
        IMessageBroker messageBroker,
        ILogger<MultiTenantOrderProcessor> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task ProcessOrdersAsync(List<Order> orders)
    {
        // Group orders by tenant
        var ordersByTenant = orders.GroupBy(o => o.TenantId);

        foreach (var tenantOrders in ordersByTenant)
        {
            var tenantId = tenantOrders.Key;
            _logger.LogInformation(
                "Processing {Count} orders for tenant {TenantId}",
                tenantOrders.Count(),
                tenantId);

            foreach (var order in tenantOrders)
            {
                try
                {
                    var options = new PublishOptions
                    {
                        Headers = new Dictionary<string, object>
                        {
                            ["TenantId"] = tenantId
                        }
                    };

                    await _messageBroker.PublishAsync(
                        new OrderCreatedEvent { Order = order },
                        options);
                }
                catch (RateLimitExceededException ex)
                {
                    _logger.LogWarning(
                        "Rate limit exceeded for tenant {TenantId}. Retry after {RetryAfter}",
                        tenantId,
                        ex.RetryAfter);

                    // Implement tenant-specific retry logic
                    await Task.Delay(ex.RetryAfter);
                    // Retry or queue for later processing
                }
            }
        }
    }
}
```

## Error Handling

### Basic Error Handling

```csharp
public async Task PublishWithRetryAsync<TMessage>(TMessage message)
{
    const int maxRetries = 3;
    int retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            await _messageBroker.PublishAsync(message);
            return; // Success
        }
        catch (RateLimitExceededException ex)
        {
            retryCount++;
            
            if (retryCount >= maxRetries)
            {
                _logger.LogError(
                    "Failed to publish message after {MaxRetries} retries",
                    maxRetries);
                throw;
            }

            _logger.LogWarning(
                "Rate limit exceeded. Retry {RetryCount}/{MaxRetries} after {RetryAfter}",
                retryCount,
                maxRetries,
                ex.RetryAfter);

            await Task.Delay(ex.RetryAfter);
        }
    }
}
```

### Exponential Backoff

```csharp
public async Task PublishWithExponentialBackoffAsync<TMessage>(TMessage message)
{
    const int maxRetries = 5;
    int retryCount = 0;
    TimeSpan delay = TimeSpan.FromMilliseconds(100);

    while (retryCount < maxRetries)
    {
        try
        {
            await _messageBroker.PublishAsync(message);
            return;
        }
        catch (RateLimitExceededException ex)
        {
            retryCount++;
            
            if (retryCount >= maxRetries)
            {
                throw;
            }

            // Use the larger of: suggested retry-after or exponential backoff
            var backoffDelay = TimeSpan.FromMilliseconds(
                Math.Pow(2, retryCount) * 100);
            var actualDelay = ex.RetryAfter > backoffDelay 
                ? ex.RetryAfter 
                : backoffDelay;

            _logger.LogWarning(
                "Rate limit exceeded. Waiting {Delay}ms before retry {RetryCount}",
                actualDelay.TotalMilliseconds,
                retryCount);

            await Task.Delay(actualDelay);
        }
    }
}
```

### Circuit Breaker Pattern

```csharp
public class RateLimitCircuitBreaker
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger _logger;
    private int _consecutiveFailures;
    private DateTimeOffset _circuitOpenedAt;
    private bool _circuitOpen;
    private readonly int _failureThreshold = 5;
    private readonly TimeSpan _circuitResetTimeout = TimeSpan.FromMinutes(1);

    public async Task PublishAsync<TMessage>(TMessage message)
    {
        // Check if circuit is open
        if (_circuitOpen)
        {
            if (DateTimeOffset.UtcNow - _circuitOpenedAt > _circuitResetTimeout)
            {
                _logger.LogInformation("Circuit breaker reset, attempting to close");
                _circuitOpen = false;
                _consecutiveFailures = 0;
            }
            else
            {
                throw new InvalidOperationException(
                    "Circuit breaker is open due to rate limiting");
            }
        }

        try
        {
            await _messageBroker.PublishAsync(message);
            _consecutiveFailures = 0; // Reset on success
        }
        catch (RateLimitExceededException ex)
        {
            _consecutiveFailures++;

            if (_consecutiveFailures >= _failureThreshold)
            {
                _circuitOpen = true;
                _circuitOpenedAt = DateTimeOffset.UtcNow;
                _logger.LogError(
                    "Circuit breaker opened after {Failures} consecutive rate limit failures",
                    _consecutiveFailures);
            }

            throw;
        }
    }
}
```

## Monitoring and Metrics

### Basic Metrics Collection

```csharp
public class RateLimitMetricsCollector : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RateLimitMetricsCollector> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var messageBroker = scope.ServiceProvider
                    .GetRequiredService<IMessageBroker>();

                if (messageBroker is RateLimitMessageBrokerDecorator decorator)
                {
                    var metrics = decorator.GetMetrics();

                    _logger.LogInformation(
                        "Rate Limit Metrics - Total: {Total}, Allowed: {Allowed}, " +
                        "Rejected: {Rejected}, Rate: {Rate:F2} req/s, " +
                        "Rejection Rate: {RejectionRate:F2}%, Active Keys: {ActiveKeys}",
                        metrics.TotalRequests,
                        metrics.AllowedRequests,
                        metrics.RejectedRequests,
                        metrics.CurrentRate,
                        metrics.RejectionRate,
                        metrics.ActiveKeys);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting rate limit metrics");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

### Prometheus Metrics Export

```csharp
public class RateLimitPrometheusExporter
{
    private readonly RateLimitMessageBrokerDecorator _decorator;
    private readonly Counter _totalRequestsCounter;
    private readonly Counter _rejectedRequestsCounter;
    private readonly Gauge _activeKeysGauge;
    private readonly Gauge _rejectionRateGauge;

    public RateLimitPrometheusExporter(RateLimitMessageBrokerDecorator decorator)
    {
        _decorator = decorator;

        _totalRequestsCounter = Metrics.CreateCounter(
            "messagebroker_ratelimit_requests_total",
            "Total number of rate limit checks");

        _rejectedRequestsCounter = Metrics.CreateCounter(
            "messagebroker_ratelimit_rejected_total",
            "Total number of rejected requests");

        _activeKeysGauge = Metrics.CreateGauge(
            "messagebroker_ratelimit_active_keys",
            "Number of active rate limit keys");

        _rejectionRateGauge = Metrics.CreateGauge(
            "messagebroker_ratelimit_rejection_rate",
            "Percentage of requests rejected");
    }

    public void UpdateMetrics()
    {
        var metrics = _decorator.GetMetrics();

        _totalRequestsCounter.IncTo(metrics.TotalRequests);
        _rejectedRequestsCounter.IncTo(metrics.RejectedRequests);
        _activeKeysGauge.Set(metrics.ActiveKeys);
        _rejectionRateGauge.Set(metrics.RejectionRate);
    }
}
```

## Advanced Scenarios

### Dynamic Rate Limit Adjustment

```csharp
public class DynamicRateLimitManager
{
    private readonly IServiceCollection _services;
    private RateLimitOptions _currentOptions;

    public void AdjustRateLimit(int newLimit)
    {
        _services.Configure<RateLimitOptions>(options =>
        {
            options.RequestsPerSecond = newLimit;
            
            // Adjust bucket capacity proportionally
            if (options.BucketCapacity.HasValue)
            {
                var ratio = (double)newLimit / options.RequestsPerSecond;
                options.BucketCapacity = (int)(options.BucketCapacity.Value * ratio);
            }
        });

        Console.WriteLine($"Rate limit adjusted to {newLimit} requests per second");
    }

    public void AdjustTenantLimit(string tenantId, int newLimit)
    {
        _services.Configure<RateLimitOptions>(options =>
        {
            options.TenantLimits ??= new Dictionary<string, int>();
            options.TenantLimits[tenantId] = newLimit;
        });

        Console.WriteLine(
            $"Rate limit for tenant {tenantId} adjusted to {newLimit} requests per second");
    }
}
```

### Batch Publishing with Rate Limiting

```csharp
public class BatchPublisher
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<BatchPublisher> _logger;

    public async Task PublishBatchAsync<TMessage>(
        IEnumerable<TMessage> messages,
        string? tenantId = null)
    {
        var messageList = messages.ToList();
        var successCount = 0;
        var failureCount = 0;

        _logger.LogInformation(
            "Publishing batch of {Count} messages",
            messageList.Count);

        foreach (var message in messageList)
        {
            try
            {
                var options = tenantId != null
                    ? new PublishOptions
                    {
                        Headers = new Dictionary<string, object>
                        {
                            ["TenantId"] = tenantId
                        }
                    }
                    : null;

                await _messageBroker.PublishAsync(message, options);
                successCount++;
            }
            catch (RateLimitExceededException ex)
            {
                failureCount++;
                _logger.LogWarning(
                    "Rate limit exceeded during batch publish. " +
                    "Waiting {RetryAfter} before continuing",
                    ex.RetryAfter);

                await Task.Delay(ex.RetryAfter);
                
                // Retry this message
                try
                {
                    await _messageBroker.PublishAsync(message);
                    successCount++;
                    failureCount--;
                }
                catch
                {
                    _logger.LogError("Failed to publish message after retry");
                }
            }
        }

        _logger.LogInformation(
            "Batch publish completed. Success: {Success}, Failures: {Failures}",
            successCount,
            failureCount);
    }
}
```

### Integration with ASP.NET Core

```csharp
// Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Add message broker
    services.AddRabbitMQMessageBroker(options =>
    {
        options.HostName = Configuration["RabbitMQ:HostName"];
    });

    // Add rate limiting
    services.AddMessageBrokerRateLimit(options =>
    {
        options.Enabled = true;
        options.Strategy = RateLimitStrategy.TokenBucket;
        options.RequestsPerSecond = Configuration.GetValue<int>("RateLimit:RequestsPerSecond");
        options.EnablePerTenantLimits = true;
        options.DefaultTenantLimit = Configuration.GetValue<int>("RateLimit:DefaultTenantLimit");
    });

    services.DecorateMessageBrokerWithRateLimit();
}

// Controller
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMessageBroker _messageBroker;

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order)
    {
        try
        {
            var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";

            var options = new PublishOptions
            {
                Headers = new Dictionary<string, object>
                {
                    ["TenantId"] = tenantId
                }
            };

            await _messageBroker.PublishAsync(
                new OrderCreatedEvent { Order = order },
                options);

            return Ok(new { OrderId = order.Id });
        }
        catch (RateLimitExceededException ex)
        {
            Response.Headers.Add("Retry-After", ex.RetryAfter.TotalSeconds.ToString());
            Response.Headers.Add("X-RateLimit-Reset", ex.ResetAt?.ToUnixTimeSeconds().ToString());

            return StatusCode(429, new
            {
                Error = "Rate limit exceeded",
                RetryAfter = ex.RetryAfter.TotalSeconds,
                ResetAt = ex.ResetAt
            });
        }
    }
}
```

## See Also

- [README.md](README.md) - Complete documentation
- [Security Documentation](../Security/README.md) - Authentication and authorization
- [Metrics Documentation](../Metrics/README.md) - Monitoring and observability
