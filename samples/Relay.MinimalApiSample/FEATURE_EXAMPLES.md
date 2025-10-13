# Relay Framework - Feature Examples

This document is a comprehensive guide to all feature examples in the Relay.MinimalApiSample project.

## 🎯 Overview

Relay Framework is a powerful mediator framework for modern .NET applications. These examples demonstrate the framework's core features with real-world scenarios.

## 📚 Table of Contents

1. [Validation - Request Validation](#1-validation)
2. [Pre/Post Processors - Pre/Post Processing](#2-prepost-processors)
3. [Exception Handling - Error Management](#3-exception-handling)
4. [Pipeline Behaviors - Pipeline Behaviors](#4-pipeline-behaviors)
5. [Caching - Caching](#5-caching)
6. [Notifications/Events - Notifications](#6-notificationsevents)
7. [Streaming - Streaming Operations](#7-streaming)
8. [Transactions - Transaction Management](#8-transactions)

---

## 1. Validation

### 📖 Description
Automatic request validation system. Validates requests before handlers execute.

### ✨ Features
- Automatic validation
- Comprehensive error messages
- Regex-based validation
- Async validation support

### 🎯 Use Case
**User Registration**
- Username: 3-50 characters, alphanumeric
- Email: Valid email format
- Password: 8+ characters, uppercase, lowercase, number, special character
- Age: Between 18-120

### 🔗 Endpoint
```http
POST /api/examples/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "age": 25
}
```

### 📝 Code Example
```csharp
public class RegisterUserValidator : IValidationRule<RegisterUserRequest>
{
    public ValueTask<IEnumerable<string>> ValidateAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("Username is required");

        if (request.Username.Length < 3)
            errors.Add("Username must be at least 3 characters");

        // ... other rules

        return ValueTask.FromResult<IEnumerable<string>>(errors);
    }
}
```

### ✅ Success Response (201 Created)
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "john_doe",
  "message": "User registered successfully!"
}
```

### ❌ Error Response (400 Bad Request)
```json
{
  "isValid": false,
  "errors": [
    "Username must be at least 3 characters",
    "Email must be a valid email address",
    "Password must be at least 8 characters",
    "User must be at least 18 years old"
  ]
}
```

### 📍 Detailed Documentation
[Features/Examples/01-Validation/README.md](Features/Examples/01-Validation/README.md)

---

## 2. Pre/Post Processors

### 📖 Description
Processors that run before and after handler execution. Ideal for cross-cutting concerns.

### ✨ Features
- Request pre-processing
- Response post-processing
- Audit logging
- Data transformation

### 🎯 Use Case
**Order Processing**
- Pre: Stock validation, price calculation
- Post: Audit log, email notification

### 📝 Code Example
```csharp
// Pre-processor
public class OrderPreProcessor : IRequestPreProcessor<CreateOrderRequest>
{
    public ValueTask ProcessAsync(CreateOrderRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Pre-processing order for customer: {CustomerId}",
            request.CustomerId);
        return default;
    }
}

// Post-processor
public class OrderPostProcessor : IRequestPostProcessor<CreateOrderRequest, OrderResponse>
{
    public ValueTask ProcessAsync(
        CreateOrderRequest request,
        OrderResponse response,
        CancellationToken ct)
    {
        _auditService.LogOrderCreation(response.OrderId);
        return default;
    }
}
```

---

## 3. Exception Handling

### 📖 Description
Comprehensive error management system. Catches exceptions and provides graceful fallback.

### ✨ Features
- Exception handlers (suppress exceptions)
- Exception actions (for side effects)
- Graceful degradation
- Detailed error logging

### 🎯 Use Case
**Payment Processing**
- InsufficientFundsException: Insufficient balance
- PaymentTimeoutException: Timeout error
- Return fallback response

### 📝 Code Example
```csharp
public class InsufficientFundsHandler
    : IRequestExceptionHandler<ProcessPaymentRequest, PaymentResult, InsufficientFundsException>
{
    public ValueTask<ExceptionHandlerResult<PaymentResult>> HandleAsync(
        ProcessPaymentRequest request,
        InsufficientFundsException exception,
        CancellationToken cancellationToken)
    {
        // Return fallback response
        var result = new PaymentResult(
            Success: false,
            Message: "Insufficient funds"
        );

        return ValueTask.FromResult(
            ExceptionHandlerResult<PaymentResult>.Handle(result)
        );
    }
}
```

---

## 4. Pipeline Behaviors

### 📖 Description
Pipeline behaviors that run for all requests. For common operations like logging, timing, caching.

### ✨ Features
- Request/response intercepting
- Performance monitoring
- Logging
- Custom cross-cutting concerns

### 📝 Code Example
```csharp
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        _logger.LogInformation("Handled {RequestType} in {Elapsed}ms",
            typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

---

## 5. Caching

### 📖 Description
Smart caching system. Caches responses, improves performance.

### ✨ Features
- In-memory caching
- Distributed caching (Redis)
- Cache invalidation
- TTL (Time To Live) support

### 🎯 Use Case
**Product List**
- First request: Comes from database (slow)
- Subsequent requests: Comes from cache (fast)
- Cache duration: 5 minutes

### 📝 Code Example
```csharp
[CacheResponse(Duration = 300)] // 5 minutes
public record GetProductsRequest : IRequest<List<Product>>;

// Or programmatically
services.AddRelayCaching(options =>
{
    options.DefaultDuration = TimeSpan.FromMinutes(5);
    options.EnableDistributedCache = true;
});
```

---

## 6. Notifications/Events

### 📖 Description
Event-driven architecture. Event publishing with multiple handler support.

### ✨ Features
- Event publishing
- Multiple handlers
- Sequential/Parallel execution
- Priority-based handling

### 🎯 Use Case
**User Creation**
- UserCreatedEvent is published
- Email handler: Sends welcome email
- Analytics handler: Logs user registration
- Notification handler: Sends admin notification

### 📝 Code Example
```csharp
// Event definition
public record UserCreatedEvent(Guid UserId, string Email) : INotification;

// Handler 1
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedEvent>
{
    [Notification(Priority = 1)]
    public async ValueTask HandleAsync(UserCreatedEvent notification, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email);
    }
}

// Handler 2
public class LogAnalyticsHandler : INotificationHandler<UserCreatedEvent>
{
    [Notification(Priority = 0, DispatchMode = NotificationDispatchMode.Parallel)]
    public async ValueTask HandleAsync(UserCreatedEvent notification, CancellationToken ct)
    {
        await _analyticsService.TrackUserCreatedAsync(notification.UserId);
    }
}

// Publish event
await _relay.PublishAsync(new UserCreatedEvent(userId, email));
```

---

## 7. Streaming

### 📖 Description
Streaming operations for large datasets. `IAsyncEnumerable` support.

### ✨ Features
- Server-Sent Events
- Memory-efficient processing
- Backpressure handling
- Real-time data streaming

### 🎯 Use Case
**Log Streaming**
- Real-time log streaming
- Large file processing
- Database cursors

### 📝 Code Example
```csharp
public record StreamLogsRequest(DateTime StartDate) : IStreamRequest<LogEntry>;

public class StreamLogsHandler : IStreamRequestHandler<StreamLogsRequest, LogEntry>
{
    public async IAsyncEnumerable<LogEntry> HandleAsync(
        StreamLogsRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var log in _logRepository.StreamLogsAsync(request.StartDate, cancellationToken))
        {
            yield return log;

            if (cancellationToken.IsCancellationRequested)
                yield break;
        }
    }
}

// Usage
await foreach (var log in relay.StreamAsync(new StreamLogsRequest(DateTime.Today)))
{
    Console.WriteLine(log.Message);
}
```

---

## 8. Transactions

### 📖 Description
Transaction management and Unit of Work pattern. ACID guarantees.

### ✨ Features
- Automatic transaction management
- ACID guarantees
- Rollback on error
- EF Core integration

### 🎯 Use Case
**Order Creation**
- Add to Order table
- Deduct from Inventory
- Create Payment record
- Rollback all on error

### 📝 Code Example
```csharp
// Transaction marker interface
public record CreateOrderCommand(Order Order)
    : IRequest<OrderResult>,
      ITransactionalRequest<OrderResult>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResult>
{
    public async ValueTask<OrderResult> HandleAsync(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Create Order
        _dbContext.Orders.Add(request.Order);

        // 2. Update Inventory
        var product = await _dbContext.Products.FindAsync(request.Order.ProductId);
        product.Stock -= request.Order.Quantity;

        // 3. Payment record
        _dbContext.Payments.Add(new Payment { OrderId = request.Order.Id });

        // SaveChanges is called automatically
        // Transaction is committed automatically
        // Automatic rollback on error

        return new OrderResult(request.Order.Id, "Success");
    }
}

// Registration
services.AddRelayTransactions();
services.AddRelayUnitOfWork();
```

---

## 🚀 Quick Start

### 1. Run the Project

```bash
cd samples/Relay.MinimalApiSample
dotnet run
```

### 2. Open Swagger UI

```
https://localhost:5001/swagger
```

### 3. Find "Feature Examples" Section

All examples are located under the "Feature Examples" tag in Swagger UI.

---

## 🎓 Learning Path

### Beginner Level
1. ✅ [Validation](#1-validation) - Basic validation
2. ✅ [Pipeline Behaviors](#4-pipeline-behaviors) - Logging, timing

### Intermediate Level
3. ✅ [Pre/Post Processors](#2-prepost-processors) - Pre/post processing
4. ✅ [Exception Handling](#3-exception-handling) - Error management
5. ✅ [Caching](#5-caching) - Caching

### Advanced Level
6. ✅ [Notifications/Events](#6-notificationsevents) - Event-driven
7. ✅ [Streaming](#7-streaming) - Large data
8. ✅ [Transactions](#8-transactions) - Transaction management

---

## 📊 Comparison Table

| Feature | When to Use | Performance | Complexity |
|---------|------------|-------------|------------|
| Validation | Every request | ⚡⚡⚡ Very Fast | 🟢 Easy |
| Pre/Post Processors | Cross-cutting | ⚡⚡ Fast | 🟡 Medium |
| Exception Handling | Error management | ⚡⚡⚡ Very Fast | 🟢 Easy |
| Pipeline Behaviors | Common operations | ⚡⚡ Fast | 🟡 Medium |
| Caching | Read-heavy | ⚡⚡⚡ Very Fast | 🟡 Medium |
| Notifications | Event-driven | ⚡⚡ Fast | 🟡 Medium |
| Streaming | Large data | ⚡ Normal | 🔴 Hard |
| Transactions | Write operations | ⚡ Normal | 🔴 Hard |

---

## 💡 Best Practices

### ✅ Do's

1. **Validation**: Create validator for every request
2. **Pre/Post**: Use for audit logging
3. **Exception Handling**: Provide graceful degradation
4. **Pipeline Behaviors**: For common operations (logging, timing)
5. **Caching**: For read-heavy operations
6. **Notifications**: Use events for loose coupling
7. **Streaming**: Prefer streaming for large datasets
8. **Transactions**: Use transactions for write operations

### ❌ Don'ts

1. ❌ Validation logic in handlers
2. ❌ Try-catch blocks everywhere
3. ❌ Caching write operations
4. ❌ Streaming for small datasets
5. ❌ Transactions for read operations

---

## 🔗 Related Resources

- [Relay Documentation](../../README.md)
- [Minimal API Sample README](README.md)
- [Controller API Sample](../Relay.ControllerApiSample/README.md)
- [Feature Examples Directory](Features/Examples/README.md)

---

## 🤝 Contributing

Want to add a new example?

1. Fork the repository
2. Create a new feature branch
3. Add your example (Request/Response/Handler/README)
4. Submit a PR

---

## 📄 License

These examples are licensed under the same license as the Relay framework (MIT).

---

**Relay Framework** - Modern, high-performance mediator framework for .NET

🚀 Happy coding!
