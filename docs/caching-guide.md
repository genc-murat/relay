# Caching Pipeline Behavior

Relay provides built-in support for caching handler results through pipeline behaviors. This feature allows you to cache expensive operations and improve application performance.

## 🚀 Quick Start

### 1. Enable Caching

To enable caching, call `AddRelayCaching()` or `AddRelayAdvancedCaching()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayAdvancedCaching(); // Enable advanced caching
```

### 2. Define Cacheable Requests

Mark requests as cacheable by applying the `CacheAttribute`:

```csharp
[Cache(30)] // Cache for 30 seconds
public record GetUserRequest(int UserId) : IRequest<User>;
```

### 3. Use Caching

Caching happens automatically when you send requests:

```csharp
var request = new GetUserRequest(123);
var user = await relay.SendAsync(request); // First call - executes handler
var user2 = await relay.SendAsync(request); // Second call - returns cached result
```

## 🎯 Key Features

### Cache Attribute

The `CacheAttribute` enables caching for specific request types:

```csharp
[Cache(60)] // Cache for 60 seconds
public record ExpensiveRequest(int Id) : IRequest<ExpensiveResult>;
```

### Memory and Distributed Caching

The advanced caching pipeline behavior supports both in-memory and distributed caching:

```csharp
// Configure distributed caching
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost";
    options.InstanceName = "RelaySample:";
});

services.AddRelayAdvancedCaching();
```

### Sliding and Absolute Expiration

Configure caching behavior with sliding or absolute expiration:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultCachingOptions.UseSlidingExpiration = true;
    options.DefaultCachingOptions.SlidingExpirationSeconds = 30;
});
```

### Automatic Caching

Enable automatic caching for all requests:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultCachingOptions.EnableAutomaticCaching = true;
    options.DefaultCachingOptions.DefaultCacheDurationSeconds = 60;
});
```

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure caching options for specific handlers:

```csharp
services.ConfigureCaching<GetUserRequest>(options =>
{
    options.DefaultCacheDurationSeconds = 120;
    options.UseSlidingExpiration = true;
});
```

### Cache Key Generation

The caching pipeline behavior generates cache keys based on the request type and serialized request data. For high-performance scenarios, you can implement a custom key generation strategy.

### Cache Size Limits

Configure cache size limits to prevent memory issues:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultCachingOptions.SizeLimitMegabytes = 50; // 50 MB limit
});
```

## ⚡ Performance

Caching significantly improves performance for expensive operations:

- **Memory Caching**: Fast, in-process caching with minimal overhead
- **Distributed Caching**: Shared caching across multiple instances
- **Sliding Expiration**: Keep frequently accessed data in cache longer
- **Absolute Expiration**: Ensure data freshness

## 🧪 Testing

Cached requests can be tested like regular requests. The caching behavior is transparent to the caller.