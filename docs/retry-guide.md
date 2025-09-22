# Retry Pipeline Behavior

Relay provides built-in support for retry logic through pipeline behaviors. This feature allows you to automatically retry failed requests with various retry strategies.

## 🚀 Quick Start

### 1. Enable Retry

To enable retry behavior, call `AddRelayRetry()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayRetry(); // Enable retry behavior
```

### 2. Define Requests with Retry

Mark requests as requiring retry logic by applying the `RetryAttribute`:

```csharp
[Retry(3, 1000)] // Retry 3 times with 1 second delay
public record GetUserRequest(int UserId) : IRequest<User>;
```

### 3. Use Retry

Retry happens automatically when you send requests:

```csharp
try
{
    var request = new GetUserRequest(123);
    var user = await relay.SendAsync(request); // Will retry up to 3 times if it fails
}
catch (RetryExhaustedException ex)
{
    Console.WriteLine($"All retry attempts exhausted: {ex.Exceptions.Count} attempts were made.");
}
```

## 🎯 Key Features

### Retry Attribute

The `RetryAttribute` enables retry logic for specific request types:

```csharp
[Retry(5, 2000)] // Retry 5 times with 2 second delay
public record ExpensiveRequest(int Id) : IRequest<ExpensiveResult>;

[Retry(typeof(ExponentialBackoffRetryStrategy), 3)] // Use exponential backoff strategy
public record NetworkRequest(string Url) : IRequest<string>;
```

### Retry Strategies

Relay provides several built-in retry strategies:

1. **Linear Retry Strategy** - Fixed delay between attempts
2. **Exponential Backoff Retry Strategy** - Increasing delay with optional jitter
3. **Circuit Breaker Retry Strategy** - Stops retrying after consecutive failures

### Custom Retry Strategies

Implement custom retry strategies by implementing the `IRetryStrategy` interface:

```csharp
public class CustomRetryStrategy : IRetryStrategy
{
    public async ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
    {
        // Custom logic to determine if retry should be attempted
        return exception is TimeoutException;
    }
    
    public async ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
    {
        // Custom logic to determine delay
        return TimeSpan.FromSeconds(attempt);
    }
}
```

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure retry options for specific handlers:

```csharp
services.ConfigureRetry<GetUserRequest>(options =>
{
    options.DefaultMaxRetryAttempts = 5;
    options.DefaultRetryDelayMilliseconds = 2000;
});
```

### Global Retry

Enable automatic retry for all requests:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultRetryOptions.EnableAutomaticRetry = true;
    options.DefaultRetryOptions.DefaultMaxRetryAttempts = 3;
});
```

### Exception Handling

Configure whether to throw exceptions when all retry attempts are exhausted:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultRetryOptions.ThrowOnRetryExhausted = false;
});
```

## ⚡ Performance

Retry behavior is designed to be lightweight and efficient:

- **Configurable**: Only executes when retry is enabled
- **Flexible Strategies**: Supports various retry patterns
- **Async Support**: Fully asynchronous implementation
- **Early Exit**: Stops retrying when appropriate

## 🧪 Testing

Requests with retry can be tested by simulating failures and verifying retry behavior.