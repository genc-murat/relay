# Notification Publishing Strategy Sample

This sample demonstrates configurable notification publishing strategies in Relay framework with event-driven architecture.

## Features Demonstrated

### 1. **INotificationPublisher Interface**
Pluggable strategy pattern for controlling how notifications are dispatched to handlers:
- **Sequential**: Handlers execute one at a time in priority order
- **Parallel**: Handlers execute concurrently for maximum performance
- **ParallelWhenAll**: All handlers execute even if some fail, collecting exceptions
- **Custom**: Implement your own publishing strategy

### 2. **Built-in Publishing Strategies**

#### Sequential Publisher
```csharp
services.UseSequentialNotificationPublisher();
```
- Safest option
- Handlers execute in priority order
- Stops on first exception
- Best for handlers with dependencies

#### Parallel Publisher
```csharp
services.UseParallelNotificationPublisher();
```
- Fastest option
- All handlers execute concurrently
- Stops on first exception
- Best for independent handlers

#### ParallelWhenAll Publisher
```csharp
services.UseParallelWhenAllNotificationPublisher(continueOnException: true);
```
- Most resilient option
- All handlers execute even if some fail
- Collects all exceptions
- Best for fire-and-forget scenarios

## Running the Sample

```bash
cd samples/NotificationPublishingSample
dotnet run
```

## Architecture

```
Event Published
  ↓
INotificationPublisher (Strategy)
  ├─ Sequential Strategy
  │   └─ Handler1 → Handler2 → Handler3
  ├─ Parallel Strategy
  │   ├─ Handler1
  │   ├─ Handler2 (concurrent)
  │   └─ Handler3 (concurrent)
  └─ Custom Strategy
      └─ Your implementation
```

## Configuration Examples

### 1. Using Extension Methods

```csharp
// Sequential (safe, ordered)
services.UseSequentialNotificationPublisher();

// Parallel (fast, concurrent)
services.UseParallelNotificationPublisher();

// Parallel with exception tolerance
services.UseParallelWhenAllNotificationPublisher(
    continueOnException: true);
```

### 2. Using Configuration Options

```csharp
services.ConfigureNotificationPublisher(options =>
{
    options.PublisherType = NotificationPublishingStrategy.Parallel;
    options.Lifetime = ServiceLifetime.Singleton;
});
```

### 3. Custom Publisher

```csharp
public class CustomPublisher : INotificationPublisher
{
    public async ValueTask PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<INotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken)
    {
        // Your custom publishing logic
        // Examples:
        // - Round-robin distribution
        // - Load-based routing
        // - Priority queues
        // - Rate-limited execution
    }
}

services.UseCustomNotificationPublisher<CustomPublisher>();
```

## Real-World Use Cases

### Event Sourcing
```csharp
public record UserRegisteredEvent(int UserId, string Email) : INotification;

// Multiple projections update in parallel
[Notification] public class ReadModelProjection { }
[Notification] public class AnalyticsProjection { }
[Notification] public class SearchIndexProjection { }
```

### Microservices Integration
```csharp
public record OrderPlacedEvent(int OrderId, decimal Amount) : INotification;

// Notify multiple services
[Notification] public class InventoryService { }
[Notification] public class ShippingService { }
[Notification] public class NotificationService { }
```

### Cross-Cutting Concerns
```csharp
public record EntityChangedEvent<T>(T Entity, string Action) : INotification;

// Parallel execution of independent concerns
[Notification(DispatchMode = Parallel)] public class AuditLogger { }
[Notification(DispatchMode = Parallel)] public class CacheInvalidator { }
[Notification(DispatchMode = Parallel)] public class SearchIndexUpdater { }
```

## Strategy Comparison

| Strategy | Speed | Safety | Use Case |
|----------|-------|--------|----------|
| **Sequential** | ⭐️ Slow | ⭐️⭐️⭐️ Safe | Handlers with dependencies |
| **Parallel** | ⭐️⭐️⭐️ Fast | ⭐️⭐️ Moderate | Independent handlers |
| **ParallelWhenAll** | ⭐️⭐️⭐️ Fast | ⭐️⭐️⭐️ Safe | Fire-and-forget events |

## Performance Considerations

### Sequential Performance
```
Handler1 (100ms) → Handler2 (100ms) → Handler3 (100ms)
Total: 300ms
```

### Parallel Performance
```
Handler1 (100ms) ┐
Handler2 (100ms) ├─ All concurrent
Handler3 (100ms) ┘
Total: 100ms (3x faster)
```

## Exception Handling

### Sequential Strategy
```csharp
Handler1 ✅ → Handler2 ❌ → Handler3 ⏭️ (skipped)
Exception propagates immediately
```

### Parallel Strategy
```csharp
Handler1 ✅ ┐
Handler2 ❌ ├─ First exception propagates
Handler3 ⏭️ ┘ (may not complete)
```

### ParallelWhenAll Strategy
```csharp
Handler1 ✅ ┐
Handler2 ❌ ├─ All execute
Handler3 ✅ ┘
AggregateException with all failures
```

## Best Practices

1. **Choose the Right Strategy**
   - Sequential: When order matters or handlers have dependencies
   - Parallel: When performance matters and handlers are independent
   - ParallelWhenAll: When you need guaranteed execution

2. **Handler Design**
   - Keep handlers small and focused
   - Make handlers idempotent
   - Avoid shared mutable state in parallel handlers
   - Use cancellation tokens appropriately

3. **Error Handling**
   - Log exceptions in handlers
   - Use exception actions for monitoring
   - Consider compensation strategies
   - Implement retry logic where appropriate

4. **Performance**
   - Use parallel strategies for I/O-bound handlers
   - Monitor handler execution times
   - Consider batching for high-volume events
   - Use priority-based execution when needed

## MediatR Compatibility

This implementation follows MediatR's notification publishing concepts but provides:
- Built-in strategy configuration
- More control over execution order
- Better exception handling options
- Performance optimizations

## Migration from MediatR

```csharp
// MediatR
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.NotificationPublisher = new ForeachAwaitPublisher();
});

// Relay
services.UseSequentialNotificationPublisher();
// or
services.UseParallelNotificationPublisher();
// or
services.UseParallelWhenAllNotificationPublisher();
```

## Advanced Scenarios

### Priority-Based Sequential Execution
```csharp
[Notification(Priority = 10)] // Runs first
[Notification(Priority = 5)]  // Runs second
[Notification(Priority = 1)]  // Runs last
```

### Mixed Sequential/Parallel Execution
```csharp
// Sequential handlers with higher priority run first
[Notification(Priority = 10, DispatchMode = Sequential)]

// Then parallel handlers execute concurrently
[Notification(Priority = 0, DispatchMode = Parallel)]
```

### Custom Publisher with Rate Limiting
```csharp
public class RateLimitedPublisher : INotificationPublisher
{
    private readonly SemaphoreSlim _semaphore;

    public async ValueTask PublishAsync<TNotification>(...)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Publish with rate limiting
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## Benefits

✅ **Flexibility**: Choose the right strategy for your use case
✅ **Performance**: Parallel execution for independent handlers
✅ **Reliability**: Exception tolerance options
✅ **Simplicity**: Easy configuration and registration
✅ **Testability**: Mock different strategies in tests
✅ **Extensibility**: Implement custom publishers
