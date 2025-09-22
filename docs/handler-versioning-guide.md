# Handler Versioning

Relay provides built-in support for handler versioning. This feature allows you to define multiple versions of the same handler and route requests to specific versions.

## 🚀 Quick Start

### 1. Enable Handler Versioning

To enable handler versioning, call `AddRelayHandlerVersioning()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayHandlerVersioning(); // Enable handler versioning
```

### 2. Define Versioned Handlers

Mark handlers with version information using the `HandlerVersionAttribute`:

```csharp
// Version 1.0 handler
public class UserServiceV1
{
    [Handle]
    [HandlerVersion("1.0", IsDefault = true)]
    public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
    {
        // Implementation for version 1.0
    }
}

// Version 2.0 handler
public class UserServiceV2
{
    [Handle]
    [HandlerVersion("2.0")]
    public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
    {
        // Implementation for version 2.0
    }
}
```

### 3. Use Handler Versioning

Use versioned relay to call specific handler versions:

```csharp
// Get the versioned relay instance
var versionedRelay = serviceProvider.GetRequiredService<IVersionedRelay>();

// Call version 2.0 of the handler
var request = new GetUserRequest(123);
var user = await versionedRelay.SendAsync(request, "2.0");
```

## 🎯 Key Features

### HandlerVersion Attribute

The `HandlerVersionAttribute` enables versioning for specific handlers:

```csharp
[Handle]
[HandlerVersion("1.0", IsDefault = true)] // Default version
public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
{
    // Implementation
}

[Handle]
[HandlerVersion("2.0")] // Version 2.0
public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
{
    // Implementation
}
```

### Versioned Relay Interface

The `IVersionedRelay` interface provides methods for calling versioned handlers:

```csharp
public interface IVersionedRelay
{
    ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, string version, CancellationToken cancellationToken = default);
    ValueTask SendAsync(IRequest request, string version, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, string version, CancellationToken cancellationToken = default);
    ValueTask PublishAsync<TNotification>(TNotification notification, string version, CancellationToken cancellationToken = default);
}
```

### Version Selection Strategies

Relay supports different version selection strategies:

1. **Exact Match** - Only exact version matches are used
2. **Latest Compatible** - Uses the latest compatible version
3. **Latest** - Uses the latest available version

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure handler versioning options for specific handlers:

```csharp
services.ConfigureHandlerVersioning<GetUserRequest>(options =>
{
    options.DefaultVersion = "2.0";
    options.VersionSelectionStrategy = VersionSelectionStrategy.LatestCompatible;
});
```

### Global Handler Versioning

Enable automatic handler versioning:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultHandlerVersioningOptions.EnableAutomaticVersioning = true;
    options.DefaultHandlerVersioningOptions.DefaultVersion = "1.0";
});
```

### Exception Handling

Configure whether to throw exceptions when a requested version is not found:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultHandlerVersioningOptions.ThrowOnVersionNotFound = false;
});
```

## ⚡ Performance

Handler versioning is designed to be lightweight and efficient:

- **Configurable**: Only executes when handler versioning is enabled
- **Fast Routing**: Uses optimized routing mechanisms
- **Async Support**: Fully asynchronous implementation
- **Low Overhead**: Minimal performance impact

## 🧪 Testing

Versioned handlers can be tested by calling specific versions and verifying behavior.