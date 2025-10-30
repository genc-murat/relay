# Backpressure Management Examples

This document provides practical examples of using backpressure management in Relay.MessageBroker.

## Table of Contents

- [Basic Setup](#basic-setup)
- [Monitoring Backpressure](#monitoring-backpressure)
- [Custom Throttling Logic](#custom-throttling-logic)
- [Integration with Metrics](#integration-with-metrics)
- [Production Configuration](#production-configuration)

## Basic Setup

### Simple Configuration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Backpressure;

var services = new ServiceCollection();

services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
    
    // Enable backpressure with default settings
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true
    };
});

var serviceProvider = services.BuildServiceProvider();
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
```

### Custom Thresholds

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.ConnectionString = "localhost:9092";
    
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true,
        LatencyThreshold = TimeSpan.FromSeconds(3),      // Activate at 3s latency
        QueueDepthThreshold = 5000,                       // Activate at 5000 messages
        RecoveryLatencyThreshold = TimeSpan.FromSeconds(1), // Recover at 1s latency
        ThrottleFactor = 0.7                              // 70% reduction
    };
});
```

## Monitoring Backpressure

### Event-Based Monitoring

```csharp
public class BackpressureMonitorService : BackgroundService
{
    private readonly IBackpressureController _backpressureController;
    private readonly ILogger<BackpressureMonitorService> _logger;
    private readonly IMetricsCollector _metricsCollector;

    public BackpressureMonitorService(
        IBackpressureController backpressureController,
        ILogger<BackpressureMonitorService> logger,
        IMetricsCollector metricsCollector)
    {
        _backpressureController = backpressureController;
        _logger = logger;
        _metricsCollector = metricsCollector;

        // Subscribe to backpressure events
        if (_backpressureController is BackpressureController controller)
        {
            controller.BackpressureStateChanged += OnBackpressureStateChanged;
        }
    }

    private void OnBackpressureStateChanged(object? sender, BackpressureEvent e)
    {
        switch (e.EventType)
        {
            case BackpressureEventType.Activated:
                _logger.LogWarning(
                    "⚠️ BACKPRESSURE ACTIVATED: {Reason}. " +
                    "AvgLatency: {AvgLatency}ms, QueueDepth: {QueueDepth}",
                    e.Reason,
                    e.AverageLatency.TotalMilliseconds,
                    e.QueueDepth);
                
                // Send alert
                _metricsCollector.RecordBackpressureActivation(e);
                break;

            case BackpressureEventType.Deactivated:
                _logger.LogInformation(
                    "✅ BACKPRESSURE DEACTIVATED: {Reason}. " +
                    "AvgLatency: {AvgLatency}ms, QueueDepth: {QueueDepth}",
                    e.Reason,
                    e.AverageLatency.TotalMilliseconds,
                    e.QueueDepth);
                
                // Send recovery notification
                _metricsCollector.RecordBackpressureRecovery(e);
                break;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var metrics = _backpressureController.GetMetrics();
            
            _logger.LogDebug(
                "Backpressure Metrics - AvgLatency: {AvgLatency}ms, " +
                "QueueDepth: {QueueDepth}, IsThrottling: {IsThrottling}",
                metrics.AverageLatency.TotalMilliseconds,
                metrics.QueueDepth,
                metrics.IsThrottling);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

### Periodic Metrics Collection

```csharp
public class BackpressureMetricsCollector
{
    private readonly IBackpressureController _backpressureController;
    private readonly ILogger<BackpressureMetricsCollector> _logger;

    public BackpressureMetricsCollector(
        IBackpressureController backpressureController,
        ILogger<BackpressureMetricsCollector> logger)
    {
        _backpressureController = backpressureController;
        _logger = logger;
    }

    public async Task<BackpressureReport> CollectMetricsAsync()
    {
        var metrics = _backpressureController.GetMetrics();

        var report = new BackpressureReport
        {
            Timestamp = DateTimeOffset.UtcNow,
            AverageLatencyMs = metrics.AverageLatency.TotalMilliseconds,
            MinLatencyMs = metrics.MinLatency.TotalMilliseconds,
            MaxLatencyMs = metrics.MaxLatency.TotalMilliseconds,
            QueueDepth = metrics.QueueDepth,
            IsThrottling = metrics.IsThrottling,
            TotalProcessingRecords = metrics.TotalProcessingRecords,
            BackpressureActivations = metrics.BackpressureActivations,
            LastActivation = metrics.LastBackpressureActivation,
            LastDeactivation = metrics.LastBackpressureDeactivation
        };

        _logger.LogInformation(
            "Backpressure Report: AvgLatency={AvgLatency}ms, " +
            "QueueDepth={QueueDepth}, Throttling={Throttling}, " +
            "Activations={Activations}",
            report.AverageLatencyMs,
            report.QueueDepth,
            report.IsThrottling,
            report.BackpressureActivations);

        return report;
    }
}

public class BackpressureReport
{
    public DateTimeOffset Timestamp { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public int QueueDepth { get; set; }
    public bool IsThrottling { get; set; }
    public long TotalProcessingRecords { get; set; }
    public long BackpressureActivations { get; set; }
    public DateTimeOffset? LastActivation { get; set; }
    public DateTimeOffset? LastDeactivation { get; set; }
}
```

## Custom Throttling Logic

### Adaptive Consumer

```csharp
public class AdaptiveMessageConsumer
{
    private readonly IMessageBroker _messageBroker;
    private readonly IBackpressureController _backpressureController;
    private readonly ILogger<AdaptiveMessageConsumer> _logger;
    private int _currentPrefetchCount = 100;

    public AdaptiveMessageConsumer(
        IMessageBroker messageBroker,
        IBackpressureController backpressureController,
        ILogger<AdaptiveMessageConsumer> logger)
    {
        _messageBroker = messageBroker;
        _backpressureController = backpressureController;
        _logger = logger;

        // Subscribe to backpressure events to adjust prefetch
        if (_backpressureController is BackpressureController controller)
        {
            controller.BackpressureStateChanged += OnBackpressureStateChanged;
        }
    }

    private void OnBackpressureStateChanged(object? sender, BackpressureEvent e)
    {
        if (e.EventType == BackpressureEventType.Activated)
        {
            // Reduce prefetch count by 50%
            _currentPrefetchCount = Math.Max(10, _currentPrefetchCount / 2);
            _logger.LogInformation(
                "Reduced prefetch count to {PrefetchCount} due to backpressure",
                _currentPrefetchCount);
        }
        else if (e.EventType == BackpressureEventType.Deactivated)
        {
            // Gradually increase prefetch count
            _currentPrefetchCount = Math.Min(100, _currentPrefetchCount * 2);
            _logger.LogInformation(
                "Increased prefetch count to {PrefetchCount} after backpressure recovery",
                _currentPrefetchCount);
        }
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                // Check if we should throttle before processing
                var shouldThrottle = await _backpressureController.ShouldThrottleAsync(ct);
                
                if (shouldThrottle)
                {
                    // Add delay to reduce consumption rate
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                }

                await ProcessOrderAsync(message, ct);
            },
            new SubscriptionOptions
            {
                PrefetchCount = (ushort)_currentPrefetchCount
            },
            cancellationToken);
    }

    private async Task ProcessOrderAsync(OrderCreatedEvent order, CancellationToken ct)
    {
        // Process order...
        await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
    }
}
```

### Rate-Limited Consumer

```csharp
public class RateLimitedConsumer
{
    private readonly IBackpressureController _backpressureController;
    private readonly ILogger<RateLimitedConsumer> _logger;

    public RateLimitedConsumer(
        IBackpressureController backpressureController,
        ILogger<RateLimitedConsumer> logger)
    {
        _backpressureController = backpressureController;
        _logger = logger;
    }

    public async Task ConsumeWithBackpressureAsync(
        Func<CancellationToken, Task> consumeAction,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var shouldThrottle = await _backpressureController.ShouldThrottleAsync(cancellationToken);

            if (shouldThrottle)
            {
                var metrics = _backpressureController.GetMetrics();
                
                // Calculate delay based on current latency
                var delayMs = CalculateThrottleDelay(metrics);
                
                _logger.LogDebug(
                    "Throttling consumption. Delay: {Delay}ms, AvgLatency: {AvgLatency}ms",
                    delayMs,
                    metrics.AverageLatency.TotalMilliseconds);

                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }

            await consumeAction(cancellationToken);
        }
    }

    private int CalculateThrottleDelay(BackpressureMetrics metrics)
    {
        // More aggressive throttling for higher latency
        var latencyRatio = metrics.AverageLatency.TotalMilliseconds / 5000.0; // 5s threshold
        return (int)(100 * Math.Min(latencyRatio, 5.0)); // Max 500ms delay
    }
}
```

## Integration with Metrics

### OpenTelemetry Integration

```csharp
using OpenTelemetry.Metrics;

