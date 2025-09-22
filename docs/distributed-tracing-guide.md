# Distributed Tracing Pipeline Behavior

Relay provides built-in support for distributed tracing through pipeline behaviors. This feature allows you to automatically trace request and response processing using OpenTelemetry.

## 🚀 Quick Start

### 1. Enable Distributed Tracing

To enable distributed tracing, call `AddRelayDistributedTracing()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayDistributedTracing(); // Enable distributed tracing
```

### 2. Define Requests with Distributed Tracing

Mark requests as requiring distributed tracing by applying the `TraceAttribute`:

```csharp
[Trace]
public record GetUserRequest(int UserId) : IRequest<User>;
```

### 3. Use Distributed Tracing

Distributed tracing happens automatically when you send requests:

```csharp
var request = new GetUserRequest(123);
var user = await relay.SendAsync(request); // Will create traces for request processing
```

## 🎯 Key Features

### Trace Attribute

The `TraceAttribute` enables distributed tracing for specific request types:

```csharp
[Trace(TraceRequest = true, TraceResponse = false)] // Only trace request
public record CreateOrderRequest(int UserId, decimal Total) : IRequest<Order>;

[Trace(OperationName = "GetUser")] // Custom operation name
public record GetUserRequest(int UserId) : IRequest<User>;
```

### OpenTelemetry Integration

Relay provides OpenTelemetry integration for distributed tracing:

```csharp
// Configure OpenTelemetry
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    tracerProviderBuilder
        .AddRelayInstrumentation()
        .AddConsoleExporter()
        .AddJaegerExporter();
});
```

### Custom Tracing Providers

Implement custom tracing providers by implementing the `IDistributedTracingProvider` interface:

```csharp
public class CustomTracingProvider : IDistributedTracingProvider
{
    public Activity? StartActivity(string operationName, Type requestType, string? correlationId = null, IDictionary<string, object?>? tags = null)
    {
        // Custom tracing implementation
    }
    
    // Implement other methods...
}
```

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure distributed tracing options for specific handlers:

```csharp
services.ConfigureDistributedTracing<GetUserRequest>(options =>
{
    options.TraceRequests = true;
    options.TraceResponses = true;
    options.RecordExceptions = true;
});
```

### Global Distributed Tracing

Enable automatic distributed tracing for all requests:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultDistributedTracingOptions.EnableAutomaticDistributedTracing = true;
    options.DefaultDistributedTracingOptions.ServiceName = "MyService";
});
```

### Exception Recording

Configure whether to record exceptions in traces:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultDistributedTracingOptions.RecordExceptions = false;
});
```

## ⚡ Performance

Distributed tracing is designed to be lightweight and efficient:

- **Configurable**: Only executes when distributed tracing is enabled
- **OpenTelemetry Integration**: Uses industry-standard tracing framework
- **Async Support**: Fully asynchronous implementation
- **Low Overhead**: Minimal performance impact when tracing is disabled

## 🧪 Testing

Requests with distributed tracing can be tested by verifying trace creation and propagation.