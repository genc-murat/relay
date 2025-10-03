# Relay.MessageBroker - Advanced Features

## 🚀 Overview

Relay.MessageBroker now includes four advanced features to take your message-based applications to the next level:

1. **Circuit Breaker Pattern** - Protect your services from cascading failures
2. **Message Compression** - Reduce bandwidth and improve performance
3. **OpenTelemetry Integration** - Full observability with distributed tracing
4. **Saga Pattern** - Orchestrate complex distributed transactions

---

## 1. Circuit Breaker Pattern

### Overview
The Circuit Breaker pattern prevents cascading failures in distributed systems by monitoring operation failures and temporarily blocking requests when a threshold is reached.

### Features
- **Three States**: Closed, Open, Half-Open
- **Configurable Thresholds**: Failure count and failure rate
- **Slow Call Detection**: Track and act on slow operations
- **Automatic Recovery**: Half-open state for testing service health
- **Real-time Metrics**: Monitor circuit breaker performance

### Configuration

```csharp
services.AddRelayMessageBroker(options =>
{
    options.CircuitBreaker = new CircuitBreakerOptions
    {
        Enabled = true,
        FailureThreshold = 5,              // Open after 5 failures
        SuccessThreshold = 2,              // Close after 2 successes
        Timeout = TimeSpan.FromSeconds(60), // Wait 60s before half-open
        FailureRateThreshold = 0.5,        // 50% failure rate threshold
        TrackSlowCalls = true,
        SlowCallDurationThreshold = TimeSpan.FromSeconds(5),
        OnStateChanged = args =>
        {
            Console.WriteLine($"Circuit breaker state changed: {args.PreviousState} -> {args.NewState}");
            Console.WriteLine($"Reason: {args.Reason}");
        }
    };
});
```

### Usage

```csharp
var circuitBreaker = new CircuitBreaker(options);

try
{
    await circuitBreaker.ExecuteAsync(async ct =>
    {
        await messageBroker.PublishAsync(message, ct);
    }, cancellationToken);
}
catch (CircuitBreakerOpenException ex)
{
    // Circuit is open, handle accordingly
    _logger.LogWarning("Circuit breaker is open: {Message}", ex.Message);
}
```

### Metrics

```csharp
var metrics = circuitBreaker.Metrics;
Console.WriteLine($"Total Calls: {metrics.TotalCalls}");
Console.WriteLine($"Failed Calls: {metrics.FailedCalls}");
Console.WriteLine($"Failure Rate: {metrics.FailureRate:P}");
Console.WriteLine($"Slow Calls: {metrics.SlowCalls}");
```

---

## 2. Message Compression

### Overview
Message compression reduces message size for better network utilization and cost savings.

### Supported Algorithms
- **GZip** - Standard compression (default)
- **Deflate** - Similar to GZip
- **Brotli** - Higher compression ratio
- **LZ4** - Faster compression/decompression (requires additional package)
- **Zstandard (Zstd)** - Balanced speed and ratio (requires additional package)

### Configuration

```csharp
services.AddRelayMessageBroker(options =>
{
    options.Compression = new CompressionOptions
    {
        Enabled = true,
        Algorithm = CompressionAlgorithm.Brotli,
        Level = 6,                          // 0-9, higher = better compression
        MinimumSizeBytes = 1024,            // Only compress messages > 1KB
        AutoDetectCompressed = true,
        AddMetadataHeaders = true,
        TrackStatistics = true,
        
        // Don't compress already compressed formats
        NonCompressibleContentTypes = new List<string>
        {
            "image/jpeg",
            "video/mp4",
            "application/zip"
        }
    };
});
```

### Usage

The compression is applied automatically when publishing messages:

```csharp
// Large message will be automatically compressed
var largeMessage = new OrderCreatedEvent
{
    OrderId = orderId,
    Items = GenerateLargeItemsList(), // > 1KB
    Metadata = GetExtensiveMetadata()
};

await messageBroker.PublishAsync(largeMessage);
```

### Compression Statistics

