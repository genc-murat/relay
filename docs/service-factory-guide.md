# ServiceFactory Pattern in Relay

## Overview

Relay now includes **ServiceFactory** delegate pattern, providing MediatR-compatible service resolution for enhanced flexibility and easier migration.

## What is ServiceFactory?

`ServiceFactory` is a delegate that provides a standardized way to resolve services from the DI container:

```csharp
public delegate object? ServiceFactory(Type serviceType);
```

This pattern is particularly useful when:
- You need to resolve services dynamically at runtime
- You want to avoid circular dependencies
- Services may not always be registered
- You're migrating from MediatR and want to maintain compatibility

## Basic Usage

### Automatic Registration

ServiceFactory is automatically registered when you add Relay:

```csharp
services.AddRelay();
// ServiceFactory is now available in DI container
```

### Injecting ServiceFactory

You can inject ServiceFactory into any class:

```csharp
public class MyPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public MyPipelineBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Resolve services dynamically
        var logger = _serviceFactory(typeof(ILogger)) as ILogger;
        
        return await next();
    }
}
```

## Extension Methods

Relay provides type-safe extension methods for easier service resolution:

### GetService<T>

Resolves a service of the specified type:

```csharp
var logger = _serviceFactory.GetService<ILogger<MyClass>>();
if (logger != null)
{
    logger.LogInformation("Service resolved successfully");
}
```

### GetRequiredService<T>

Resolves a required service (throws if not found):

```csharp
// Throws InvalidOperationException if not registered
var logger = _serviceFactory.GetRequiredService<ILogger<MyClass>>();
logger.LogInformation("Required service resolved");
```

### GetServices<T>

Resolves all services of the specified type:

```csharp
var validators = _serviceFactory.GetServices<IValidator<MyRequest>>();
foreach (var validator in validators)
{
    await validator.ValidateAsync(request, cancellationToken);
}
```

### TryGetService<T>

Safely attempts to resolve a service:

```csharp
if (_serviceFactory.TryGetService<ICache>(out var cache) && cache != null)
{
    // Use cache if available
    var cachedResult = await cache.GetAsync(key);
}
```

## Real-World Examples

### Example 1: Dynamic Logger Resolution

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public LoggingBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var logger = _serviceFactory.GetService<ILogger<LoggingBehavior<TRequest, TResponse>>>();
        
        logger?.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        
        try
        {
            var response = await next();
            logger?.LogInformation("Successfully handled {RequestType}", typeof(TRequest).Name);
            return response;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}
```

### Example 2: Conditional Service Resolution

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableRequest
{
    private readonly ServiceFactory _serviceFactory;
    
    public CachingBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Try to use cache if available, otherwise skip
        if (!_serviceFactory.TryGetService<IDistributedCache>(out var cache) || cache == null)
        {
            return await next();
        }
        
        var cacheKey = request.GetCacheKey();
        
        // Try to get from cache
        var cached = await cache.GetAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            return DeserializeResponse(cached);
        }
        
        // Execute handler and cache result
        var response = await next();
        await cache.SetAsync(cacheKey, SerializeResponse(response), cancellationToken);
        
        return response;
    }
}
```

### Example 3: Multiple Service Resolution

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public ValidationBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Resolve all validators for this request type
        var validators = _serviceFactory.GetServices<IValidator<TRequest>>();
        
        var errors = new List<string>();
        
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                errors.AddRange(result.Errors);
            }
        }
        
        if (errors.Any())
        {
            throw new ValidationException(errors);
        }
        
        return await next();
    }
}
```

### Example 4: Accessing ServiceFactory from IRelay

```csharp
public class MyController
{
    private readonly IRelay _relay;
    
    public MyController(IRelay relay)
    {
        _relay = relay;
    }
    
