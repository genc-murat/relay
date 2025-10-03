# 🚀 Relay MessageBroker - Quick Start Guide

## Hızlı Başlangıç

### 1. Temel Kurulum

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;

var services = new ServiceCollection();

services.AddRelayMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };
});

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();
```

---

## 2. Circuit Breaker Kullanımı

### Basit Kullanım
```csharp
services.AddRelayMessageBroker(options =>
{
    options.CircuitBreaker = new CircuitBreakerOptions
    {
        Enabled = true,
        FailureThreshold = 5,           // 5 hatadan sonra devreyi aç
        SuccessThreshold = 2,           // 2 başarıdan sonra devreyi kapat
        Timeout = TimeSpan.FromSeconds(60)
    };
});
```

### Event Handling
```csharp
options.CircuitBreaker = new CircuitBreakerOptions
{
    Enabled = true,
    OnStateChanged = args =>
    {
        Console.WriteLine($"Circuit State: {args.PreviousState} → {args.NewState}");
        Console.WriteLine($"Reason: {args.Reason}");
        Console.WriteLine($"Failed Calls: {args.Metrics?.FailedCalls}");
    },
    OnRejected = args =>
    {
        Console.WriteLine($"Request rejected! Circuit is {args.CurrentState}");
    }
};
```

### Manuel Kontrol
```csharp
var circuitBreaker = serviceProvider.GetRequiredService<ICircuitBreaker>();

// Metrikler
var metrics = circuitBreaker.Metrics;
Console.WriteLine($"Failure Rate: {metrics.FailureRate:P}");

// Manuel reset
circuitBreaker.Reset();

// Manuel isolation
circuitBreaker.Isolate();
```

---

## 3. Message Compression Kullanımı

### Basit Kullanım
```csharp
services.AddRelayMessageBroker(options =>
{
    options.Compression = new CompressionOptions
    {
        Enabled = true,
        Algorithm = CompressionAlgorithm.Brotli,
        Level = 6,
        MinimumSizeBytes = 1024         // Sadece > 1KB mesajları sıkıştır
    };
});
```

### Gelişmiş Konfigürasyon
```csharp
options.Compression = new CompressionOptions
{
    Enabled = true,
    Algorithm = CompressionAlgorithm.Brotli,
    Level = 9,                          // Maksimum sıkıştırma
    MinimumSizeBytes = 512,
    AutoDetectCompressed = true,
    TrackStatistics = true,
    
    // JSON ve text sıkıştır, medya dosyalarını sıkıştırma
    NonCompressibleContentTypes = new List<string>
    {
        "image/jpeg",
        "image/png",
        "video/mp4",
        "application/zip"
    }
};
```

### İstatistikler
```csharp
var stats = compressionService.GetStatistics();