```csharp
var stats = compressionService.GetStatistics();
Console.WriteLine($"Total Messages: {stats.TotalMessages}");
Console.WriteLine($"Compressed: {stats.CompressedMessages} ({stats.CompressionRate:P})");
Console.WriteLine($"Bytes Saved: {stats.TotalBytesSaved:N0} bytes");
Console.WriteLine($"Average Compression Ratio: {stats.AverageCompressionRatio:P}");
Console.WriteLine($"Avg Compression Time: {stats.AverageCompressionTime.TotalMilliseconds}ms");
```

### Performance Comparison

| Algorithm | Compression Ratio | Speed | Use Case |
|-----------|------------------|-------|----------|
| GZip | ~70% | Fast | General purpose |
| Deflate | ~70% | Fast | General purpose |
| Brotli | ~75% | Medium | Better compression |
| LZ4 | ~60% | Very Fast | Real-time processing |
| Zstd | ~72% | Fast | Balanced |

---

## 3. OpenTelemetry Integration

### Overview
Full observability with distributed tracing, metrics, and logging following OpenTelemetry standards.

### Features
- **Distributed Tracing**: Track messages across services
- **Metrics Collection**: Monitor message broker performance
- **Context Propagation**: Trace context across message boundaries
- **Multiple Exporters**: OTLP, Jaeger, Zipkin, Prometheus, and more
- **Automatic Instrumentation**: Minimal code changes required

### Configuration

```csharp
services.AddRelayMessageBroker(options =>
{
    options.Telemetry = new TelemetryOptions
    {
        Enabled = true,
        EnableTracing = true,
        EnableMetrics = true,
        EnableLogging = true,
        
        ServiceName = "OrderService",
        ServiceVersion = "1.0.0",
        ServiceNamespace = "Production",
        
        // Security
        CaptureMessagePayloads = false,     // Don't log sensitive data
        CaptureMessageHeaders = true,
        ExcludedHeaderKeys = new List<string>
        {
            "Authorization",
            "X-API-Key"
        },
        
        // Sampling
        SamplingRate = 1.0,                 // 100% sampling
        
        // Exporters
        Exporters = new TelemetryExportersOptions
        {
            EnableOtlp = true,
            OtlpEndpoint = "http://localhost:4317",
            OtlpProtocol = "grpc",
            
            EnableJaeger = true,
            JaegerAgentHost = "localhost",
            JaegerAgentPort = 6831,
            
            EnablePrometheus = true,
            PrometheusEndpoint = "/metrics"
        }
    };
});
```

### Available Metrics

```csharp
// Counters
- relay.messages.published
- relay.messages.received
- relay.messages.processed
- relay.messages.failed
- relay.messages.retried
- relay.messages.compressed
- relay.messages.decompressed

// Histograms
- relay.message.publish.duration
- relay.message.process.duration
- relay.message.payload.size
- relay.message.compression.ratio

// Gauges
- relay.circuit_breaker.state
- relay.connections.active
- relay.queue.size
```

### Custom Instrumentation

```csharp
using var activity = MessageBrokerTelemetry.ActivitySource.StartActivity(
    "ProcessOrder",
    ActivityKind.Consumer);

activity?.SetTag(MessageBrokerTelemetry.Attributes.MessageType, "OrderCreated");
activity?.SetTag(MessageBrokerTelemetry.Attributes.MessagingOperation, "process");

try
{
    await ProcessOrderAsync(order);
    activity?.SetStatus(ActivityStatusCode.Ok);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.RecordException(ex);
    throw;
}
```

### Trace Context Propagation

```csharp
// Publishing service
await messageBroker.PublishAsync(new OrderCreatedEvent
{
    OrderId = orderId
    // Trace context is automatically propagated in message headers
});

// Consuming service - trace context is automatically extracted
[Handle]
public async ValueTask HandleAsync(OrderCreatedEvent message, ...)
{
    // This handler runs in the same trace as the publisher
    // Spans are automatically created and linked
}
```

---

## 4. Saga Pattern

### Overview
The Saga pattern manages long-running transactions across multiple services with automatic compensation on failure.

