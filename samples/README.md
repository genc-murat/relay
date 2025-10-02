# Relay Framework - Sample Projects

This directory contains comprehensive sample projects demonstrating all features of the Relay framework.

## 📚 Sample Projects

### Core Features

#### 1. **StreamingSample** ⭐⭐⭐
Demonstrates `IAsyncEnumerable<T>` streaming support for handling large datasets efficiently.

**Features:**
- Stream request/response pattern
- Backpressure handling
- Cancellation support
- Memory-efficient processing

**Run:**
```bash
cd StreamingSample
dotnet run
```

#### 2. **WorkflowEngineSample** ⭐⭐⭐
Shows how to orchestrate complex business processes with the built-in workflow engine.

**Features:**
- Sequential workflow steps
- Parallel execution
- Conditional steps
- Compensating actions (Saga pattern)
- Workflow state management

**Run:**
```bash
cd WorkflowEngineSample
dotnet run
```

#### 3. **NamedHandlersSample** ⭐⭐
Demonstrates the Strategy pattern with multiple handlers for the same request type.

**Features:**
- Multiple implementation strategies
- Runtime strategy selection
- Performance vs accuracy trade-offs

**Run:**
```bash
cd NamedHandlersSample
dotnet run
```

#### 4. **NotificationPublishingSample** ⭐⭐
Event-driven architecture with parallel and sequential notification dispatch.

**Features:**
- Multiple event handlers
- Priority-based execution
- Parallel/Sequential dispatch modes
- Domain events

**Run:**
```bash
cd NotificationPublishingSample
dotnet run
```

### Resilience Patterns

#### 5. **CircuitBreakerSample** ⭐⭐⭐
Prevents cascading failures with automatic circuit breaker pattern.

**Features:**
- Automatic failure detection
- Circuit state transitions
- Fast-fail mechanism
- Automatic recovery testing

**Run:**
```bash
cd CircuitBreakerSample
dotnet run
```

#### 6. **BulkheadPatternSample** ⭐⭐⭐
Resource isolation and fault tolerance with bulkhead pattern.

**Features:**
- Concurrency limiting
- Request queuing
- Fast-fail for overload
- Resource pool isolation

**Run:**
```bash
cd BulkheadPatternSample
dotnet run
```

### Integration Samples

#### 7. **WebApiIntegrationSample** ⭐⭐
ASP.NET Core Web API integration with Relay.

**Features:**
- RESTful API endpoints
- Swagger/OpenAPI integration
- CRUD operations
- Request validation

**Run:**
```bash
cd WebApiIntegrationSample
dotnet run
# Open http://localhost:5000/swagger
```

#### 8. **BackgroundServiceSample** ⭐⭐
Long-running background workers with Relay integration.

**Features:**
- Hosted services
- Scheduled tasks
- Background processing
- Worker patterns

**Run:**
```bash
cd BackgroundServiceSample
dotnet run
```

#### 9. **GrpcIntegrationSample** ⭐
gRPC services with Relay mediator pattern.

**Features:**
- gRPC server implementation
- Relay handler integration
- Remote procedure calls

**Run:**
```bash
cd GrpcIntegrationSample
dotnet run
```

#### 10. **SignalRIntegrationSample** ⭐
Real-time communication with SignalR and Relay.

**Features:**
- WebSocket communication
- Real-time notifications
- Event broadcasting

**Run:**
```bash
cd SignalRIntegrationSample
dotnet run
# Open http://localhost:5002
```

### Performance Samples

#### 11. **BatchProcessingSample** ⭐
High-performance batch processing with SIMD optimization.

**Features:**
- Standard processing
- SIMD vectorization
- Parallel processing
- Performance benchmarks

**Run:**
```bash
cd BatchProcessingSample
dotnet run
```

#### 12. **ObservabilitySample** ⭐
Monitoring, metrics, and observability features.

**Features:**
- Performance metrics
- Request tracking
- Success/failure rates
- Telemetry data

**Run:**
```bash
cd ObservabilitySample
dotnet run
```

## 🚀 Running All Samples

To run all samples sequentially:

```bash
# Run from samples directory
foreach ($dir in Get-ChildItem -Directory) {
    Write-Host "Running $($dir.Name)..." -ForegroundColor Green
    Push-Location $dir.Name
    dotnet run
    Pop-Location
    Write-Host ""
}
```

## 📖 Learning Path

**Recommended order for learning:**

1. Start with **StreamingSample** - Learn core streaming concepts
2. Try **NamedHandlersSample** - Understand strategy pattern
3. Explore **NotificationPublishingSample** - Event-driven architecture
4. Study **CircuitBreakerSample** - Resilience patterns
5. Practice **BulkheadPatternSample** - Resource isolation
6. Build **WorkflowEngineSample** - Complex orchestration
7. Integrate **WebApiIntegrationSample** - Real-world APIs
8. Scale **BackgroundServiceSample** - Background processing
9. Optimize **BatchProcessingSample** - Performance tuning
10. Monitor **ObservabilitySample** - Production observability

## 🔧 Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 / VS Code / Rider (optional)

## 📚 Additional Resources

- [Main Documentation](../../docs/)
- [API Reference](../../docs/api-documentation.md)
- [Performance Guide](../../docs/performance-guide.md)
- [Testing Guide](../../docs/testing-guide.md)

## 🤝 Contributing

Found an issue or want to improve a sample? Please contribute!

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

MIT License - See LICENSE file for details