Console.WriteLine($"Total Messages: {stats.TotalMessages}");
Console.WriteLine($"Compressed: {stats.CompressedMessages} ({stats.CompressionRate:P})");
Console.WriteLine($"Bytes Saved: {stats.TotalBytesSaved:N0}");
Console.WriteLine($"Avg Compression Ratio: {stats.AverageCompressionRatio:P}");
Console.WriteLine($"Avg Compression Time: {stats.AverageCompressionTime.TotalMilliseconds}ms");
```

---

## 4. OpenTelemetry Kullanımı

### Basit Kullanım
```csharp
services.AddRelayMessageBroker(options =>
{
    options.Telemetry = new TelemetryOptions
    {
        Enabled = true,
        ServiceName = "OrderService",
        EnableTracing = true,
        EnableMetrics = true,
        Exporters = new TelemetryExportersOptions
        {
            EnableOtlp = true,
            OtlpEndpoint = "http://localhost:4317"
        }
    };
});
```

### Production Setup
```csharp
options.Telemetry = new TelemetryOptions
{
    Enabled = true,
    ServiceName = "ProductionService",
    ServiceVersion = "1.0.0",
    ServiceNamespace = "Production",
    
    // Güvenlik
    CaptureMessagePayloads = false,     // Hassas veri loglanmaz
    CaptureMessageHeaders = true,
    ExcludedHeaderKeys = new List<string>
    {
        "Authorization",
        "X-API-Key",
        "Password"
    },
    
    // Sampling
    SamplingRate = 0.1,                 // %10 sampling
    
    // Multiple exporters
    Exporters = new TelemetryExportersOptions
    {
        // OTLP
        EnableOtlp = true,
        OtlpEndpoint = "http://otel-collector:4317",
        OtlpProtocol = "grpc",
        
        // Jaeger
        EnableJaeger = true,
        JaegerAgentHost = "jaeger",
        JaegerAgentPort = 6831,
        
        // Prometheus
        EnablePrometheus = true,
        PrometheusEndpoint = "/metrics",
        
        // Azure Monitor
        EnableAzureMonitor = true,
        AzureMonitorConnectionString = "InstrumentationKey=..."
    }
};
```

### Custom Metrics
```csharp
using var activity = MessageBrokerTelemetry.ActivitySource.StartActivity(
    "ProcessOrder",
    ActivityKind.Consumer);

activity?.SetTag("order.id", orderId);
activity?.SetTag("order.total", totalAmount);

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

---

## 5. Saga Pattern Kullanımı

### Saga Data Definition
```csharp
public class OrderSagaData : SagaDataBase
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid PaymentId { get; set; }
    public Guid InventoryReservationId { get; set; }
    public Guid ShipmentId { get; set; }
}
```

### Saga Step Implementation
```csharp
public class ProcessPaymentStep : SagaStep<OrderSagaData>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPaymentStep> _logger;

    public ProcessPaymentStep(IPaymentService paymentService, ILogger<ProcessPaymentStep> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public override async ValueTask ExecuteAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment for order {OrderId}", data.OrderId);
        
        var paymentId = await _paymentService.ProcessPaymentAsync(
            data.OrderId,
            data.TotalAmount,
            cancellationToken);
        
        data.PaymentId = paymentId;
        _logger.LogInformation("Payment processed: {PaymentId}", paymentId);
    }

    public override async ValueTask CompensateAsync(
        OrderSagaData data, 
        CancellationToken cancellationToken)
    {
        if (data.PaymentId != Guid.Empty)
        {
            _logger.LogWarning("Compensating payment {PaymentId}", data.PaymentId);
            
            await _paymentService.RefundPaymentAsync(
                data.PaymentId,
                cancellationToken);
            
            _logger.LogInformation("Payment refunded: {PaymentId}", data.PaymentId);
        }
    }
}
```

### Saga Orchestration
```csharp
public class OrderSaga : Saga<OrderSagaData>
{
    public OrderSaga(
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IShippingService shippingService,
        ILogger<OrderSaga> logger)
    {
        AddStep(new ReserveInventoryStep(inventoryService, logger))
            .AddStep(new ProcessPaymentStep(paymentService, logger))
            .AddStep(new CreateShipmentStep(shippingService, logger));
    }
}
```

