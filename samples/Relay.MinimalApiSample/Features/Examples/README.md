# Relay Framework - Feature Examples

This directory contains minimal API examples demonstrating all features of the Relay framework.

## 📋 Available Examples

### 1. ✅ [Validation](01-Validation/README.md)
Automatic request validation system
- Comprehensive validation rules
- Regex-based validation
- Error message management

**Example:** User registration (username, email, password validation)

### 2. 🔄 [Pre/Post Processors](02-PrePostProcessors/README.md)
Pre and post-execution processors
- Request pre-processing
- Response post-processing
- Audit logging
- Data transformation

**Example:** Order processing (pre: validation, post: logging)

### 3. ⚠️ [Exception Handling](03-ExceptionHandling/README.md)
Comprehensive error management
- Exception handlers
- Exception actions
- Graceful fallback
- Error logging

**Example:** Payment processing (insufficient funds error)

### 4. 🔧 [Pipeline Behaviors](04-PipelineBehaviors/README.md)
Pipeline behaviors
- Logging behavior
- Timing behavior
- Custom cross-cutting concerns

**Example:** Request/response logging and performance measurement

### 5. 💾 [Caching](05-Caching/README.md)
Smart caching system
- Response caching
- Cache invalidation
- Distributed caching
- Cache strategies

**Example:** Product list caching

### 6. 📢 [Notifications/Events](06-Notifications/README.md)
Event-driven architecture
- Event publishing
- Multiple handlers
- Sequential/Parallel execution
- Event priorities

**Example:** User created event (email, analytics)

### 7. 🌊 [Streaming](07-Streaming/README.md)
Streaming operations (IAsyncEnumerable)
- Server-Sent Events
- Real-time data streaming
- Backpressure handling

**Example:** Log streaming, large datasets

### 8. 💼 [Transactions](08-Transactions/README.md)
Transaction management and Unit of Work
- Automatic transactions
- ACID guarantees
- Rollback on error
- EF Core integration

**Example:** Order creation (multi-table updates)

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

### 3. Test the Examples

Find and test the relevant endpoint for each example in Swagger UI.

## 📊 Endpoints

| Feature | Method | Endpoint | Description |
|---------|--------|----------|-------------|
| Validation | POST | `/api/examples/register` | User registration |
| Pre/Post | POST | `/api/examples/orders` | Order processing |
| Exception | POST | `/api/examples/payment` | Payment processing |
| Pipeline | GET | `/api/examples/profile/{id}` | Get profile |
| Caching | GET | `/api/examples/products` | Cached product list |
| Notifications | POST | `/api/examples/user-created` | Publish event |
| Streaming | GET | `/api/examples/logs/stream` | Log streaming |
| Transactions | POST | `/api/examples/order-transaction` | Order with transaction |

## 🎯 Usage Scenarios

### Scenario 1: User Registration (Validation + Events)

```bash
# 1. Register user (validation runs)
POST /api/examples/register

# 2. UserCreated event is automatically published
#    - Email is sent
#    - Analytics are recorded
```

### Scenario 2: Order Processing (Transactions + Pre/Post)

```bash
# 1. Create order
POST /api/examples/order-transaction

# 2. Pre-processor: Stock validation
# 3. Transaction begins
# 4. Order is saved
# 5. Stock is updated
# 6. Transaction commits
# 7. Post-processor: Logging
```

### Scenario 3: Data Streaming (Streaming + Caching)

```bash
# 1. Get cached data
GET /api/examples/products

# 2. Get real-time log stream
GET /api/examples/logs/stream
```

## 🔧 Setup

### 1. Install Relay Packages

```bash
dotnet add package Relay.Core
```

### 2. Configure in Program.cs

```csharp
// Enable all features
builder.Services.AddRelay();
builder.Services
    .AddRelayValidation()
    .AddRelayPrePostProcessors()
    .AddRelayExceptionHandlers()
    .AddRelayCaching()
    .AddRelayTransactions();
```

### 3. Add Endpoints

```csharp
// Validation endpoint
app.MapPost("/api/examples/register", async (RegisterUserRequest request, IRelay relay) =>
{
    var response = await relay.SendAsync(request);
    return Results.Created($"/api/users/{response.UserId}", response);
});
```

## 📚 Learning Path

1. **Beginner**: [01-Validation](01-Validation/README.md)
2. **Intermediate**: [02-PrePostProcessors](02-PrePostProcessors/README.md), [04-PipelineBehaviors](04-PipelineBehaviors/README.md)
3. **Advanced**: [05-Caching](05-Caching/README.md), [08-Transactions](08-Transactions/README.md)
4. **Expert**: [07-Streaming](07-Streaming/README.md), [06-Notifications](06-Notifications/README.md)

## 🎓 Best Practices

1. ✅ **Validation**: Create validator for every request
2. ✅ **Pre/Post Processors**: Use for cross-cutting concerns
3. ✅ **Exception Handling**: Provide graceful degradation
4. ✅ **Pipeline Behaviors**: For common operations like logging, timing
5. ✅ **Caching**: Use cache for read-heavy operations
6. ✅ **Notifications**: For event-driven architecture
7. ✅ **Streaming**: Prefer streaming for large datasets
8. ✅ **Transactions**: Use transactions for write operations

## 🔗 Related Resources

- [Relay Documentation](../../../README.md)
- [Minimal API Sample](../../README.md)
- [Controller API Sample](../../../Relay.ControllerApiSample/README.md)

## 💡 Tips

### Performance
- ✅ Validation: Checks before handler executes
- ✅ Caching: Caches responses
- ✅ Streaming: Memory-efficient large data processing

### Reliability
- ✅ Exception Handling: Catches errors and provides fallback
- ✅ Transactions: Provides ACID guarantees
- ✅ Validation: Invalid data never reaches handler

### Maintainability
- ✅ Pre/Post Processors: Prevents code duplication
- ✅ Pipeline Behaviors: Separates cross-cutting concerns
- ✅ Notifications: Provides loose coupling

## 🤝 Contributing

Want to add a new example? Send a PR!

Each example should include:
- ✅ Request/Response/Handler
- ✅ Validator (if needed)
- ✅ README.md (descriptions and test scenarios)
- ✅ Real-world use case

## 📄 License

These examples are licensed under the same license as the Relay framework (MIT).