public class BackpressureMetricsExporter
{
    private readonly IBackpressureController _backpressureController;
    private readonly Meter _meter;

    public BackpressureMetricsExporter(
        IBackpressureController backpressureController,
        IMeterFactory meterFactory)
    {
        _backpressureController = backpressureController;
        _meter = meterFactory.Create("Relay.MessageBroker.Backpressure");

        // Register observable gauges
        _meter.CreateObservableGauge(
            "backpressure.average_latency_ms",
            () => GetMetrics().AverageLatency.TotalMilliseconds,
            "ms",
            "Average message processing latency");

        _meter.CreateObservableGauge(
            "backpressure.queue_depth",
            () => GetMetrics().QueueDepth,
            "messages",
            "Current queue depth");

        _meter.CreateObservableGauge(
            "backpressure.is_throttling",
            () => GetMetrics().IsThrottling ? 1 : 0,
            "boolean",
            "Whether backpressure throttling is active");

        _meter.CreateObservableCounter(
            "backpressure.activations_total",
            () => GetMetrics().BackpressureActivations,
            "activations",
            "Total number of backpressure activations");
    }

    private BackpressureMetrics GetMetrics()
    {
        return _backpressureController.GetMetrics();
    }
}
```

### Prometheus Metrics

```csharp
public class PrometheusBackpressureMetrics
{
    private readonly IBackpressureController _backpressureController;
    private readonly ILogger<PrometheusBackpressureMetrics> _logger;

