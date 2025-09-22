# Rate Limiting Pipeline Behavior

Relay provides built-in support for rate limiting through pipeline behaviors. This feature allows you to protect your handlers from excessive requests and prevent abuse.

## 🚀 Quick Start

### 1. Enable Rate Limiting

To enable rate limiting, call `AddRelayRateLimiting()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayRateLimiting(); // Enable rate limiting
```

### 2. Define Rate Limited Requests

Mark requests as rate limited by applying the `RateLimitAttribute`:

```csharp
[RateLimit(100, 60, "User")] // 100 requests per minute per user
public record GetUserRequest(int UserId) : IRequest<User>;
```

### 3. Use Rate Limiting

Rate limiting happens automatically when you send requests:

```csharp
try
{
    var request = new GetUserRequest(123);
    var user = await relay.SendAsync(request); // May throw RateLimitExceededException
}
catch (RateLimitExceededException ex)
{
    Console.WriteLine($"Rate limit exceeded. Retry after {ex.RetryAfter.TotalSeconds} seconds.");
}
```

## 🎯 Key Features

### Rate Limit Attribute

The `RateLimitAttribute` enables rate limiting for specific request types:

```csharp
[RateLimit(1000, 3600, "Global")] // 1000 requests per hour globally
public record ExpensiveRequest(int Id) : IRequest<ExpensiveResult>;
```

### Multiple Rate Limiting Strategies

Support for different rate limiting keys:

- **Global**: Rate limit across all requests
- **User**: Rate limit per user
- **IP**: Rate limit per IP address
- **Type**: Rate limit per request type

### In-Memory Rate Limiter

Built-in in-memory rate limiter with sliding window algorithm:

```csharp
services.AddTransient<IRateLimiter, InMemoryRateLimiter>();
```

### Custom Rate Limiters

Implement custom rate limiters by implementing the `IRateLimiter` interface:

```csharp
public class RedisRateLimiter : IRateLimiter
{
    public async ValueTask<bool> IsAllowedAsync(string key, CancellationToken cancellationToken = default)
    {
        // Custom implementation
    }
    
    public async ValueTask<TimeSpan> GetRetryAfterAsync(string key, CancellationToken cancellationToken = default)
    {
        // Custom implementation
    }
}
```

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure rate limiting options for specific handlers:

```csharp
services.ConfigureRateLimiting<GetUserRequest>(options =>
{
    options.DefaultRequestsPerWindow = 200;
    options.DefaultWindowSeconds = 60;
    options.DefaultKey = "User";
});
```

### Global Rate Limiting

Enable automatic rate limiting for all requests:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultRateLimitingOptions.EnableAutomaticRateLimiting = true;
    options.DefaultRateLimitingOptions.DefaultRequestsPerWindow = 1000;
    options.DefaultRateLimitingOptions.DefaultWindowSeconds = 3600;
});
```

### Exception Handling

Configure whether to throw exceptions when rate limits are exceeded:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultRateLimitingOptions.ThrowOnRateLimitExceeded = false;
});
```

## ⚡ Performance

Rate limiting is designed to be lightweight and efficient:

- **In-Memory**: Fast, in-process rate limiting with minimal overhead
- **Sliding Window**: Efficient sliding window algorithm to prevent bursts
- **Early Execution**: Rate limiting pipeline behavior runs early in the pipeline to fail fast

## 🧪 Testing

Rate limited requests can be tested by exceeding the rate limit and catching the `RateLimitExceededException`.