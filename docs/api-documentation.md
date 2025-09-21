# API Documentation

## Core Interfaces

### IRelay

The main interface for sending requests and publishing notifications.

```csharp
public interface IRelay
{
    // Send request with response
    ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default);

    // Send request without response
    ValueTask SendAsync(
        IRequest request, 
        CancellationToken cancellationToken = default);

    // Send to named handler
    ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        string handlerName, 
        CancellationToken cancellationToken = default);

    // Stream responses
    IAsyncEnumerable<TResponse> StreamAsync<TResponse>(
        IStreamRequest<TResponse> request, 
        CancellationToken cancellationToken = default);

    // Publish notification
    ValueTask PublishAsync<TNotification>(
        TNotification notification, 
        CancellationToken cancellationToken = default) 
        where TNotification : INotification;
}
```

### Request Interfaces

```csharp
// Request with response
public interface IRequest<out TResponse> { }

// Request without response
public interface IRequest { }

// Streaming request
public interface IStreamRequest<out TResponse> { }

// Notification
public interface INotification { }
```

### Handler Interfaces

For explicit interface implementation (optional):

```csharp
public interface IRequestHandler<in TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest> 
    where TRequest : IRequest
{
    ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface IStreamHandler<in TRequest, out TResponse> 
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

public interface INotificationHandler<in TNotification> 
    where TNotification : INotification
{
    ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
```

## Attributes

### HandleAttribute

Registers a method as a request handler.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class HandleAttribute : Attribute
{
    public string? Name { get; set; }
    public int Priority { get; set; } = 0;
}
```

**Properties:**
- `Name`: Optional name for the handler (enables multiple handlers per request type)
- `Priority`: Execution priority (higher values execute first)

**Usage:**
```csharp
[Handle] // Basic handler
[Handle(Name = "Fast")] // Named handler
[Handle(Priority = 10)] // High priority handler
```

### NotificationAttribute

Registers a method as a notification handler.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class NotificationAttribute : Attribute
{
    public NotificationDispatchMode DispatchMode { get; set; } = NotificationDispatchMode.Parallel;
    public int Priority { get; set; } = 0;
}
```

**Properties:**
- `DispatchMode`: How to execute multiple handlers (Parallel or Sequential)
- `Priority`: Execution order for sequential dispatch

**Usage:**
```csharp
[Notification] // Parallel execution
[Notification(DispatchMode = NotificationDispatchMode.Sequential)]
[Notification(Priority = 5)] // Higher priority executes first in sequential mode
```

### PipelineAttribute

Registers a method as a pipeline behavior.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class PipelineAttribute : Attribute
{
    public int Order { get; set; } = 0;
    public PipelineScope Scope { get; set; } = PipelineScope.All;
}
```

**Properties:**
- `Order`: Execution order (lower values execute first)
- `Scope`: Which operations the pipeline applies to

**Usage:**
```csharp
[Pipeline] // Applies to all operations
[Pipeline(Order = -100)] // Execute early
[Pipeline(Scope = PipelineScope.Requests)] // Only requests
```

### ExposeAsEndpointAttribute

Generates HTTP endpoint metadata for handlers.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class ExposeAsEndpointAttribute : Attribute
{
    public string? Route { get; set; }
    public string HttpMethod { get; set; } = "POST";
    public string? Version { get; set; }
}
```

**Usage:**
```csharp
[Handle]
[ExposeAsEndpoint(Route = "/api/users/{id}", HttpMethod = "GET")]
public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
```

## Pipeline System

### Pipeline Behavior Signature

```csharp
public async ValueTask<TResponse> MyPipeline<TRequest, TResponse>(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    // Pre-processing
    var response = await next();
    // Post-processing
    return response;
}
```

### Streaming Pipeline Signature

```csharp
public async IAsyncEnumerable<TResponse> MyStreamPipeline<TRequest, TResponse>(
    TRequest request,
    StreamHandlerDelegate<TResponse> next,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // Pre-processing
    await foreach (var item in next())
    {
        // Process each item
        yield return item;
    }
}
```

### Pipeline Scopes

```csharp
public enum PipelineScope
{
    All,           // All operations
    Requests,      // Request/response operations only
    Streams,       // Streaming operations only
    Notifications  // Notification operations only
}
```

## Configuration

### RelayOptions

```csharp
public class RelayOptions
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableTelemetry { get; set; } = true;
    public bool EnableDiagnostics { get; set; } = false;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public NotificationDispatchMode DefaultNotificationDispatchMode { get; set; } = NotificationDispatchMode.Parallel;
    public int MaxConcurrentNotifications { get; set; } = Environment.ProcessorCount * 2;
    public bool EnableObjectPooling { get; set; } = true;
    public int ObjectPoolMaxSize { get; set; } = 100;
}
```

### Configuration Methods

