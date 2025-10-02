# 🏷️ Relay Framework - Attribute Kullanım Örnekleri

Bu dosya, **Relay Framework**'ün sunduğu tüm attribute'ların nasıl kullanıldığını kapsamlı örneklerle göstermektedir.

## 📋 İçindekiler

1. [Handle Attribute](#-handle-attribute)
2. [Notification Attribute](#-notification-attribute)
3. [Pipeline Attribute](#-pipeline-attribute)
4. [ExposeAsEndpoint Attribute](#-exposeasendpoint-attribute)
5. [Kombine Kullanım Örnekleri](#-kombine-kullanım-örnekleri)
6. [Configuration ile Override](#-configuration-ile-override)

---

## 🎯 Handle Attribute

`[Handle]` attribute'u, method'ları request handler olarak işaretlemek için kullanılır.

### Temel Kullanım

```csharp
public class UserHandler : IRequestHandler<GetUserQuery, User?>
{
    [Handle] // Basit kullanım
    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetByIdAsync(request.UserId);
    }
}
```

### Named Handler

```csharp
public class UserHandler : IRequestHandler<GetUserQuery, User?>
{
    [Handle(Name = "GetUserFromDatabase")]
    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetByIdAsync(request.UserId);
    }
}

public class CachedUserHandler : IRequestHandler<GetUserQuery, User?>
{
    [Handle(Name = "GetUserFromCache")]
    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetUserAsync(request.UserId);
    }
}

// Kullanım
var user = await _relay.SendAsync(new GetUserQuery(123), "GetUserFromCache");
```

### Priority ile Handler

```csharp
public class PrimaryUserHandler : IRequestHandler<GetUserQuery, User?>
{
    [Handle(Priority = 10)] // Yüksek priority - önce çalışır
    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        if (await _primaryDbService.IsAvailableAsync())
        {
            return await _primaryDbService.GetUserAsync(request.UserId);
        }
        return null; // Fallback handler'a geç
    }
}

public class FallbackUserHandler : IRequestHandler<GetUserQuery, User?>
{
    [Handle(Priority = 1)] // Düşük priority - fallback
    public async ValueTask<User?> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _backupDbService.GetUserAsync(request.UserId);
    }
}
```

### Kompleks Handle Example

```csharp
public class AdvancedUserHandler
{
    [Handle(Name = "CreateUserWithValidation", Priority = 5)]
    public async ValueTask<User> HandleCreateUser(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Validation burada pipeline'da yapılacak
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _userService.CreateAsync(user);
    }

    [Handle(Name = "UpdateUserProfile", Priority = 3)]
    public async ValueTask<User?> HandleUpdateUser(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userService.GetByIdAsync(request.UserId);
        if (existingUser == null) return null;

        existingUser.Name = request.Name ?? existingUser.Name;
        existingUser.Email = request.Email ?? existingUser.Email;
        existingUser.UpdatedAt = DateTime.UtcNow;

        return await _userService.UpdateAsync(existingUser);
    }
}
```

---

## 📢 Notification Attribute

`[Notification]` attribute'u, notification handler'ları yapılandırmak için kullanılır.

### Temel Notification Handler

```csharp
public class UserEmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    [Notification] // Basit kullanım - parallel execution
    public async ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email);
    }
}
```

### Priority ile Notification

```csharp
public class UserNotificationHandlers
{
    // En yüksek priority - ilk çalışacak
    [Notification(Priority = 10, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleUserValidation(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _validationService.ValidateNewUserAsync(notification.UserId);
    }

    // Orta priority
    [Notification(Priority = 5)]
    public async ValueTask HandleUserAnalytics(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _analyticsService.TrackUserCreatedAsync(notification.UserId);
    }

    // Düşük priority - son çalışacak
    [Notification(Priority = 1)]
    public async ValueTask HandleUserWelcomeEmail(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email);
    }
}
```

### Dispatch Mode ile Notification

```csharp
public class OrderNotificationHandlers
{
    // Sequential - sırayla çalışacak (inventory → payment → email)
    [Notification(Priority = 10, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleInventoryUpdate(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _inventoryService.ReserveItemsAsync(notification.OrderId);
    }

    [Notification(Priority = 9, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandlePaymentProcessing(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _paymentService.ProcessPaymentAsync(notification.OrderId);
    }

    // Parallel - diğerleriyle paralel çalışacak
    [Notification(Priority = 1, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleOrderConfirmationEmail(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationAsync(notification.UserId, notification.OrderId);
    }

    [Notification(Priority = 1, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleOrderAnalytics(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _analyticsService.TrackOrderCreatedAsync(notification.OrderId, notification.TotalAmount);
    }
}
```

### Error Handling ile Notification

```csharp
public class ResilientNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    [Notification(Priority = 5)]
    public async ValueTask HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _externalApiService.NotifyUserCreatedAsync(notification.UserId);
        }
        catch (Exception ex)
        {
            // Hata durumunda log'la ama diğer handler'ları etkileme
            _logger.LogError(ex, "External API notification failed for user {UserId}", notification.UserId);
            // Continue - diğer handler'lar çalışmaya devam etsin
        }
    }
}
```

---

## 🔧 Pipeline Attribute

`[Pipeline]` attribute'u, pipeline behavior'ları yapılandırmak için kullanılır.

### Temel Pipeline Behavior

```csharp
public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    [Pipeline(Order = 0, Scope = PipelineScope.All)] // En önce çalışacak, tüm request'lerde
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling request: {RequestName}", requestName);

        var response = await next();

        _logger.LogInformation("Handled request: {RequestName}", requestName);
        return response;
    }
}
```

### Scoped Pipeline Behaviors

```csharp
// Sadece Request'lerde çalışacak (Stream ve Notification'larda değil)
public class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    [Pipeline(Order = 1, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validator.CanValidate(typeof(TRequest)))
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        return await next();
    }
}

// Sadece Streaming request'lerde çalışacak
public class StreamingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    [Pipeline(Order = 2, Scope = PipelineScope.Streams)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting stream processing for {RequestType}", typeof(TRequest).Name);
        
        var response = await next();
        
        _logger.LogInformation("Stream processing completed for {RequestType}", typeof(TRequest).Name);
        return response;
    }
}

// Sadece Notification'larda çalışacak
public class NotificationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    [Pipeline(Order = 1, Scope = PipelineScope.Notifications)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing notification: {NotificationType}", typeof(TRequest).Name);
        
        return await next();
    }
}
```

### Conditional Pipeline

```csharp
public class CachingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    [Pipeline(Order = 2, Scope = PipelineScope.Requests)]
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Sadece query request'leri cache'le
        if (!IsQueryRequest(typeof(TRequest)))
        {
            return await next();
        }

        var cacheKey = GenerateCacheKey(request);
        
        // Cache'den kontrol et
        if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
        {
            _logger.LogInformation("Cache hit for {RequestType}", typeof(TRequest).Name);
            return cachedResponse;
        }

        // Cache miss - handler'ı çal
        var response = await next();
        
        // Response'u cache'le
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = GetCacheDuration(typeof(TRequest))
        };
        
        _cache.Set(cacheKey, response, cacheOptions);
        _logger.LogInformation("Cached response for {RequestType}", typeof(TRequest).Name);
        
        return response;
    }
}
```

### Performance Monitoring Pipeline

```csharp
public class PerformanceMonitoringPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    [Pipeline(Order = -1, Scope = PipelineScope.All)] // En son çalışacak (timing için)
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestName = typeof(TRequest).Name;

        try
        {
            var response = await next();
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            
            if (elapsedMs > 1000) // Slow request warning
            {
                _logger.LogWarning("Slow request detected: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs);
            }
            else
            {
                _logger.LogDebug("Request {RequestName} completed in {ElapsedMs}ms", requestName, elapsedMs);
            }

            // Metrics collection
            _metrics.RecordRequestDuration(requestName, elapsedMs);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            _metrics.RecordRequestError(requestName);
            throw;
        }
    }
}
```

---

## 🌐 ExposeAsEndpoint Attribute

`[ExposeAsEndpoint]` attribute'u, handler'ları otomatik HTTP endpoint'leri olarak expose etmek için kullanılır.

### Temel Endpoint

```csharp
public class UserApiHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = "/api/users/{userId}", HttpMethod = "GET")]
    public async ValueTask<User?> GetUser(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetByIdAsync(request.UserId);
    }

    [Handle]
    [ExposeAsEndpoint(Route = "/api/users", HttpMethod = "POST")]
    public async ValueTask<User> CreateUser(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _userService.CreateAsync(user);
    }
}
```

### Versioned Endpoints

```csharp
public class UserApiV1Handler
{
    [Handle(Name = "GetUserV1")]
    [ExposeAsEndpoint(Route = "/api/v1/users/{userId}", HttpMethod = "GET", Version = "1.0")]
    public async ValueTask<UserV1> GetUserV1(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(request.UserId);
        return _mapper.Map<UserV1>(user); // V1 response format
    }
}

public class UserApiV2Handler
{
    [Handle(Name = "GetUserV2")]
    [ExposeAsEndpoint(Route = "/api/v2/users/{userId}", HttpMethod = "GET", Version = "2.0")]
    public async ValueTask<UserV2> GetUserV2(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(request.UserId);
        return _mapper.Map<UserV2>(user); // V2 response format (enhanced)
    }
}
```

### RESTful API Endpoints

```csharp
public class ProductApiHandler
{
    [Handle]
    [ExposeAsEndpoint(Route = "/api/products", HttpMethod = "GET")]
    public async ValueTask<PagedResponse<Product>> GetProducts(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await _productService.GetPagedAsync(request.Page, request.PageSize, request.Category);
    }

    [Handle]
    [ExposeAsEndpoint(Route = "/api/products/{productId}", HttpMethod = "GET")]
    public async ValueTask<Product?> GetProduct(GetProductQuery request, CancellationToken cancellationToken)
    {
        return await _productService.GetByIdAsync(request.ProductId);
    }

    [Handle]
    [ExposeAsEndpoint(Route = "/api/products", HttpMethod = "POST")]
    public async ValueTask<Product> CreateProduct(CreateProductCommand request, CancellationToken cancellationToken)
    {
        return await _productService.CreateAsync(request);
    }

    [Handle]
    [ExposeAsEndpoint(Route = "/api/products/{productId}", HttpMethod = "PUT")]
    public async ValueTask<Product?> UpdateProduct(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return await _productService.UpdateAsync(request);
    }

    [Handle]
    [ExposeAsEndpoint(Route = "/api/products/{productId}", HttpMethod = "DELETE")]
    public async ValueTask<bool> DeleteProduct(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        return await _productService.DeleteAsync(request.ProductId);
    }
}
```

---

## 🔗 Kombine Kullanım Örnekleri

### Complete Handler Example

```csharp
public class ComprehensiveUserHandler : 
    IRequestHandler<GetUserQuery, User?>,
    IRequestHandler<CreateUserCommand, User>,
    INotificationHandler<UserCreatedNotification>
{
    // High-priority cached GET endpoint
    [Handle(Name = "GetUserOptimized", Priority = 10)]
    [ExposeAsEndpoint(Route = "/api/users/{userId}", HttpMethod = "GET", Version = "2.0")]
    public async ValueTask<User?> GetUser(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userService.GetByIdAsync(request.UserId);
    }

    // POST endpoint with notifications
    [Handle(Name = "CreateUserAdvanced", Priority = 5)]
    [ExposeAsEndpoint(Route = "/api/users", HttpMethod = "POST")]
    public async ValueTask<User> CreateUser(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request);
        
        // Notification otomatik olarak publish edilecek (pipeline'da)
        await _mediator.PublishAsync(new UserCreatedNotification(user.Id, user.Name, user.Email));
        
        return user;
    }

    // High-priority notification handler
    [Notification(Priority = 10, DispatchMode = NotificationDispatchMode.Sequential)]
    public async ValueTask HandleUserCreated(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Critical operations first
        await _auditService.LogUserCreatedAsync(notification.UserId);
        await _securityService.SetupUserPermissionsAsync(notification.UserId);
    }
}
```

### Complete Pipeline Setup

```csharp
// Request Processing Pipeline
public class RequestPipelineSetup
{
    // 1. Performance monitoring (wrapper)
    [Pipeline(Order = -2, Scope = PipelineScope.All)]
    public class PerformanceWrapper<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

    // 2. Logging (first)  
    [Pipeline(Order = -1, Scope = PipelineScope.All)]
    public class LoggingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

    // 3. Authentication/Authorization
    [Pipeline(Order = 0, Scope = PipelineScope.Requests)]
    public class AuthorizationPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

    // 4. Validation
    [Pipeline(Order = 1, Scope = PipelineScope.Requests)]
    public class ValidationPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

    // 5. Caching
    [Pipeline(Order = 2, Scope = PipelineScope.Requests)]
    public class CachingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

    // 6. Rate Limiting
    [Pipeline(Order = 3, Scope = PipelineScope.Requests)]
    public class RateLimitingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

    // 7. Exception Handling (outer wrapper)
    [Pipeline(Order = 10, Scope = PipelineScope.All)]
    public class ExceptionHandlingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }
}
```

---

## ⚙️ Configuration ile Override

Attribute'lar configuration ile override edilebilir:

### appsettings.json Configuration

```json
{
  "Relay": {
    "EnableTelemetry": true,
    "MaxConcurrentNotificationHandlers": 10,
    "HandlerOverrides": {
      "GetUserQueryHandler.HandleAsync": {
        "DefaultPriority": 15,
        "EnableCaching": true,
        "DefaultTimeout": "00:00:30",
        "EnableRetry": true,
        "MaxRetryAttempts": 3
      }
    },
    "NotificationOverrides": {
      "UserCreatedEmailHandler.HandleAsync": {
        "DefaultDispatchMode": "Sequential",
        "DefaultPriority": 20,
        "ContinueOnError": false,
        "DefaultTimeout": "00:01:00",
        "MaxDegreeOfParallelism": 1
      }
    },
    "PipelineOverrides": {
      "ValidationPipeline.HandleAsync": {
        "DefaultOrder": 0,
        "DefaultScope": "Requests",
        "EnableCaching": false,
        "DefaultTimeout": "00:00:10"
      }
    }
  }
}
```

### Programmatic Configuration

```csharp
// Service registration
services.ConfigureRelay(options =>
{
    options.EnableTelemetry = true;
    options.MaxConcurrentNotificationHandlers = 10;
});

// Handler-specific configuration
services.ConfigureHandler("GetUserQueryHandler.HandleAsync", options =>
{
    options.DefaultPriority = 15;
    options.EnableCaching = true;
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.EnableRetry = true;
    options.MaxRetryAttempts = 3;
});

// Notification-specific configuration  
services.ConfigureNotification("UserCreatedEmailHandler.HandleAsync", options =>
{
    options.DefaultDispatchMode = NotificationDispatchMode.Sequential;
    options.DefaultPriority = 20;
    options.ContinueOnError = false;
    options.DefaultTimeout = TimeSpan.FromMinutes(1);
    options.MaxDegreeOfParallelism = 1;
});

// Pipeline-specific configuration
services.ConfigurePipeline("ValidationPipeline.HandleAsync", options =>
{
    options.DefaultOrder = 0;
    options.DefaultScope = PipelineScope.Requests;
    options.EnableCaching = false;
    options.DefaultTimeout = TimeSpan.FromSeconds(10);
});
```

---

## 🎯 Best Practices

### 1. Attribute Priorities
```csharp
// Critical işlemler için yüksek priority
[Handle(Priority = 10)] // Database operations
[Handle(Priority = 5)]  // Cache operations  
[Handle(Priority = 1)]  // Fallback operations

[Notification(Priority = 10)] // Validation/Security
[Notification(Priority = 5)]  // Business logic
[Notification(Priority = 1)]  // Logging/Analytics
```

### 2. Pipeline Ordering
```csharp
[Pipeline(Order = -2)] // Performance monitoring (outermost)
[Pipeline(Order = -1)] // Logging
[Pipeline(Order = 0)]  // Authentication/Authorization
[Pipeline(Order = 1)]  // Validation
[Pipeline(Order = 2)]  // Caching
[Pipeline(Order = 10)] // Exception handling (outermost)
```

### 3. Scope Usage
```csharp
[Pipeline(Scope = PipelineScope.All)]           // Logging, monitoring
[Pipeline(Scope = PipelineScope.Requests)]      // Validation, caching
[Pipeline(Scope = PipelineScope.Streams)]       // Streaming-specific logic
[Pipeline(Scope = PipelineScope.Notifications)] // Event-specific logic
```

### 4. Naming Conventions
```csharp
[Handle(Name = "CreateUser_Database")]    // Implementation-specific
[Handle(Name = "CreateUser_Cache")]       // Source-specific
[Handle(Name = "GetUser_V2")]            // Version-specific
[ExposeAsEndpoint(Version = "2.0")]      // API versioning
```

Bu kapsamlı attribute örneği, Relay Framework'ün tüm attribute özelliklerinin gerçek dünya senaryolarında nasıl kullanıldığını göstermektedir.