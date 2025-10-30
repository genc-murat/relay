# Batch Processing Examples

This document provides practical examples of using the Batch Processing feature in Relay.MessageBroker.

## Table of Contents

1. [Basic Setup](#basic-setup)
2. [High-Throughput Scenario](#high-throughput-scenario)
3. [Low-Latency Scenario](#low-latency-scenario)
4. [Manual Flushing](#manual-flushing)
5. [Monitoring and Metrics](#monitoring-and-metrics)
6. [Advanced Configuration](#advanced-configuration)

## Basic Setup

### Step 1: Configure Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Batch;

var services = new ServiceCollection();

// Add message broker with batching
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
})
.AddMessageBrokerBatching(batchOptions =>
{
    batchOptions.Enabled = true;
    batchOptions.MaxBatchSize = 100;
    batchOptions.FlushInterval = TimeSpan.FromMilliseconds(100);
    batchOptions.EnableCompression = true;
    batchOptions.PartialRetry = true;
});

var serviceProvider = services.BuildServiceProvider();
```

### Step 2: Publish Messages

```csharp
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();

// Messages are automatically batched
for (int i = 0; i < 1000; i++)
{
    await messageBroker.PublishAsync(new OrderCreatedEvent
    {
        OrderId = i,
        CustomerId = $"CUST-{i}",
        Amount = 99.99m,
        CreatedAt = DateTime.UtcNow
    });
}

// Batches are automatically flushed when:
// - MaxBatchSize (100) is reached
// - FlushInterval (100ms) expires
// - Application shuts down
```

## High-Throughput Scenario

For scenarios where you need to publish large volumes of messages with maximum throughput:

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.ConnectionString = "localhost:9092";
})
.AddMessageBrokerBatching(batchOptions =>
{
    batchOptions.Enabled = true;
    batchOptions.MaxBatchSize = 1000;           // Large batches
    batchOptions.FlushInterval = TimeSpan.FromSeconds(1);  // 1 second window
    batchOptions.EnableCompression = true;       // Reduce bandwidth
    batchOptions.PartialRetry = true;
});

// Publish 100,000 events
var tasks = Enumerable.Range(0, 100_000)
    .Select(async i => await messageBroker.PublishAsync(new TelemetryEvent
    {
        DeviceId = $"DEVICE-{i % 1000}",
        Timestamp = DateTime.UtcNow,
        Temperature = 20 + (i % 10),
        Humidity = 50 + (i % 20)
    }));

await Task.WhenAll(tasks);

Console.WriteLine("Published 100,000 events with batching");
```

## Low-Latency Scenario

For scenarios where low latency is critical:

```csharp
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
})
.AddMessageBrokerBatching(batchOptions =>
{
    batchOptions.Enabled = true;
    batchOptions.MaxBatchSize = 10;             // Small batches
    batchOptions.FlushInterval = TimeSpan.FromMilliseconds(50);  // Quick flush
    batchOptions.EnableCompression = false;     // Skip compression overhead
    batchOptions.PartialRetry = true;
});

// Publish time-sensitive notifications
await messageBroker.PublishAsync(new NotificationEvent
{
    UserId = "user-123",
    Type = "URGENT",
    Message = "Your order has been shipped!",
    Timestamp = DateTime.UtcNow
});

// Message will be flushed within 50ms or when 10 messages accumulate
```

## Manual Flushing

### Flush All Batches

```csharp
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();

// Publish some messages
for (int i = 0; i < 50; i++)
{
    await messageBroker.PublishAsync(new Event { Id = i });
}

// Manually flush all pending batches
if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
{
    await batchDecorator.FlushAllAsync();
    Console.WriteLine("All batches flushed");
}
```

### Flush on Critical Operations

```csharp
public async Task ProcessCriticalOrderAsync(Order order)
{
    // Publish order events
    await messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = order.Id });
    await messageBroker.PublishAsync(new PaymentProcessedEvent { OrderId = order.Id });
    
    // Ensure events are published immediately
    if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
    {
        await batchDecorator.FlushAllAsync();
    }
    
    Console.WriteLine($"Order {order.Id} processed and events published");
}
```

## Monitoring and Metrics

### Get Metrics for a Message Type

```csharp
if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
{
    var metrics = batchDecorator.GetMetrics<OrderCreatedEvent>();
    
    if (metrics != null)
    {
        Console.WriteLine($"=== Batch Metrics for OrderCreatedEvent ===");
        Console.WriteLine($"Current Batch Size: {metrics.CurrentBatchSize}");
        Console.WriteLine($"Average Batch Size: {metrics.AverageBatchSize:F2}");
        Console.WriteLine($"Total Batches Processed: {metrics.TotalBatchesProcessed}");
        Console.WriteLine($"Total Messages Processed: {metrics.TotalMessagesProcessed}");
        Console.WriteLine($"Average Processing Time: {metrics.AverageProcessingTimeMs:F2}ms");
        Console.WriteLine($"Success Rate: {metrics.SuccessRate:P2}");
        Console.WriteLine($"Total Failed Messages: {metrics.TotalFailedMessages}");
        Console.WriteLine($"Compression Ratio: {metrics.CompressionRatio:F2}x");
        Console.WriteLine($"Last Flush: {metrics.LastFlushAt}");
    }
}
```

### Periodic Metrics Reporting

```csharp
using System.Threading;

var cts = new CancellationTokenSource();

// Start background metrics reporting
_ = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        
        if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
        {
            var metrics = batchDecorator.GetMetrics<OrderCreatedEvent>();
            if (metrics != null)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] " +
                    $"Batches: {metrics.TotalBatchesProcessed}, " +
                    $"Messages: {metrics.TotalMessagesProcessed}, " +
                    $"Success Rate: {metrics.SuccessRate:P0}");
            }
        }
    }
}, cts.Token);

// ... application logic ...

// Stop metrics reporting
cts.Cancel();
```

## Advanced Configuration

### Environment-Specific Configuration

```csharp
public static IServiceCollection AddMessageBrokerWithBatching(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var environment = configuration["Environment"];
    
    services.AddMessageBroker(options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        options.ConnectionString = configuration["MessageBroker:ConnectionString"];
    });
    
    if (environment == "Production")
    {
        // Production: High throughput
        services.AddMessageBrokerBatching(batchOptions =>
        {
            batchOptions.Enabled = true;
            batchOptions.MaxBatchSize = 1000;
            batchOptions.FlushInterval = TimeSpan.FromSeconds(1);
            batchOptions.EnableCompression = true;
            batchOptions.PartialRetry = true;
        });
    }
    else if (environment == "Development")
    {
        // Development: Low latency for debugging
        services.AddMessageBrokerBatching(batchOptions =>
        {
            batchOptions.Enabled = true;
            batchOptions.MaxBatchSize = 10;
            batchOptions.FlushInterval = TimeSpan.FromMilliseconds(50);
            batchOptions.EnableCompression = false;
            batchOptions.PartialRetry = true;
        });
    }
    
    return services;
}
```

### Dynamic Configuration

```csharp
public class BatchConfigurationService
{
    private readonly IOptionsMonitor<BatchOptions> _optionsMonitor;
    
    public BatchConfigurationService(IOptionsMonitor<BatchOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        
        // React to configuration changes
        _optionsMonitor.OnChange(options =>
        {
            Console.WriteLine($"Batch configuration changed:");
            Console.WriteLine($"  MaxBatchSize: {options.MaxBatchSize}");
            Console.WriteLine($"  FlushInterval: {options.FlushInterval}");
        });
    }
}
```

### Conditional Batching

```csharp
public class SmartBatchingService
{
    private readonly IMessageBroker _messageBroker;
    
    public async Task PublishAsync<TMessage>(TMessage message, bool useBatching = true)
    {
        if (!useBatching && _messageBroker is BatchMessageBrokerDecorator batchDecorator)
        {
            // Bypass batching for critical messages
            await batchDecorator.FlushAllAsync();
        }
        
        await _messageBroker.PublishAsync(message);
        
        if (!useBatching && _messageBroker is BatchMessageBrokerDecorator decorator)
        {
            // Ensure immediate delivery
            await decorator.FlushAllAsync();
        }
    }
}
```

## Complete Example Application

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.MessageBroker;
using Relay.MessageBroker.Batch;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.ConnectionString = "amqp://localhost";
        })
        .AddMessageBrokerBatching(batchOptions =>
        {
            batchOptions.Enabled = true;
            batchOptions.MaxBatchSize = 100;
            batchOptions.FlushInterval = TimeSpan.FromMilliseconds(100);
            batchOptions.EnableCompression = true;
            batchOptions.PartialRetry = true;
        });
        
        services.AddHostedService<MessagePublisherService>();
    })
    .Build();

