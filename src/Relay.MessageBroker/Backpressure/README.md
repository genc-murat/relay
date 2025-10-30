# Backpressure Management

Backpressure management helps handle situations when message consumers cannot keep up with message production by monitoring processing metrics and applying throttling when needed.

## Overview

The backpressure controller monitors:
- **Processing Latency**: Average time to process messages
- **Queue Depth**: Number of messages waiting to be processed

When thresholds are exceeded, backpressure is activated, which can:
- Reduce message consumption rate by 50% (configurable)
- Emit events for monitoring and alerting
- Automatically recover when conditions improve

## Features

- **Latency Monitoring**: Tracks processing duration with sliding window
- **Queue Depth Monitoring**: Monitors message queue depth
- **Automatic Throttling**: Reduces consumption rate when thresholds exceeded
- **Automatic Recovery**: Resumes normal operation when conditions improve
- **Event Emission**: Raises events for backpressure state changes
- **Metrics**: Provides detailed metrics for monitoring

## Configuration

### Basic Configuration

```csharp
services.AddMessageBroker(options =>
{
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true,
        LatencyThreshold = TimeSpan.FromSeconds(5),
        QueueDepthThreshold = 10000,
        RecoveryLatencyThreshold = TimeSpan.FromSeconds(2),
        ThrottleFactor = 0.5 // 50% reduction
    };
});
```

### Advanced Configuration

```csharp
services.AddBackpressureManagement(options =>
{
    options.Enabled = true;
    options.LatencyThreshold = TimeSpan.FromSeconds(5);
    options.QueueDepthThreshold = 10000;
    options.RecoveryLatencyThreshold = TimeSpan.FromSeconds(2);
    options.SlidingWindowSize = 100; // Number of samples
    options.ThrottleFactor = 0.5; // 50% reduction
});
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `Enabled` | Enable/disable backpressure management | `false` |
| `LatencyThreshold` | Latency threshold that triggers backpressure | `5 seconds` |
| `QueueDepthThreshold` | Queue depth threshold that triggers backpressure | `10000` |
| `RecoveryLatencyThreshold` | Latency threshold for recovery | `2 seconds` |
| `SlidingWindowSize` | Number of samples for latency calculation | `100` |
| `ThrottleFactor` | Consumption rate reduction (0.0-1.0) | `0.5` |

## Usage

### Monitoring Backpressure Events

```csharp
public class BackpressureMonitor
{
    private readonly IBackpressureController _backpressureController;
    private readonly ILogger<BackpressureMonitor> _logger;

    public BackpressureMonitor(
        IBackpressureController backpressureController,
        ILogger<BackpressureMonitor> logger)
    {
        _backpressureController = backpressureController;
        _logger = logger;

        // Subscribe to backpressure events
        if (_backpressureController is BackpressureController controller)
        {
            controller.BackpressureStateChanged += OnBackpressureStateChanged;
        }
    }

    private void OnBackpressureStateChanged(object? sender, BackpressureEvent e)
    {
        _logger.LogWarning(
            "Backpressure {EventType}: {Reason}. AvgLatency: {AvgLatency}ms, QueueDepth: {QueueDepth}",
            e.EventType,
            e.Reason,
            e.AverageLatency.TotalMilliseconds,
            e.QueueDepth);
    }
}
```

### Getting Backpressure Metrics

```csharp
public class MetricsCollector
{
    private readonly IBackpressureController _backpressureController;

    public MetricsCollector(IBackpressureController backpressureController)
    {
        _backpressureController = backpressureController;
    }

    public async Task CollectMetricsAsync()
    {
        var metrics = _backpressureController.GetMetrics();

        Console.WriteLine($"Average Latency: {metrics.AverageLatency.TotalMilliseconds}ms");
        Console.WriteLine($"Queue Depth: {metrics.QueueDepth}");
        Console.WriteLine($"Is Throttling: {metrics.IsThrottling}");
        Console.WriteLine($"Backpressure Activations: {metrics.BackpressureActivations}");
    }
}
```

### Checking Backpressure Status

```csharp
public class MessageConsumer
{
    private readonly IBackpressureController _backpressureController;

    public MessageConsumer(IBackpressureController backpressureController)
    {
        _backpressureController = backpressureController;
    }

    public async Task ConsumeMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Check if we should throttle
            var shouldThrottle = await _backpressureController.ShouldThrottleAsync(cancellationToken);