### Features
- **Orchestration-based Sagas**: Centralized saga coordinator
- **Automatic Compensation**: Rollback on failure
- **State Persistence**: Resume sagas after crashes
- **Telemetry Integration**: Full observability
- **Flexible Step Definition**: Easy to compose complex workflows

### Saga Definition

```csharp
public class OrderSagaData : SagaDataBase
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ShipmentId { get; set; }
}

public class OrderSaga : Saga<OrderSagaData>
{
    public OrderSaga(
        IPaymentService paymentService,
        IInventoryService inventoryService,
        IShippingService shippingService)
    {
        AddStep(new ReserveInventoryStep(inventoryService))
            .AddStep(new ProcessPaymentStep(paymentService))
            .AddStep(new CreateShipmentStep(shippingService));
    }
}
```

### Saga Step Implementation

```csharp
public class ProcessPaymentStep : SagaStep<OrderSagaData>
{
    private readonly IPaymentService _paymentService;

    public ProcessPaymentStep(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public override async ValueTask ExecuteAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        // Execute forward action
        var paymentId = await _paymentService.ProcessPaymentAsync(
            data.OrderId,
            data.TotalAmount,
            cancellationToken);
        
        data.PaymentId = paymentId;
    }

    public override async ValueTask CompensateAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        // Compensate (rollback) action
        if (data.PaymentId != Guid.Empty)
        {
            await _paymentService.RefundPaymentAsync(
                data.PaymentId,
                cancellationToken);
        }
    }
}
```

### Configuration

```csharp
services.AddRelayMessageBroker(options =>
{
    options.Saga = new SagaOptions
    {
        Enabled = true,
        DefaultTimeout = TimeSpan.FromMinutes(30),
        AutoPersist = true,
        PersistenceInterval = TimeSpan.FromSeconds(5),
        
        AutoRetryFailedSteps = true,
        MaxRetryAttempts = 3,
        RetryDelay = TimeSpan.FromSeconds(5),
        UseExponentialBackoff = true,
        
        AutoCompensateOnFailure = true,
        ContinueCompensationOnError = true,
        
        EnableTelemetry = true,
        TrackMetrics = true,
        
        OnSagaCompleted = args =>
        {
            _logger.LogInformation(
                "Saga {SagaId} completed in {Duration}ms with {Steps} steps",
                args.SagaId,
                args.Duration.TotalMilliseconds,
                args.StepsExecuted);
        },
        
        OnSagaFailed = args =>
        {
            _logger.LogError(
                args.Exception,
                "Saga {SagaId} failed at step {Step}",
                args.SagaId,
                args.FailedStep);
        }
    };
});

// Register saga persistence
services.AddSingleton<ISagaPersistence<OrderSagaData>, InMemorySagaPersistence<OrderSagaData>>();
```

### Executing a Saga

```csharp
var saga = new OrderSaga(paymentService, inventoryService, shippingService);

var sagaData = new OrderSagaData
{
    OrderId = orderId,
    TotalAmount = 99.99m,
    CorrelationId = correlationId
};

var result = await saga.ExecuteAsync(sagaData, cancellationToken);

if (result.IsSuccess)
{
    _logger.LogInformation("Order saga completed successfully");
}
else
{
    _logger.LogError(
        result.Exception,
        "Order saga failed at step {Step}. Compensation: {Compensated}",
        result.FailedStep,
        result.CompensationSucceeded ? "Succeeded" : "Failed");
}
```

### Saga States

```csharp
public enum SagaState
{
    NotStarted,      // Initial state
    Running,         // Executing steps
    Compensating,    // Rolling back
    Completed,       // Successfully completed
    Compensated,     // Rolled back successfully
    Failed,          // Failed without compensation
    Aborted         // Manually aborted
}
```

### Saga Persistence

