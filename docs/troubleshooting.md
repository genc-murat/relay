# Troubleshooting Guide

This guide helps you diagnose and resolve common issues when using Relay.

## Common Issues

### Compilation Errors

#### Handler Not Found

**Error:**
```
error CS0246: The type or namespace name 'HandlerNotFoundException' could not be found
```

**Cause:** No handler is registered for the request type.

**Solutions:**

1. **Ensure handler method has the correct attribute:**
   ```csharp
   public class UserService
   {
       [Handle] // ✅ Correct
       public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

2. **Verify request type implements correct interface:**
   ```csharp
   public record GetUserQuery(int UserId) : IRequest<User>; // ✅ Correct
   ```

3. **Check service registration:**
   ```csharp
   services.AddRelay(); // ✅ Registers Relay
   services.AddScoped<UserService>(); // ✅ Registers handler service
   ```

#### Multiple Handlers Found

**Error:**
```
Compilation error: Multiple handlers found for request type 'GetUserQuery'
```

**Cause:** Multiple unnamed handlers exist for the same request type.

**Solutions:**

1. **Use named handlers:**
   ```csharp
   [Handle(Name = "Default")]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   
   [Handle(Name = "Cached")]
   public async ValueTask<User> GetCachedUser(GetUserQuery query, CancellationToken cancellationToken)
   ```

2. **Remove duplicate handlers:**
   ```csharp
   // Keep only one unnamed handler
   [Handle]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   ```

#### Invalid Handler Signature

**Error:**
```
Compilation error: Handler method 'GetUser' has invalid signature
```

**Cause:** Handler method signature doesn't match expected pattern.

**Solutions:**

1. **Correct method signature for requests with response:**
   ```csharp
   [Handle]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   //     ^^^^^^^^^^^^^^^^ Must return ValueTask<T> or Task<T>
   //                                   ^^^^^^^^^ First parameter must be request
   //                                                      ^^^^^^^^^^^^^^^^^ Second parameter must be CancellationToken
   ```

2. **Correct method signature for requests without response:**
   ```csharp
   [Handle]
   public async ValueTask CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
   //     ^^^^^^^^^ Must return ValueTask or Task
   ```

3. **Correct method signature for streaming:**
   ```csharp
   [Handle]
   public async IAsyncEnumerable<User> GetUsers(GetUsersQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
   //     ^^^^^^^^^^^^^^^^^^^^^^^ Must return IAsyncEnumerable<T>
   ```

### Runtime Errors

#### Handler Execution Failed

**Error:**
```
HandlerExecutionException: Handler execution failed for request type 'GetUserQuery'
```

**Diagnosis:**

1. **Check inner exception:**
   ```csharp
   try
   {
       var user = await relay.SendAsync(new GetUserQuery(123));
   }
   catch (HandlerExecutionException ex)
   {
       _logger.LogError(ex.InnerException, "Handler failed: {Message}", ex.Message);
       // Check ex.InnerException for the actual error
   }
   ```

2. **Enable detailed logging:**
   ```csharp
   services.Configure<RelayOptions>(options =>
   {
       options.EnableDiagnostics = true;
       options.LogLevel = LogLevel.Debug;
   });
   ```

**Common Causes:**

1. **Null reference in handler:**
   ```csharp
   [Handle]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   {
       // ❌ _repository might be null
       return await _repository.GetByIdAsync(query.UserId, cancellationToken);
   }
   ```

   **Solution:** Check dependency injection registration:
   ```csharp
   services.AddScoped<IUserRepository, UserRepository>(); // ✅ Register dependencies
   ```

2. **Database connection issues:**
   ```csharp
   [Handle]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   {
       try
       {
           return await _repository.GetByIdAsync(query.UserId, cancellationToken);
       }
       catch (SqlException ex)
       {
           _logger.LogError(ex, "Database error getting user {UserId}", query.UserId);
           throw; // Re-throw or handle appropriately
       }
   }
   ```

#### Pipeline Execution Failed

**Error:**
```
PipelineExecutionException: Pipeline behavior failed
```

**Diagnosis:**

1. **Check pipeline order:**
   ```csharp
   [Pipeline(Order = -100)] // ✅ Validation runs first
   public async ValueTask<TResponse> ValidationPipeline<TRequest, TResponse>(...)
   
   [Pipeline(Order = 100)]  // ✅ Logging runs last
   public async ValueTask<TResponse> LoggingPipeline<TRequest, TResponse>(...)
   ```

2. **Ensure pipeline calls next:**
   ```csharp
   [Pipeline]
   public async ValueTask<TResponse> MyPipeline<TRequest, TResponse>(
       TRequest request,
       RequestHandlerDelegate<TResponse> next,
       CancellationToken cancellationToken)
   {
       // Pre-processing
       var response = await next(); // ✅ Must call next()
       // Post-processing
       return response;
   }
   ```

### Performance Issues

#### Slow Request Processing

**Symptoms:**
- High response times
- Timeouts
- Poor throughput

**Diagnosis:**

1. **Enable performance monitoring:**
   ```csharp
   services.Configure<RelayOptions>(options =>
   {
       options.EnableTelemetry = true;
   });
   
   // Check metrics
   var stats = metricsProvider.GetHandlerExecutionStats(typeof(GetUserQuery));
   if (stats.AverageExecutionTime > TimeSpan.FromMilliseconds(100))
   {
       // Investigate slow handlers
   }
   ```

2. **Profile handler execution:**
   ```csharp
   [Handle]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   {
       using var activity = Activity.StartActivity("GetUser");
       activity?.SetTag("UserId", query.UserId.ToString());
       
       var stopwatch = Stopwatch.StartNew();
       try
       {
           var user = await _repository.GetByIdAsync(query.UserId, cancellationToken);
           _logger.LogInformation("GetUser completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
           return user;
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "GetUser failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
           throw;
       }
   }
   ```

**Common Solutions:**

1. **Use ValueTask for synchronous paths:**
   ```csharp
   [Handle]
   public ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   {
       // Fast path: return cached result
       if (_cache.TryGetValue(query.UserId, out var cachedUser))
           return ValueTask.FromResult(cachedUser);
       
       // Slow path: database call
       return LoadUserAsync(query.UserId, cancellationToken);
   }
   ```

2. **Optimize database queries:**
   ```csharp
   [Handle]
   public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
   {
       // ❌ N+1 query problem
       var user = await _context.Users.FindAsync(query.UserId);
       user.Orders = await _context.Orders.Where(o => o.UserId == query.UserId).ToListAsync();
       
       // ✅ Single query with includes
       var user = await _context.Users
           .Include(u => u.Orders)
           .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
   }
   ```

3. **Add caching pipeline:**
   ```csharp
   [Pipeline(Order = -100)]
   public async ValueTask<TResponse> CachingPipeline<TRequest, TResponse>(
       TRequest request,
       RequestHandlerDelegate<TResponse> next,
       CancellationToken cancellationToken)
   {
       var cacheKey = GenerateCacheKey(request);
       if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
           return cachedResponse;
       
       var response = await next();
       _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
       return response;
   }
   ```

#### Memory Issues

**Symptoms:**
- High memory usage
- Frequent garbage collection
- OutOfMemoryException

**Diagnosis:**

1. **Use memory profiler** (dotMemory, PerfView, etc.)

2. **Check for memory leaks in handlers:**
   ```csharp
   [Handle]
   public async ValueTask<IEnumerable<User>> GetUsers(GetUsersQuery query, CancellationToken cancellationToken)
   {
       // ❌ Loads all users into memory
       return await _context.Users.ToListAsync(cancellationToken);
       
       // ✅ Use streaming for large datasets
       return _context.Users.AsAsyncEnumerable();
   }
   ```

3. **Monitor object pooling:**
   ```csharp
   services.Configure<RelayOptions>(options =>
   {
       options.EnableObjectPooling = true;
       options.ObjectPoolMaxSize = 1000; // Adjust based on load
   });
   ```

### Configuration Issues

#### Services Not Registered

**Error:**
```
InvalidOperationException: Unable to resolve service for type 'IRelay'
```

**Solution:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// ✅ Register Relay services
builder.Services.AddRelay();

// ✅ Register handler services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();
```

#### Configuration Not Applied

**Issue:** RelayOptions configuration not taking effect.

**Solution:**
```csharp
// ✅ Configure before AddRelay()
services.Configure<RelayOptions>(options =>
{
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.EnableTelemetry = true;
});

services.AddRelay();

// Or configure inline
services.AddRelay(options =>
{
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
});
```

## Debugging Techniques

### Enable Detailed Logging

```csharp
services.Configure<RelayOptions>(options =>
{
    options.EnableDiagnostics = true;
    options.LogLevel = LogLevel.Trace;
});

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

### Use Diagnostic Pipeline

```csharp
public class DiagnosticPipeline
{
    private readonly ILogger<DiagnosticPipeline> _logger;

    [Pipeline(Order = int.MinValue)] // Execute first
    public async ValueTask<TResponse> DiagnosticPipeline<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var requestType = typeof(TRequest).Name;
        
        _logger.LogInformation("[{RequestId}] Starting {RequestType}: {@Request}", 
            requestId, requestType, request);
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            
            _logger.LogInformation("[{RequestId}] Completed {RequestType} in {ElapsedMs}ms: {@Response}", 
                requestId, requestType, stopwatch.ElapsedMilliseconds, response);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{RequestId}] Failed {RequestType} after {ElapsedMs}ms", 
                requestId, requestType, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Source Generator Debugging

If the source generator isn't working:

1. **Check build output:**
   ```bash
   dotnet build --verbosity diagnostic
   ```

2. **Verify generated files:**
   - Look in `obj/Debug/net8.0/generated/` directory
   - Check for `RelayServiceCollectionExtensions.g.cs`

3. **Enable source generator logging:**
   ```xml
   <PropertyGroup>
     <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
     <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
   </PropertyGroup>
   ```

## Testing Issues

### Handler Not Found in Tests

**Issue:** Handlers work in application but not in tests.

**Solution:**
```csharp
[Test]
public async Task Should_Handle_Request()
{
    // ✅ Create test relay with handler
    var handler = new UserService(mockRepository.Object, mockLogger.Object);
    var relay = RelayTestHarness.CreateTestRelay(handler);
    
    var result = await relay.SendAsync(new GetUserQuery(123));
    
    Assert.NotNull(result);
}
```

### Mock Setup Issues

**Issue:** Mocking IRelay for unit tests.

**Solution:**
```csharp
[Test]
public async Task Controller_Should_Return_User()
{
    // ✅ Mock IRelay
    var mockRelay = new Mock<IRelay>();
    var expectedUser = new User { Id = 123, Name = "Murat" };
    
    mockRelay.Setup(r => r.SendAsync(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(expectedUser);
    
    var controller = new UsersController(mockRelay.Object);
    var result = await controller.GetUser(123, CancellationToken.None);
    
    Assert.IsType<OkObjectResult>(result.Result);
}
```

## Getting Help

### Enable Diagnostics

Always enable diagnostics when reporting issues:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.EnableDiagnostics = true;
    options.EnableTelemetry = true;
    options.LogLevel = LogLevel.Debug;
});
```

### Collect Information

When reporting issues, include:

1. **Relay version**
2. **Target framework** (.NET 6, 8, etc.)
3. **Complete error message and stack trace**
4. **Minimal reproduction code**
5. **Generated source files** (if source generator issue)
6. **Build output** (for compilation issues)

### Performance Issues

For performance problems:

1. **Run benchmarks** using BenchmarkDotNet
2. **Collect metrics** using built-in telemetry
3. **Profile memory usage** with dotMemory or similar
4. **Compare with baseline** (before Relay implementation)

### Community Resources

- **GitHub Issues**: Report bugs and feature requests
- **Discussions**: Ask questions and share experiences
- **Stack Overflow**: Tag questions with `relay-mediator`
- **Documentation**: Check the latest docs for updates

This troubleshooting guide covers the most common issues. For specific problems not covered here, please check the GitHub issues or create a new issue with detailed information.