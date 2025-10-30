# Batch Processing

The Batch Processing feature provides automatic batching of messages to improve throughput and reduce overhead when publishing high volumes of messages.

## Features

- **Automatic Batching**: Messages are automatically collected into batches based on size or time thresholds
- **Compression**: Optional GZip compression for batches to reduce network overhead (achieves 30%+ reduction for JSON)
- **Partial Retry**: Failed messages in a batch can be retried individually
- **Metrics**: Comprehensive metrics for monitoring batch performance
- **Thread-Safe**: Fully thread-safe implementation for concurrent message publishing

## Configuration

### Basic Setup

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    // ... other options
})
.AddMessageBrokerBatching(batchOptions =>
{
    batchOptions.Enabled = true;
    batchOptions.MaxBatchSize = 100;
    batchOptions.FlushInterval = TimeSpan.FromMilliseconds(100);
    batchOptions.EnableCompression = true;
    batchOptions.PartialRetry = true;
});
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | false | Enables or disables batch processing |
| `MaxBatchSize` | int | 100 | Maximum number of messages per batch (1-10000) |
| `FlushInterval` | TimeSpan | 100ms | Time window for automatic batch flushing |
| `EnableCompression` | bool | true | Enables GZip compression for batches |
| `PartialRetry` | bool | true | Retries failed messages individually |

## Usage

### Publishing Messages

When batching is enabled, messages are automatically batched:

```csharp
// Messages are automatically added to batches
for (int i = 0; i < 1000; i++)
{
    await messageBroker.PublishAsync(new MyMessage { Id = i });
}

// Batches are automatically flushed when:
// 1. MaxBatchSize is reached
// 2. FlushInterval expires
// 3. The application shuts down
```

### Manual Flushing

You can manually flush all pending batches:

```csharp
if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
{
    await batchDecorator.FlushAllAsync();
}
```

### Monitoring Metrics

Get batch processing metrics:

```csharp
if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
{
    var metrics = batchDecorator.GetMetrics<MyMessage>();
    
    Console.WriteLine($"Current Batch Size: {metrics.CurrentBatchSize}");
    Console.WriteLine($"Average Batch Size: {metrics.AverageBatchSize}");
    Console.WriteLine($"Total Batches: {metrics.TotalBatchesProcessed}");
    Console.WriteLine($"Success Rate: {metrics.SuccessRate:P}");
    Console.WriteLine($"Compression Ratio: {metrics.CompressionRatio:F2}x");
}
```

## How It Works

### Batching Strategy

1. **Size-Based Flushing**: When the batch reaches `MaxBatchSize`, it's automatically flushed
2. **Time-Based Flushing**: A timer periodically flushes batches based on `FlushInterval`
3. **Shutdown Flushing**: All pending batches are flushed when the application shuts down

### Compression

When compression is enabled:
- All messages in a batch are serialized to JSON
- The batch payload is compressed using GZip with optimal compression level
- Compression metadata is added to message headers
- Typical compression ratios: 2-5x for JSON messages (30%+ size reduction)

### Partial Retry

When a batch publish fails and `PartialRetry` is enabled:
- Each message in the failed batch is retried individually
- Successful messages are counted separately from failed ones
- Failed messages are logged with detailed error information

## Performance Considerations

### Batch Size

- **Small batches (10-50)**: Lower latency, higher overhead
- **Medium batches (100-500)**: Balanced latency and throughput
- **Large batches (1000+)**: Higher throughput, increased latency

### Flush Interval

- **Short intervals (50-100ms)**: Lower latency, more frequent flushes
- **Long intervals (500-1000ms)**: Higher throughput, increased latency

### Compression

- **Enabled**: Reduces network bandwidth, adds CPU overhead
- **Disabled**: No compression overhead, higher network usage

## Best Practices

1. **Tune for Your Workload**: Adjust `MaxBatchSize` and `FlushInterval` based on your message volume and latency requirements
2. **Monitor Metrics**: Use the metrics API to track batch performance and adjust settings
3. **Enable Compression**: For JSON messages, compression typically provides significant benefits
4. **Use Partial Retry**: Enable for better resilience in case of transient failures
5. **Flush on Shutdown**: The decorator automatically flushes on disposal, but you can manually flush for critical scenarios

## Example: High-Throughput Scenario

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
})
.AddMessageBrokerBatching(batchOptions =>
{
    batchOptions.Enabled = true;
    batchOptions.MaxBatchSize = 1000;           // Large batches for throughput
    batchOptions.FlushInterval = TimeSpan.FromSeconds(1);  // 1 second window
    batchOptions.EnableCompression = true;       // Reduce network usage
    batchOptions.PartialRetry = true;           // Resilience
});

// Publish 100,000 messages
for (int i = 0; i < 100_000; i++)
{
    await messageBroker.PublishAsync(new Event { Id = i, Data = "..." });
}
```

## Example: Low-Latency Scenario

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
})
.AddMessageBrokerBatching(batchOptions =>
{
    batchOptions.Enabled = true;
    batchOptions.MaxBatchSize = 10;             // Small batches for low latency
    batchOptions.FlushInterval = TimeSpan.FromMilliseconds(50);  // Quick flush
    batchOptions.EnableCompression = false;     // Skip compression overhead
    batchOptions.PartialRetry = true;
});
```

## Troubleshooting

### Messages Not Being Published

- Check if batching is enabled: `batchOptions.Enabled = true`
- Verify the batch hasn't reached `MaxBatchSize` or `FlushInterval`
- Manually flush: `await batchDecorator.FlushAllAsync()`

### High Latency

- Reduce `MaxBatchSize` for smaller batches
- Reduce `FlushInterval` for more frequent flushing
- Disable compression if CPU is a bottleneck

### Low Throughput

- Increase `MaxBatchSize` for larger batches
- Increase `FlushInterval` for less frequent flushing
- Enable compression to reduce network overhead

## Requirements Satisfied

This implementation satisfies the following requirements:

- **4.1**: Configurable batch sizes between 1 and 10000 messages
- **4.2**: Automatic flushing based on batch size or time window
- **4.3**: Batch compression achieving 30%+ reduction for JSON messages
- **4.4**: Partial retry of failed messages within a batch
- **4.5**: Comprehensive metrics including batch size, processing time, and success rate