```csharp
// Save saga state
await sagaPersistence.SaveAsync(sagaData);

// Retrieve saga by ID
var saga = await sagaPersistence.GetByIdAsync(sagaId);

// Retrieve saga by correlation ID
var saga = await sagaPersistence.GetByCorrelationIdAsync(correlationId);

// Get all active sagas
await foreach (var activeSaga in sagaPersistence.GetActiveSagasAsync())
{
    // Resume or monitor active sagas
}

// Get sagas by state
await foreach (var failedSaga in sagaPersistence.GetByStateAsync(SagaState.Failed))
{
    // Handle failed sagas
}
```

---

## 🔧 Complete Example

Here's a complete example using all four features:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.Compression;
using Relay.MessageBroker.Telemetry;
using Relay.MessageBroker.Saga;

var services = new ServiceCollection();

services.AddRelayMessageBroker(options =>
{
    // Basic configuration
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };

    // Circuit Breaker
    options.CircuitBreaker = new CircuitBreakerOptions
    {
        Enabled = true,
        FailureThreshold = 5,
        SuccessThreshold = 2,
        Timeout = TimeSpan.FromSeconds(60),
        OnStateChanged = args => 
            Console.WriteLine($"Circuit: {args.PreviousState} -> {args.NewState}")
    };

    // Compression
    options.Compression = new CompressionOptions
    {
        Enabled = true,
        Algorithm = CompressionAlgorithm.Brotli,
        Level = 6,
        MinimumSizeBytes = 1024,
        TrackStatistics = true
    };

    // OpenTelemetry
    options.Telemetry = new TelemetryOptions
    {
        Enabled = true,
        ServiceName = "MyService",
        EnableTracing = true,
        EnableMetrics = true,
        Exporters = new TelemetryExportersOptions
        {
            EnableOtlp = true,
            OtlpEndpoint = "http://localhost:4317",
            EnablePrometheus = true
        }
    };

    // Saga
    options.Saga = new SagaOptions
    {
        Enabled = true,
        DefaultTimeout = TimeSpan.FromMinutes(30),
        AutoCompensateOnFailure = true,
        EnableTelemetry = true
    };
});

var serviceProvider = services.BuildServiceProvider();
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();

// Use the message broker with all features enabled
await messageBroker.StartAsync();
```

---

## 📊 Monitoring Dashboard

All four features integrate seamlessly with monitoring tools:

### Grafana Dashboard Metrics

```promql
# Circuit Breaker State
relay_circuit_breaker_state{name="message_broker"}

# Message Compression Ratio
rate(relay_message_compression_ratio[5m])

# Saga Success Rate
rate(relay_saga_completed_total[5m]) / rate(relay_saga_started_total[5m])

# Message Processing Duration
histogram_quantile(0.95, rate(relay_message_process_duration_bucket[5m]))
```

---

## 🧪 Testing

### Unit Tests Example

```csharp
[Fact]
public async Task CircuitBreaker_OpensAfterThresholdFailures()
{
    // Arrange
    var options = new CircuitBreakerOptions
    {
        FailureThreshold = 3
    };
    var circuitBreaker = new CircuitBreaker(options);

    // Act - Simulate 3 failures
    for (int i = 0; i < 3; i++)
    {
        try
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                throw new Exception("Simulated failure");
            });
        }
        catch { }
    }

    // Assert
    Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
}

[Fact]
public async Task Compression_ReducesMessageSize()
{
    // Arrange
    var compressor = new BrotliMessageCompressor(level: 6);
    var originalData = Encoding.UTF8.GetBytes(new string('A', 10000));

    // Act
    var compressed = await compressor.CompressAsync(originalData);

    // Assert
    Assert.True(compressed.Length < originalData.Length);
    Assert.True((double)compressed.Length / originalData.Length < 0.5);
}

[Fact]
public async Task Saga_CompensatesOnFailure()
{
    // Arrange
    var saga = new TestSaga();
    var data = new TestSagaData();

    // Act
    var result = await saga.ExecuteAsync(data);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(SagaState.Compensated, result.Data.State);
    Assert.True(result.CompensationSucceeded);
}
```

---

## 📚 Additional Resources

- [Circuit Breaker Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)
- [Saga Pattern](https://microservices.io/patterns/data/saga.html)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Message Compression Best Practices](https://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html)

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## 📝 License

This project is licensed under the MIT License.