    public PrometheusBackpressureMetrics(
        IBackpressureController backpressureController,
        ILogger<PrometheusBackpressureMetrics> logger)
    {
        _backpressureController = backpressureController;
        _logger = logger;
    }

    public string ExportMetrics()
    {
        var metrics = _backpressureController.GetMetrics();
        var sb = new StringBuilder();

        // Average latency
        sb.AppendLine("# HELP backpressure_average_latency_ms Average message processing latency");
        sb.AppendLine("# TYPE backpressure_average_latency_ms gauge");
        sb.AppendLine($"backpressure_average_latency_ms {metrics.AverageLatency.TotalMilliseconds}");

        // Queue depth
        sb.AppendLine("# HELP backpressure_queue_depth Current queue depth");
        sb.AppendLine("# TYPE backpressure_queue_depth gauge");
        sb.AppendLine($"backpressure_queue_depth {metrics.QueueDepth}");

        // Is throttling
        sb.AppendLine("# HELP backpressure_is_throttling Whether backpressure throttling is active");
        sb.AppendLine("# TYPE backpressure_is_throttling gauge");
        sb.AppendLine($"backpressure_is_throttling {(metrics.IsThrottling ? 1 : 0)}");

        // Activations
        sb.AppendLine("# HELP backpressure_activations_total Total backpressure activations");
        sb.AppendLine("# TYPE backpressure_activations_total counter");
        sb.AppendLine($"backpressure_activations_total {metrics.BackpressureActivations}");

        return sb.ToString();
    }
}
```

## Production Configuration

### High-Throughput System

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.ConnectionString = "kafka-cluster:9092";
    
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true,
        LatencyThreshold = TimeSpan.FromSeconds(2),      // Tight latency requirement
        QueueDepthThreshold = 50000,                      // High throughput
        RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(500),
        SlidingWindowSize = 200,                          // Larger window for stability
        ThrottleFactor = 0.3                              // Gentle throttling (30% reduction)
    };
});
```

### Low-Latency System

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://rabbitmq:5672";
    
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true,
        LatencyThreshold = TimeSpan.FromMilliseconds(500), // Very tight latency
        QueueDepthThreshold = 1000,                         // Small queue
        RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(100),
        SlidingWindowSize = 50,                             // Smaller window for fast response
        ThrottleFactor = 0.8                                // Aggressive throttling (80% reduction)
    };
});
```

### Batch Processing System

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.AzureServiceBus;
    options.ConnectionString = "Endpoint=sb://...";
    
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true,
        LatencyThreshold = TimeSpan.FromSeconds(30),      // Longer processing time acceptable
        QueueDepthThreshold = 100000,                      // Large queue for batching
        RecoveryLatencyThreshold = TimeSpan.FromSeconds(10),
        SlidingWindowSize = 500,                           // Very large window for stability
        ThrottleFactor = 0.5                               // Moderate throttling
    };
});
```

### Complete Production Setup

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add message broker with backpressure
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.ConnectionString = Configuration["MessageBroker:ConnectionString"];
            
            options.Backpressure = new BackpressureOptions
            {
                Enabled = true,
                LatencyThreshold = TimeSpan.FromSeconds(5),
                QueueDepthThreshold = 10000,
                RecoveryLatencyThreshold = TimeSpan.FromSeconds(2),
                ThrottleFactor = 0.5
            };

            // Also enable circuit breaker for comprehensive resilience
            options.CircuitBreaker = new CircuitBreakerOptions
            {
                Enabled = true,
                FailureThreshold = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(60)
            };
        });

        // Add monitoring services
        services.AddSingleton<BackpressureMonitorService>();
        services.AddSingleton<BackpressureMetricsCollector>();
        services.AddSingleton<BackpressureMetricsExporter>();

        // Add hosted service for monitoring
        services.AddHostedService<BackpressureMonitorService>();
    }
}
```

## See Also

- [Backpressure README](README.md)
- [Circuit Breaker Examples](../CircuitBreaker/EXAMPLE.md)
- [Bulkhead Examples](../Bulkhead/EXAMPLE.md)