### Configuration & Usage
```csharp
// Configuration
services.AddRelayMessageBroker(options =>
{
    options.Saga = new SagaOptions
    {
        Enabled = true,
        DefaultTimeout = TimeSpan.FromMinutes(30),
        AutoPersist = true,
        AutoRetryFailedSteps = true,
        MaxRetryAttempts = 3,
        AutoCompensateOnFailure = true,
        EnableTelemetry = true,
        
        OnSagaCompleted = args =>
        {
            Console.WriteLine($"✓ Saga {args.SagaId} completed in {args.Duration}");
        },
        
        OnSagaFailed = args =>
        {
            Console.WriteLine($"✗ Saga {args.SagaId} failed at {args.FailedStep}");
        },
        
        OnSagaCompensated = args =>
        {
            Console.WriteLine($"↻ Saga {args.SagaId} compensated {args.StepsCompensated} steps");
        }
    };
});

// Register persistence
services.AddSingleton<ISagaPersistence<OrderSagaData>, InMemorySagaPersistence<OrderSagaData>>();

// Execute saga
var saga = serviceProvider.GetRequiredService<OrderSaga>();
var sagaData = new OrderSagaData
{
    OrderId = orderId,
    TotalAmount = 99.99m,
    CorrelationId = correlationId
};

var result = await saga.ExecuteAsync(sagaData, cancellationToken);

if (result.IsSuccess)
{
    Console.WriteLine("✓ Order processed successfully!");
}
else
{
    Console.WriteLine($"✗ Order failed: {result.Exception?.Message}");
    Console.WriteLine($"Compensation: {(result.CompensationSucceeded ? "Success" : "Failed")}");
}
```

---

## 6. Tüm Özellikleri Birlikte Kullanma

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.Compression;
using Relay.MessageBroker.Telemetry;
using Relay.MessageBroker.Saga;

var services = new ServiceCollection();

// Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Message Broker with all features
services.AddRelayMessageBroker(options =>
{
    // Basic broker configuration
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest",
        PrefetchCount = 10
    };

    // 1. Circuit Breaker - Koruma
    options.CircuitBreaker = new CircuitBreakerOptions
    {
        Enabled = true,
        FailureThreshold = 5,
        SuccessThreshold = 2,
        Timeout = TimeSpan.FromSeconds(60),
        FailureRateThreshold = 0.5,
        TrackSlowCalls = true,
        SlowCallDurationThreshold = TimeSpan.FromSeconds(5),
        OnStateChanged = args =>
        {
            Console.WriteLine($"🔴 Circuit: {args.PreviousState} → {args.NewState}");
        }
    };

    // 2. Compression - Optimizasyon
    options.Compression = new CompressionOptions
    {
        Enabled = true,
        Algorithm = CompressionAlgorithm.Brotli,
        Level = 6,
        MinimumSizeBytes = 1024,
        TrackStatistics = true,
        NonCompressibleContentTypes = new List<string>
        {
            "image/jpeg", "video/mp4", "application/zip"
        }
    };

    // 3. OpenTelemetry - Gözlemlenebilirlik
    options.Telemetry = new TelemetryOptions
    {
        Enabled = true,
        ServiceName = "OrderService",
        ServiceVersion = "1.0.0",
        EnableTracing = true,
        EnableMetrics = true,
        EnableLogging = true,
        CaptureMessagePayloads = false,
        Exporters = new TelemetryExportersOptions
        {
            EnableOtlp = true,
            OtlpEndpoint = "http://localhost:4317",
            EnableJaeger = true,
            EnablePrometheus = true,
            PrometheusEndpoint = "/metrics"
        }
    };

    // 4. Saga - Dağıtık İşlemler
    options.Saga = new SagaOptions
    {
        Enabled = true,
        DefaultTimeout = TimeSpan.FromMinutes(30),
        AutoPersist = true,
        AutoRetryFailedSteps = true,
        MaxRetryAttempts = 3,
        RetryDelay = TimeSpan.FromSeconds(5),
        UseExponentialBackoff = true,
        AutoCompensateOnFailure = true,
        EnableTelemetry = true,
        OnSagaCompleted = args =>
        {
            Console.WriteLine($"✓ Saga completed in {args.Duration.TotalSeconds}s");
        }
    };
});

// Register saga persistence
services.AddSingleton<ISagaPersistence<OrderSagaData>, InMemorySagaPersistence<OrderSagaData>>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Start message broker
var broker = serviceProvider.GetRequiredService<IMessageBroker>();
await broker.StartAsync();