```csharp
// Configure options
services.Configure<RelayOptions>(options =>
{
    options.DefaultTimeout = TimeSpan.FromMinutes(5);
    options.EnableTelemetry = true;
});

// Configure from configuration section
services.Configure<RelayOptions>(configuration.GetSection("Relay"));

// Configure with builder
services.AddRelay(options =>
{
    options.EnableDiagnostics = true;
});

// Enable built-in caching
services.AddRelayCaching();
```

## Telemetry and Observability

### ITelemetryProvider

```csharp
public interface ITelemetryProvider
{
    Activity? StartActivity(string operationName, Type requestType, string? handlerName = null);
    void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, 
        TimeSpan duration, bool success, Exception? exception = null);
    void RecordNotificationPublish(Type notificationType, int handlerCount, 
        TimeSpan duration, bool success, Exception? exception = null);
    void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, 
        TimeSpan duration, long itemCount, bool success, Exception? exception = null);
    void SetCorrelationId(string correlationId);
    string? GetCorrelationId();
}
```

### Metrics Collection

```csharp
public interface IMetricsProvider
{
    void RecordHandlerExecution(HandlerExecutionMetrics metrics);
    void RecordNotificationPublish(NotificationPublishMetrics metrics);
    void RecordStreamingOperation(StreamingOperationMetrics metrics);
    
    HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null);
    NotificationPublishStats GetNotificationPublishStats(Type notificationType);
    StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null);
    
    IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan timeWindow);
    TimingBreakdown? GetTimingBreakdown(string operationId);
}
```

## Error Handling

### Exception Types

```csharp
public class RelayException : Exception
{
    public string RequestType { get; }
    public string? HandlerName { get; }
}

public class HandlerNotFoundException : RelayException
{
    public HandlerNotFoundException(string requestType);
}

public class MultipleHandlersFoundException : RelayException
{
    public MultipleHandlersFoundException(string requestType, IEnumerable<string> handlerNames);
}

public class HandlerExecutionException : RelayException
{
    public HandlerExecutionException(string requestType, string? handlerName, Exception innerException);
}
```

## Testing Support

### RelayTestHarness

```csharp
public static class RelayTestHarness
{
    public static IRelay CreateTestRelay(params object[] handlers);
    public static IRelay CreateTestRelay(IServiceProvider serviceProvider);
    public static Mock<IRelay> CreateMockRelay();
}
```

### Test Attributes

```csharp
[TestHandle] // Only registered in test environments
[TestNotification]
[TestPipeline]
```

### Usage Example

```csharp
[Test]
public async Task Should_Handle_Request()
{
    // Arrange
    var handler = new UserService();
    var relay = RelayTestHarness.CreateTestRelay(handler);
    
    // Act
    var result = await relay.SendAsync(new GetUserQuery(123));
    
    // Assert
    Assert.NotNull(result);
}
```

## Performance Optimizations

### ValueTask Usage

```csharp
// Prefer ValueTask for potentially synchronous operations
[Handle]
public ValueTask<User> GetCachedUser(GetUserQuery query, CancellationToken cancellationToken)
{
    if (_cache.TryGetValue(query.UserId, out var user))
        return ValueTask.FromResult(user); // Synchronous completion
    
    return LoadUserAsync(query.UserId, cancellationToken); // Asynchronous
}
```

### Object Pooling

```csharp
// Automatic pooling for telemetry contexts
services.AddSingleton<ITelemetryContextPool, DefaultTelemetryContextPool>();

// Custom pooling
services.AddSingleton<IObjectPool<MyObject>>(provider =>
    new DefaultObjectPool<MyObject>(new MyObjectPoolPolicy()));
```

### Memory Optimization

```csharp
// Use Span<T> for efficient data handling
[Handle]
public ValueTask ProcessData(ProcessDataCommand command, CancellationToken cancellationToken)
{
    ReadOnlySpan<byte> data = command.Data.AsSpan();
    // Process without additional allocations
    return ValueTask.CompletedTask;
}
```

## Source Generator Integration

The source generator automatically creates:

1. **Handler Registry**: Compile-time mapping of requests to handlers
2. **DI Registration**: ServiceCollection extensions for automatic registration
3. **Dispatch Code**: Optimized method calls without reflection
4. **Validation**: Compile-time validation of handler signatures

### Generated Code Example

```csharp
// Generated ServiceCollection extension
public static class RelayServiceCollectionExtensions
{
    public static IServiceCollection AddRelay(this IServiceCollection services)
    {
        services.AddSingleton<IRelay, GeneratedRelay>();
        services.AddSingleton<IRequestDispatcher, GeneratedRequestDispatcher>();
        // ... other registrations
        return services;
    }
}

// Generated dispatcher
internal class GeneratedRequestDispatcher : IRequestDispatcher
{
    public async ValueTask<TResponse> DispatchAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken)
    {
        return request switch
        {
            GetUserQuery getUserQuery => await _userService.GetUser(getUserQuery, cancellationToken),
            // ... other handlers
            _ => throw new HandlerNotFoundException(typeof(TRequest).Name)
        };
    }
}
```