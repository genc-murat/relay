# Relay Framework - Sample Projects

This directory contains comprehensive sample projects demonstrating all features of the Relay framework.

## 📚 Sample Projects

### Core Features

#### 1. **SagaPatternSample** ⭐⭐⭐ 🆕
Demonstrates distributed transaction management with automatic compensation using Saga pattern.

**Features:**
- Multi-step distributed transactions
- Automatic compensation on failure
- State persistence (In-Memory & Database)
- Complete e-commerce order processing example
- Timeout handling

**Run:**
```bash
cd SagaPatternSample/SagaPatternSample
dotnet run
# Open https://localhost:7xxx/swagger
```

#### 2. **StreamingSample** ⭐⭐⭐
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

#### 3. **MessageCompressionSample** ⭐⭐ 🆕
Demonstrates automatic message compression for large payloads to reduce bandwidth.

**Features:**
- Automatic compression/decompression
- Configurable compression threshold
- Multiple compression algorithms (Gzip, Brotli, Deflate)
- Compression statistics and ratios
- Performance optimization

**Run:**
```bash
cd MessageCompressionSample
dotnet run
```

#### 4. **OpenTelemetrySample** ⭐⭐⭐ 🆕
Comprehensive OpenTelemetry integration for distributed tracing and observability.

**Features:**
- Distributed tracing across handlers
- Automatic ASP.NET Core instrumentation
- Custom activity sources and spans
- Metrics collection and Prometheus export
- Error tracking and exception recording
- Jaeger integration

**Run:**
```bash
cd OpenTelemetrySample
dotnet run
# Open https://localhost:7xxx/swagger
# Metrics: https://localhost:7xxx/metrics
```

#### 5. **WorkflowEngineSample** ⭐⭐⭐
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

#### 6. **NamedHandlersSample** ⭐⭐
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

#### 7. **NotificationPublishingSample** ⭐⭐
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

#### 8. **CircuitBreakerSample** ⭐⭐⭐
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

#### 9. **BulkheadPatternSample** ⭐⭐⭐
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

#### 10. **MessageBroker.Sample** ⭐⭐⭐ 🆕
Comprehensive message broker integration with multiple providers (RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, Redis).

**Features:**
- Multiple broker provider support
- Event-driven architecture
- Publish/Subscribe patterns
- Message acknowledgment
- Error handling and retries
- Dead letter queues

**Run:**
```bash
cd MessageBroker.Sample
dotnet run -- --broker rabbitmq  # or kafka, azure, aws, nats, redis
```

#### 11. **AwsSqsSnsMessageBrokerSample** ⭐⭐ 🆕
AWS SQS and SNS integration for cloud-native event messaging.

**Features:**
- AWS SQS queue subscriptions
- AWS SNS topic publishing
- LocalStack support for development
- Dead letter queue configuration
- IAM role integration

**Run:**
```bash
cd AwsSqsSnsMessageBrokerSample
# Start LocalStack for development
docker run -d -p 4566:4566 localstack/localstack
dotnet run
```

#### 12. **AzureServiceBusMessageBrokerSample** ⭐⭐ 🆕
Azure Service Bus integration for enterprise messaging.

**Features:**
- Azure Service Bus queues and topics
- Session-based messaging
- Message scheduling
- Duplicate detection
- Transaction support

**Run:**
```bash
cd AzureServiceBusMessageBrokerSample
dotnet run
```

#### 13. **WebApiIntegrationSample** ⭐⭐
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

#### 11. **BackgroundServiceSample** ⭐⭐
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

#### 12. **GrpcIntegrationSample** ⭐
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

#### 13. **SignalRIntegrationSample** ⭐
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

#### 14. **BatchProcessingSample** ⭐
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

#### 15. **ObservabilitySample** ⭐
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
6. Master **SagaPatternSample** 🆕 - Distributed transactions
7. Build **WorkflowEngineSample** - Complex orchestration
8. Integrate **WebApiIntegrationSample** - Real-world APIs
9. Scale **BackgroundServiceSample** - Background processing
10. Optimize **BatchProcessingSample** - Performance tuning
11. Optimize **MessageCompressionSample** 🆕 - Bandwidth optimization
12. Monitor **ObservabilitySample** - Basic observability
13. Advanced **OpenTelemetrySample** 🆕 - Distributed tracing

## 🆕 New Features (Recently Added)

### 1. Saga Pattern Integration
Complete distributed transaction management with automatic compensation. Perfect for:
- Multi-step business transactions
- E-commerce order processing
- Microservices coordination
- Automatic rollback on failures

### 2. Message Compression
Automatic compression for large messages to reduce bandwidth:
- Gzip, Brotli, and Deflate algorithms
- Configurable compression threshold
- Transparent compression/decompression
- Performance optimization

### 3. OpenTelemetry Integration
Industry-standard observability with OpenTelemetry:
- Distributed tracing across services
- Automatic instrumentation
- Prometheus metrics export
- Jaeger integration
- Custom spans and events

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
