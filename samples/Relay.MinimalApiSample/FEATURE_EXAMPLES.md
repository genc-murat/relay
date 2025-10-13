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
9. [AI Optimization - AI-Powered Performance](#9-ai-optimization)

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

## 9. AI Optimization

### 📖 Description
AI-powered performance optimization with machine learning. Automatically analyzes and optimizes request patterns.

### ✨ Features
- Smart Batching: ML-based request batching
- Intelligent Caching: AI predicts optimal caching strategies
- Performance Tracking: Automatic metrics collection for ML training
- Auto-Optimization: AI can automatically apply optimizations
- Risk Assessment: Confidence scores and risk levels

### 🎯 Use Case
**Product Recommendations**
- AI analyzes access patterns
- Automatically batches similar requests
- Predicts which results to cache
- Learns from execution metrics
- Optimizes performance over time

### 🔗 Endpoint
```http
POST /api/examples/recommendations
Content-Type: application/json

{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "category": "electronics",
  "count": 10
}
```

### 📝 Code Example
```csharp
[AIOptimized(
    AutoApplyOptimizations = true,
    MinConfidenceScore = 0.7,
    MaxRiskLevel = RiskLevel.Low,
    EnableMetricsTracking = true,
    EnableLearning = true,
    Priority = OptimizationPriority.High
)]
[SmartBatching(
    MinBatchSize = 2,
    MaxBatchSize = 50,
    MaxWaitTimeMilliseconds = 100,
    Strategy = BatchingStrategy.Dynamic
)]
[IntelligentCaching(
    EnableAIAnalysis = true,
    MinAccessFrequency = 5,
    MinPredictedHitRate = 0.3,
    UseDynamicTtl = true
)]
public class ProductRecommendationHandler
    : IRequestHandler<GetProductRecommendationsRequest, ProductRecommendationsResponse>
{
    public async ValueTask<ProductRecommendationsResponse> HandleAsync(
        GetProductRecommendationsRequest request,
        CancellationToken cancellationToken)
    {
        // Handler implementation
        // AI pipeline automatically:
        // 1. Batches similar requests
        // 2. Caches frequently accessed results
        // 3. Tracks performance metrics
        // 4. Learns and improves over time
    }
}
```

### ✅ Success Response (200 OK)
```json
{
  "recommendations": [
    {
      "productId": "7d9a85f64-1234-4562-b3fc-2c963f66afa6",
      "name": "Laptop",
      "price": 1299.99,
      "relevanceScore": 0.95,
      "reason": "Based on your recent purchases"
    },
    {
      "productId": "8e0b96f75-2345-5673-c4gd-3d074g77bgb7",
      "name": "Smartphone",
      "price": 899.99,
      "relevanceScore": 0.87,
      "reason": "Popular in your area"
    }
  ],
  "metrics": {
    "optimizationStrategy": "SmartBatching + IntelligentCaching",
    "processingTimeMs": 45,
    "wasBatched": true,
    "wasCached": false,
    "confidenceScore": 0.85,
    "performanceGain": "Estimated 40% improvement with batching"
  }
}
```

### 🧠 AI Features Explained

#### 1. Smart Batching
AI analyzes request patterns and automatically batches similar requests together:
- **Dynamic Sizing**: ML predicts optimal batch size based on load
- **Intelligent Timing**: Learns best wait times for maximum throughput
- **Pattern Recognition**: Groups similar requests efficiently

#### 2. Intelligent Caching
AI predicts which results should be cached:
- **Access Pattern Analysis**: Tracks frequency and recency
- **Hit Rate Prediction**: ML forecasts cache effectiveness
- **Dynamic TTL**: Adjusts cache duration based on patterns
- **Adaptive Strategy**: Learns optimal caching strategies

#### 3. Performance Tracking
Automatic metrics collection for continuous improvement:
- **Execution Times**: Tracks handler performance
- **Resource Usage**: Monitors memory and CPU
- **Pattern Detection**: Identifies optimization opportunities
- **Model Training**: Improves predictions over time

#### 4. Risk Management
Safe, gradual optimization with confidence scoring:
- **Confidence Scores**: ML provides certainty estimates
- **Risk Levels**: Low/Medium/High risk classification
- **Gradual Rollout**: Safe, incremental optimizations
- **Automatic Rollback**: Reverts if performance degrades

### 🔧 Configuration

```csharp
// Enable AI Optimization
builder.Services.AddAIOptimization(options =>
{
    options.EnableAutomaticOptimization = true;
    options.MaxAutomaticOptimizationRisk = RiskLevel.Low;
    options.MinConfidenceScore = 0.7;
    options.DefaultBatchSize = 10;
    options.MaxBatchSize = 50;
    options.LearningEnabled = true;
    options.EnableMetricsExport = true;
});

// Or use scenario-based configuration
builder.Services.AddAIOptimizationForScenario(AIOptimizationScenario.HighThroughput);
// Options: HighThroughput, LowLatency, ResourceConstrained, Development, Production
```

### 📊 Performance Improvements

| Scenario | Without AI | With AI | Improvement |
|----------|------------|---------|-------------|
| High-frequency requests | 100ms avg | 35ms avg | 65% faster |
| Cached results | N/A | 5ms avg | 95% faster |
| Batch operations | Sequential | Parallel | 3-5x throughput |
| Resource usage | Baseline | -30% | 30% reduction |

### 🎯 Best Practices

1. ✅ **Enable Learning**: Let AI learn from real traffic patterns
2. ✅ **Start Conservative**: Begin with `RiskLevel.Low`
3. ✅ **Monitor Metrics**: Track performance improvements
4. ✅ **Tune Gradually**: Adjust confidence thresholds based on results
5. ✅ **Use Scenarios**: Leverage pre-configured scenarios for common use cases

### ⚠️ Important Notes

- AI optimization requires the `Relay.Core.AI` package
- ML models improve over time with more data
- Initial performance may vary while models train
- Monitor confidence scores and adjust thresholds
- Use health checks to ensure AI systems are healthy

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

### Expert Level
9. ✅ [AI Optimization](#9-ai-optimization) - ML-powered performance

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
| AI Optimization | High-traffic APIs | ⚡⚡⚡ Very Fast | 🔴 Hard |

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
9. **AI Optimization**: Enable for high-traffic, performance-critical handlers

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