Console.WriteLine("🚀 Relay MessageBroker started with all features!");
Console.WriteLine("  ✓ Circuit Breaker");
Console.WriteLine("  ✓ Compression (Brotli)");
Console.WriteLine("  ✓ OpenTelemetry (OTLP, Jaeger, Prometheus)");
Console.WriteLine("  ✓ Saga Pattern");
```

---

## 7. Monitoring & Metrics

### Prometheus Metrics Endpoint

Prometheus metrics otomatik olarak expose edilir:

```
GET http://localhost:5000/metrics
```

### Sample Queries

```promql
# Message processing rate
rate(relay_messages_processed_total[5m])

# Circuit breaker state
relay_circuit_breaker_state

# Compression ratio
avg(relay_message_compression_ratio)

# Message processing duration (95th percentile)
histogram_quantile(0.95, rate(relay_message_process_duration_bucket[5m]))

# Failed messages
rate(relay_messages_failed_total[5m])
```

### Grafana Dashboard

Dashboard panelleri:
- Message throughput (messages/sec)
- Processing duration (p50, p95, p99)
- Circuit breaker state timeline
- Compression statistics
- Saga success/failure rate
- Error rate

---

## 8. Best Practices

### Circuit Breaker
```csharp
✓ FailureThreshold: 5-10 arası
✓ Timeout: 30-60 saniye
✓ TrackSlowCalls: true (performans sorunlarını yakala)
✓ OnStateChanged callback kullan (alerting için)
```

### Compression
```csharp
✓ MinimumSizeBytes: 1024 (1KB) - küçük mesajları sıkıştırma
✓ Brotli kullan (en iyi oran)
✓ NonCompressibleContentTypes ayarla (gereksiz CPU kullanımı)
✓ TrackStatistics: true (optimizasyon için)
```

### OpenTelemetry
```csharp
✓ CaptureMessagePayloads: false (güvenlik)
✓ SamplingRate: Production'da 0.1 (10%)
✓ Multiple exporters (OTLP + Prometheus)
✓ ExcludedHeaderKeys ile sensitive data koru
```

### Saga
```csharp
✓ AutoCompensateOnFailure: true
✓ MaxRetryAttempts: 3
✓ UseExponentialBackoff: true
✓ Persistence kullan (crash recovery için)
✓ Timeout ayarla (dead-lock önle)
```

---

## 9. Troubleshooting

### Circuit Breaker Sürekli Açılıyor
```csharp
// Threshold'ları artır
FailureThreshold = 10
FailureRateThreshold = 0.7

// Timeout'u uzat
Timeout = TimeSpan.FromSeconds(120)
```

### Compression Çalışmıyor
```csharp
// Minimum size kontrol et
MinimumSizeBytes = 512  // Daha küçük mesajlar için

// Content-type kontrol et
CompressibleContentTypes = new List<string> { "application/json" }
```

### Telemetry Data Gözükmüyor
```csharp
// Sampling rate kontrol et
SamplingRate = 1.0  // %100 (test için)

// Exporter bağlantısı kontrol et
OtlpEndpoint = "http://localhost:4317"
EnableConsole = true  // Console'da görüntüle
```

### Saga Compensation Başarısız
```csharp
// Continue on error aktif et
ContinueCompensationOnError = true

// Compensation timeout'u artır
CompensationTimeout = TimeSpan.FromMinutes(5)

// Retry mekanizması
AutoRetryFailedSteps = true
MaxRetryAttempts = 3
```

---

## 10. Resources

### Dokümantasyon
- `ADVANCED_FEATURES.md` - Detaylı kullanım kılavuzu
- `MESSAGE_BROKER_TAMAMLANDI.md` - Türkçe özet
- OpenTelemetry: https://opentelemetry.io/docs/

### GitHub
- Issues: Report bugs
- Discussions: Ask questions
- Pull Requests: Contribute

### Patterns
- Circuit Breaker: https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker
- Saga: https://microservices.io/patterns/data/saga.html

---

Made with ❤️ for Relay Framework
