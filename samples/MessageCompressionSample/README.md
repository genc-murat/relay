# Message Compression Sample

This sample demonstrates the **message compression feature** in Relay MessageBroker, showing how to automatically compress large messages to reduce bandwidth and improve performance.

## Overview

Message compression automatically compresses messages that exceed a configurable threshold, reducing:
- Network bandwidth usage
- Storage requirements
- Transmission time for large payloads

The compression is **transparent** - publishers and subscribers don't need to handle compression/decompression manually.

## Features Demonstrated

- ✅ Automatic message compression for large payloads
- ✅ Configurable compression threshold
- ✅ Transparent compression/decompression
- ✅ Multiple compression algorithms (Gzip, Brotli, Deflate)
- ✅ Compression statistics and ratios
- ✅ Performance optimization

## How It Works

1. **Publish**: When a message exceeds the compression threshold, it's automatically compressed
2. **Transport**: The compressed message is transmitted
3. **Receive**: The message is automatically decompressed when received
4. **Process**: The subscriber receives the original, uncompressed message

## Configuration

### Basic Setup

```csharp
services.AddRelayMessageBroker(options =>
{
    options.UseInMemory();
    options.EnableCompression = true;
    options.CompressionThreshold = 1024; // Compress messages > 1KB
});
```

### Compression Options

```csharp
services.AddRelayMessageBroker(options =>
{
    options.UseInMemory();
    
    // Enable compression
    options.EnableCompression = true;
    
    // Set threshold (bytes) - only compress messages larger than this
    options.CompressionThreshold = 1024; // 1KB
    
    // Choose compression algorithm
    options.CompressionAlgorithm = CompressionAlgorithm.Gzip; // Default
    // options.CompressionAlgorithm = CompressionAlgorithm.Brotli; // Better ratio
    // options.CompressionAlgorithm = CompressionAlgorithm.Deflate; // Faster
    
    // Set compression level
    options.CompressionLevel = CompressionLevel.Optimal; // Default
    // options.CompressionLevel = CompressionLevel.Fastest; // Speed over size
    // options.CompressionLevel = CompressionLevel.SmallestSize; // Size over speed
});
```

## Running the Sample

```bash
cd samples/MessageCompressionSample
dotnet run
```

### Expected Output

```
=== Message Compression Sample ===
Demonstrating message compression with different payload sizes

Test 1: Small message (< threshold)
Original size: 623 bytes
Message sent and received successfully
Message ID: 12345-guid
Payload size: 500 characters
✅ Content verified - no data loss

Test 2: Medium message (> threshold)
Original size: 5123 bytes
Message sent and received successfully
Message ID: 67890-guid
Payload size: 5000 characters
✅ Content verified - no data loss

Test 3: Large message (much > threshold)
Original size: 50123 bytes
Message sent and received successfully
Message ID: abcde-guid
Payload size: 50000 characters
✅ Content verified - no data loss

Test 4: Compression statistics
Payload Size | Original | Compressed | Ratio
-------------|----------|------------|------
        1000 |     1123 |        234 | 79.2%
        5000 |     5123 |        456 | 91.1%
       10000 |    10123 |        678 | 93.3%
       50000 |    50123 |       1234 | 97.5%
      100000 |   100123 |       2345 | 97.7%

Key Insights:
- Compression is most effective for larger, repetitive data
- Small messages may not benefit from compression
- Use CompressionThreshold to optimize performance
```

## Compression Algorithms

### 1. Gzip (Default)
- **Pros**: Good balance between speed and compression ratio
- **Cons**: Moderate CPU usage
- **Use Case**: General-purpose compression
- **Compression Ratio**: ~70-90% for text data

```csharp
options.CompressionAlgorithm = CompressionAlgorithm.Gzip;
```

### 2. Brotli
- **Pros**: Better compression ratio than Gzip
- **Cons**: Higher CPU usage, slower compression
- **Use Case**: When bandwidth is more critical than CPU
- **Compression Ratio**: ~75-95% for text data

```csharp
options.CompressionAlgorithm = CompressionAlgorithm.Brotli;
```

### 3. Deflate
- **Pros**: Fastest compression/decompression
- **Cons**: Slightly lower compression ratio
- **Use Case**: When speed is critical
- **Compression Ratio**: ~65-85% for text data

```csharp
options.CompressionAlgorithm = CompressionAlgorithm.Deflate;
```

## Performance Considerations

### When to Use Compression

✅ **Good candidates:**
- Large messages (> 1KB)
- Text-based data (JSON, XML, logs)
- Repetitive content
- High bandwidth costs
- Network-constrained environments