    public async Task<IActionResult> ProcessRequest()
    {
        // Access ServiceFactory through RelayImplementation
        if (_relay is RelayImplementation implementation)
        {
            var serviceFactory = implementation.ServiceFactory;
            
            // Use factory to resolve services
            var logger = serviceFactory.GetService<ILogger>();
            logger?.LogInformation("Processing request");
        }
        
        var result = await _relay.SendAsync(new MyRequest());
        return Ok(result);
    }
}
```

## Comparison with MediatR

### MediatR Pattern

```csharp
// MediatR uses ServiceFactory in pipeline behaviors
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public MyBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var logger = _serviceFactory(typeof(ILogger)) as ILogger;
        return next();
    }
}
```

### Relay Pattern (Compatible)

```csharp
// Relay uses the same ServiceFactory pattern with enhanced type-safety
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public MyBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Same pattern + type-safe extensions
        var logger = _serviceFactory.GetService<ILogger>();
        return await next();
    }
}
```

## Benefits

### ✅ **MediatR Compatibility**
- Same delegate signature as MediatR
- Easy migration path from MediatR
- Familiar pattern for MediatR users

### ✅ **Type-Safe Extensions**
- `GetService<T>()` for safe casting
- `GetRequiredService<T>()` for required services
- `GetServices<T>()` for multiple services
- `TryGetService<T>()` for conditional resolution

### ✅ **Flexibility**
- Dynamic service resolution at runtime
- Avoid circular dependencies
- Conditional service usage
- Multiple service resolution

### ✅ **Performance**
- Direct delegate invocation
- Minimal overhead
- Compatible with Relay's high-performance architecture

## Best Practices

### 1. Prefer Constructor Injection When Possible

```csharp
// ✅ Good: Constructor injection for always-required services
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger _logger;
    
    public MyBehavior(ILogger<MyBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
}

// ✅ Also Good: ServiceFactory for conditional services
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public MyBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(...)
    {
        // Resolve only if needed
        if (condition)
        {
            var cache = _serviceFactory.GetService<ICache>();
        }
    }
}
```

### 2. Use Type-Safe Extensions

```csharp
// ❌ Avoid: Manual casting
var logger = _serviceFactory(typeof(ILogger)) as ILogger;

// ✅ Prefer: Type-safe extension
var logger = _serviceFactory.GetService<ILogger>();
```

### 3. Handle Null Services Gracefully

```csharp
// ✅ Good: Check for null
var cache = _serviceFactory.GetService<ICache>();
if (cache != null)
{
    await cache.SetAsync(key, value);
}

// ✅ Also Good: Use TryGetService
if (_serviceFactory.TryGetService<ICache>(out var cache) && cache != null)
{
    await cache.SetAsync(key, value);
}
```

### 4. Use GetRequiredService for Required Dependencies

```csharp
// ✅ Good: Fail fast if service is not registered
var validator = _serviceFactory.GetRequiredService<IValidator<TRequest>>();
await validator.ValidateAsync(request, cancellationToken);
```

## Migration from MediatR

If you're migrating from MediatR, ServiceFactory works exactly the same way:

### Before (MediatR)

```csharp
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public MyBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var logger = _serviceFactory(typeof(ILogger)) as ILogger;
        return await next();
    }
}
```

### After (Relay) - No Changes Needed!

```csharp
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ServiceFactory _serviceFactory;
    
    public MyBehavior(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }
    
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Same ServiceFactory, just use extension methods for type-safety
        var logger = _serviceFactory.GetService<ILogger>();
        return await next();
    }
}
```

## Summary

The ServiceFactory pattern in Relay provides:

- **MediatR Compatibility**: Same delegate pattern for easy migration
- **Type Safety**: Extension methods for safer service resolution
- **Flexibility**: Dynamic service resolution when needed
- **Performance**: Minimal overhead with direct delegate invocation
- **Best Practices**: Clear guidance on when and how to use it

This feature makes Relay even more compatible with MediatR while maintaining its high-performance architecture!