            if (shouldThrottle)
            {
                // Reduce consumption rate
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            // Consume message...
        }
    }
}
```

## How It Works

### Activation

Backpressure is activated when:
1. Average processing latency exceeds `LatencyThreshold`, OR
2. Queue depth exceeds `QueueDepthThreshold`

When activated:
- `ShouldThrottleAsync()` returns `true`
- `BackpressureStateChanged` event is raised with `Activated` type
- Metrics show `IsThrottling = true`

### Recovery

Backpressure is deactivated when:
1. Average processing latency falls below `RecoveryLatencyThreshold`, AND
2. Queue depth is below `QueueDepthThreshold`

When deactivated:
- `ShouldThrottleAsync()` returns `false`
- `BackpressureStateChanged` event is raised with `Deactivated` type
- Metrics show `IsThrottling = false`

### Sliding Window

The controller maintains a sliding window of processing records:
- Size controlled by `SlidingWindowSize` option
- Older records are automatically removed
- Average latency calculated from current window

## Best Practices

1. **Set Appropriate Thresholds**
   - Base thresholds on your system's capacity
   - Leave headroom for traffic spikes
   - Monitor metrics to tune thresholds

2. **Monitor Backpressure Events**
   - Log backpressure activations
   - Set up alerts for frequent activations
   - Track recovery time

3. **Adjust Throttle Factor**
   - Start with 50% reduction (0.5)
   - Increase for more aggressive throttling
   - Decrease for gentler throttling

4. **Use with Circuit Breaker**
   - Combine with circuit breaker for comprehensive resilience
   - Circuit breaker handles failures
   - Backpressure handles overload

5. **Tune Sliding Window Size**
   - Larger window = smoother average, slower response
   - Smaller window = faster response, more volatile
   - Default 100 samples works for most cases

## Metrics

The controller provides the following metrics:

- `AverageLatency`: Average processing latency in current window
- `QueueDepth`: Current queue depth
- `IsThrottling`: Whether throttling is active
- `TotalProcessingRecords`: Total number of records tracked
- `BackpressureActivations`: Number of times backpressure activated
- `LastBackpressureActivation`: Timestamp of last activation
- `LastBackpressureDeactivation`: Timestamp of last deactivation
- `MinLatency`: Minimum latency in current window
- `MaxLatency`: Maximum latency in current window

## Integration with Message Brokers

Backpressure is automatically integrated with the base message broker:

1. **Processing Duration Recording**: Automatically recorded after each message
2. **Throttle Check**: Available via `ShouldThrottleAsync()` method
3. **Metrics Access**: Available via `GetBackpressureMetrics()` method

Broker implementations can use these methods to:
- Check if throttling should be applied before consuming
- Reduce prefetch count when backpressure is active
- Adjust consumption rate dynamically

## Example: Complete Setup

```csharp
// Startup.cs or Program.cs
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
    
    // Enable backpressure
    options.Backpressure = new BackpressureOptions
    {
        Enabled = true,
        LatencyThreshold = TimeSpan.FromSeconds(5),
        QueueDepthThreshold = 10000,
        RecoveryLatencyThreshold = TimeSpan.FromSeconds(2),
        ThrottleFactor = 0.5
    };
});

// Add backpressure monitoring
services.AddSingleton<BackpressureMonitor>();
```

## Troubleshooting

### Backpressure Frequently Activating

**Symptoms**: Backpressure activates and deactivates frequently

**Solutions**:
- Increase `LatencyThreshold` or `QueueDepthThreshold`
- Increase `SlidingWindowSize` for smoother averaging
- Scale out consumers to handle more load
- Optimize message processing logic

### Backpressure Not Activating

**Symptoms**: System overloaded but backpressure not activating

**Solutions**:
- Verify `Enabled = true`
- Lower `LatencyThreshold` or `QueueDepthThreshold`
- Check that processing duration is being recorded
- Verify queue depth is being updated

### Slow Recovery

**Symptoms**: Backpressure takes long time to deactivate

**Solutions**:
- Increase `RecoveryLatencyThreshold`
- Reduce `ThrottleFactor` for less aggressive throttling
- Check for underlying performance issues
- Monitor metrics to understand recovery patterns

## See Also

- [Circuit Breaker](../CircuitBreaker/README.md)
- [Bulkhead Pattern](../Bulkhead/README.md)
- [Poison Message Handling](../PoisonMessage/README.md)