❌ **Poor candidates:**
- Small messages (< 1KB)
- Already compressed data (images, videos)
- High-frequency, low-latency messages
- CPU-constrained environments

### Tuning Compression Threshold

The compression threshold determines when compression kicks in:

```csharp
// Conservative (compress larger messages only)
options.CompressionThreshold = 5120; // 5KB

// Balanced (default)
options.CompressionThreshold = 1024; // 1KB

// Aggressive (compress most messages)
options.CompressionThreshold = 512; // 512 bytes
```

### Compression Levels

```csharp
// Fastest - Low CPU, larger size
options.CompressionLevel = CompressionLevel.Fastest;

// Optimal - Balanced (default)
options.CompressionLevel = CompressionLevel.Optimal;

// SmallestSize - High CPU, smaller size
options.CompressionLevel = CompressionLevel.SmallestSize;
```

## Real-World Scenarios

### 1. Log Aggregation
```csharp
// Logs are typically large and compress well
options.CompressionThreshold = 1024;
options.CompressionAlgorithm = CompressionAlgorithm.Gzip;
options.CompressionLevel = CompressionLevel.Optimal;
```

### 2. High-Frequency Trading
```csharp
// Prioritize speed over size
options.EnableCompression = false; // Or use very high threshold
options.CompressionThreshold = 10240; // 10KB
options.CompressionAlgorithm = CompressionAlgorithm.Deflate;
options.CompressionLevel = CompressionLevel.Fastest;
```

### 3. Document Processing
```csharp
// Documents can be very large, prioritize size
options.CompressionThreshold = 512;
options.CompressionAlgorithm = CompressionAlgorithm.Brotli;
options.CompressionLevel = CompressionLevel.SmallestSize;
```

### 4. IoT Sensor Data
```csharp
// Small, frequent messages - disable compression
options.EnableCompression = false;
```

## Integration with Other Features

### With Circuit Breaker
```csharp
services.AddRelayMessageBroker(options =>
{
    options.UseInMemory();
    options.EnableCompression = true;
    options.EnableCircuitBreaker = true; // Compression + resilience
});
```

### With Retry Policy
```csharp
services.AddRelayMessageBroker(options =>
{
    options.UseInMemory();
    options.EnableCompression = true;
    options.RetryCount = 3; // Compression + retry
});
```

### With Message Broker Providers
```csharp
// RabbitMQ with compression
services.AddRelayMessageBroker(options =>
{
    options.UseRabbitMQ(config =>
    {
        config.HostName = "localhost";
        config.Port = 5672;
    });
    options.EnableCompression = true;
});

// Kafka with compression
services.AddRelayMessageBroker(options =>
{
    options.UseKafka(config =>
    {
        config.BootstrapServers = "localhost:9092";
    });
    options.EnableCompression = true;
});
```

## Monitoring and Metrics

Track compression effectiveness:

```csharp
// Log compression statistics
logger.LogInformation("Original size: {Original} bytes", originalSize);
logger.LogInformation("Compressed size: {Compressed} bytes", compressedSize);
logger.LogInformation("Compression ratio: {Ratio:F1}%", 
    (1.0 - ((double)compressedSize / originalSize)) * 100);
```

## Best Practices

1. **Measure First**: Profile your messages to understand sizes and patterns
2. **Set Appropriate Threshold**: Don't compress everything
3. **Choose Right Algorithm**: Balance speed vs. compression ratio
4. **Monitor CPU Usage**: Compression uses CPU - monitor in production
5. **Test End-to-End**: Verify compression doesn't impact latency
6. **Document Configuration**: Make compression settings visible

## Troubleshooting

### High CPU Usage
- Increase compression threshold
- Switch to faster algorithm (Deflate)
- Use CompressionLevel.Fastest
- Disable compression for small messages

### High Bandwidth Usage
- Lower compression threshold
- Switch to better algorithm (Brotli)
- Use CompressionLevel.SmallestSize

### Compression Not Working
- Check if messages exceed threshold
- Verify EnableCompression is true
- Ensure message size is calculated correctly

## Related Samples

- **MessageBroker.Sample**: Basic message broker usage
- **DistributedTracingSample**: Tracing compressed messages
- **ObservabilitySample**: Monitoring compression metrics

## Learn More

- [Message Broker Documentation](../../docs/message-broker/README.md)
- [Performance Tuning Guide](../../docs/performance/compression.md)
- [Compression Algorithms Comparison](../../docs/technical/compression-algorithms.md)

## License

This sample is part of the Relay project and follows the same license.