await host.RunAsync();

// Background service that publishes messages
public class MessagePublisherService : BackgroundService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<MessagePublisherService> _logger;
    
    public MessagePublisherService(
        IMessageBroker messageBroker,
        ILogger<MessagePublisherService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var counter = 0;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Publish messages
                for (int i = 0; i < 100; i++)
                {
                    await _messageBroker.PublishAsync(new Event
                    {
                        Id = counter++,
                        Timestamp = DateTime.UtcNow,
                        Data = $"Event {counter}"
                    }, cancellationToken: stoppingToken);
                }
                
                // Log metrics
                if (_messageBroker is BatchMessageBrokerDecorator batchDecorator)
                {
                    var metrics = batchDecorator.GetMetrics<Event>();
                    if (metrics != null)
                    {
                        _logger.LogInformation(
                            "Published 100 messages. Total: {Total}, Success Rate: {SuccessRate:P0}",
                            metrics.TotalMessagesProcessed,
                            metrics.SuccessRate);
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing messages");
            }
        }
    }
}

public record Event
{
    public int Id { get; init; }
    public DateTime Timestamp { get; init; }
    public string Data { get; init; } = string.Empty;
}
```

## Performance Tips

1. **Batch Size**: Start with 100 and adjust based on your message size and throughput requirements
2. **Flush Interval**: Use 100ms for balanced latency/throughput, adjust based on your needs
3. **Compression**: Enable for JSON messages > 1KB, disable for small messages or binary data
4. **Partial Retry**: Enable for better resilience, disable if you have external retry mechanisms
5. **Monitoring**: Regularly check metrics to optimize configuration

## Troubleshooting

### Problem: Messages Not Being Published

**Solution**: Check if batching is enabled and manually flush if needed:

```csharp
if (messageBroker is BatchMessageBrokerDecorator batchDecorator)
{
    await batchDecorator.FlushAllAsync();
}
```

### Problem: High Memory Usage

**Solution**: Reduce `MaxBatchSize` or `FlushInterval`:

```csharp
batchOptions.MaxBatchSize = 50;  // Smaller batches
batchOptions.FlushInterval = TimeSpan.FromMilliseconds(50);  // More frequent flushing
```

### Problem: High Latency

**Solution**: Use smaller batches and shorter flush intervals:

```csharp
batchOptions.MaxBatchSize = 10;
batchOptions.FlushInterval = TimeSpan.FromMilliseconds(25);
batchOptions.EnableCompression = false;  // Skip compression overhead
```
