# Rate Limiting

The Rate Limiting feature provides configurable rate limiting capabilities for message publishing operations. It helps prevent resource exhaustion and ensures fair usage across tenants or users.

## Features

- **Multiple Strategies**: Token Bucket and Sliding Window rate limiting algorithms
- **Per-Tenant Limiting**: Configure different rate limits for different tenants
- **Burst Handling**: Token Bucket strategy allows controlled bursts while maintaining average rate
- **Automatic Cleanup**: Memory-efficient with automatic cleanup of expired entries
- **Comprehensive Metrics**: Track request rates, rejections, and active keys
- **Graceful Degradation**: Clear error messages with retry-after information

## Rate Limiting Strategies

### Token Bucket
The Token Bucket algorithm allows for burst traffic while maintaining an average rate. Tokens are added to a bucket at a constant rate, and each request consumes a token. If the bucket is empty, the request is rejected.

**Advantages:**
- Allows controlled bursts
- Smooth rate limiting
- Good for variable traffic patterns

**Configuration:**
- `RequestsPerSecond`: The rate at which tokens are added
- `BucketCapacity`: Maximum tokens (allows bursts)

### Sliding Window
The Sliding Window algorithm tracks requests in a rolling time window. It provides more accurate rate limiting than fixed windows by considering the exact timing of requests.

**Advantages:**
- More accurate than fixed windows
- No boundary issues
- Predictable behavior

**Configuration:**
- `RequestsPerSecond`: Maximum requests in the window
- `WindowSize`: Duration of the sliding window

## Configuration

### Basic Rate Limiting

```csharp
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.TokenBucket;
    options.RequestsPerSecond = 1000;
    options.BucketCapacity = 2000; // Allow bursts up to 2x
});

// Decorate the message broker
services.DecorateMessageBrokerWithRateLimit();
```

### Per-Tenant Rate Limiting

```csharp
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

### Sliding Window Strategy

```csharp
services.AddMessageBrokerRateLimit(options =>
{
    options.Enabled = true;
    options.Strategy = RateLimitStrategy.SlidingWindow;
    options.RequestsPerSecond = 1000;
    options.WindowSize = TimeSpan.FromSeconds(1);
});

services.DecorateMessageBrokerWithRateLimit();
```

## Usage

### Publishing with Rate Limiting

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
        try
        {
            var options = new PublishOptions
            {
                Headers = new Dictionary<string, object>
                {
                    ["TenantId"] = order.TenantId
                }
            };

            await _messageBroker.PublishAsync(order, options);
        }
        catch (RateLimitExceededException ex)
        {
            // Handle rate limit exceeded
            Console.WriteLine($"Rate limit exceeded. Retry after: {ex.RetryAfter}");
            Console.WriteLine($"Reset at: {ex.ResetAt}");
            
            // Implement retry logic or return error to client
            throw;
        }
    }
}
```

### Tenant ID Extraction

The rate limiter automatically extracts tenant IDs from:

1. **Explicit Headers**: `TenantId`, `X-Tenant-Id`, `X-Tenant`
2. **JWT Claims**: `tenant_id` or `tid` claims in the Authorization header
3. **Default**: Falls back to "default" if no tenant ID is found

```csharp
// Option 1: Explicit tenant header
var options = new PublishOptions
{
    Headers = new Dictionary<string, object>
    {
        ["TenantId"] = "tenant-123"
    }
};

// Option 2: JWT token with tenant claim
var options = new PublishOptions
{
    Headers = new Dictionary<string, object>
    {
        ["Authorization"] = "Bearer eyJ...token-with-tenant-claim"
    }
};
```

## Metrics

Get rate limiter metrics to monitor performance:

```csharp
public class RateLimitMonitor
{
    private readonly RateLimitMessageBrokerDecorator _decorator;

    public void LogMetrics()
    {
        var metrics = _decorator.GetMetrics();
        
        Console.WriteLine($"Total Requests: {metrics.TotalRequests}");
        Console.WriteLine($"Allowed Requests: {metrics.AllowedRequests}");
        Console.WriteLine($"Rejected Requests: {metrics.RejectedRequests}");
        Console.WriteLine($"Rejection Rate: {metrics.RejectionRate:F2}%");
        Console.WriteLine($"Current Rate: {metrics.CurrentRate:F2} req/s");
        Console.WriteLine($"Active Keys: {metrics.ActiveKeys}");
    }
}
```

## Error Handling

### RateLimitExceededException

When a rate limit is exceeded, a `RateLimitExceededException` is thrown with:

- **Message**: Description of the rate limit violation
- **RetryAfter**: Duration to wait before retrying
- **ResetAt**: Time when the rate limit window resets

```csharp
try
{
    await _messageBroker.PublishAsync(message);
}
catch (RateLimitExceededException ex)
{
    // Log the error
    _logger.LogWarning(
        "Rate limit exceeded. Retry after {RetryAfter}s, Reset at {ResetAt}",
        ex.RetryAfter.TotalSeconds,
        ex.ResetAt);

    // Implement exponential backoff
    await Task.Delay(ex.RetryAfter);
    
    // Retry the operation
    await _messageBroker.PublishAsync(message);
}
```

## Best Practices

1. **Choose the Right Strategy**
   - Use Token Bucket for variable traffic with bursts
   - Use Sliding Window for strict rate enforcement

2. **Set Appropriate Limits**
   - Consider your system capacity
   - Leave headroom for traffic spikes
   - Monitor metrics to adjust limits

3. **Handle Rate Limit Errors**
   - Implement retry logic with exponential backoff
   - Return meaningful errors to clients
   - Log rate limit violations for monitoring

4. **Per-Tenant Configuration**
   - Set different limits based on subscription tiers
   - Use default limits for unknown tenants
   - Monitor per-tenant usage

5. **Cleanup Configuration**
   - Adjust cleanup interval based on traffic patterns
   - Balance memory usage vs. cleanup overhead

## Performance Considerations

- **Memory Usage**: Each active key maintains state (bucket or window)
- **Cleanup**: Automatic cleanup runs periodically to remove inactive keys
- **Thread Safety**: All implementations are thread-safe using appropriate locking
- **Overhead**: Minimal overhead (< 1ms per request)

## Integration with Other Features

Rate limiting works seamlessly with other MessageBroker features:

- **Security**: Rate limits can be applied per authenticated user/tenant
- **Metrics**: Rate limit metrics are exposed for monitoring
- **Health Checks**: Rate limiter health can be monitored
- **Distributed Tracing**: Rate limit checks are traced

## Troubleshooting

### High Rejection Rate

If you're seeing a high rejection rate:

1. Check if limits are too low for your traffic
2. Verify tenant ID extraction is working correctly
3. Monitor traffic patterns for unexpected spikes
4. Consider increasing bucket capacity for bursts

### Memory Growth

If memory usage is growing:

1. Reduce cleanup interval for more frequent cleanup
2. Check for tenant ID extraction issues (too many unique keys)
3. Monitor active keys metric
4. Consider implementing key expiration

### Inconsistent Rate Limiting

If rate limiting seems inconsistent:

1. Verify tenant ID extraction is consistent
2. Check for clock skew in distributed systems
3. Ensure cleanup interval is appropriate
4. Monitor metrics for anomalies

## See Also

- [EXAMPLE.md](EXAMPLE.md) - Complete usage examples
- [Security Documentation](../Security/README.md) - Authentication and authorization
- [Metrics Documentation](../Metrics/README.md) - Monitoring and observability
