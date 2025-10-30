# Message Deduplication Examples

## Example 1: Basic Content-Based Deduplication

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Deduplication;

// Configure services
var services = new ServiceCollection();

services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
});

services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
    options.Strategy = DeduplicationStrategy.ContentHash;
});

services.DecorateWithDeduplication();

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();

// Define a message
public record OrderCreatedEvent(int OrderId, string CustomerName, decimal Amount);

// Publish the same message twice
var order = new OrderCreatedEvent(123, "John Doe", 99.99m);

await broker.PublishAsync(order); // First publish - succeeds
await broker.PublishAsync(order); // Duplicate - discarded

// Output: "Duplicate message detected and discarded. Type: OrderCreatedEvent, Hash: ..."
```

## Example 2: Message ID-Based Deduplication

```csharp
services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(10);
    options.Strategy = DeduplicationStrategy.MessageId;
});

services.DecorateWithDeduplication();

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();

// Publish with explicit message ID
var messageId = Guid.NewGuid().ToString();

await broker.PublishAsync(
    new OrderCreatedEvent(123, "John Doe", 99.99m),
    new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["MessageId"] = messageId
        }
    });

// Try to publish again with same message ID - will be discarded
await broker.PublishAsync(
    new OrderCreatedEvent(456, "Jane Smith", 149.99m), // Different content
    new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["MessageId"] = messageId // Same ID - duplicate!
        }
    });
```

## Example 3: Custom Hash Function

```csharp
services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(15);
    options.Strategy = DeduplicationStrategy.Custom;
    
    // Custom hash function that only considers OrderId
    options.CustomHashFunction = (data) =>
    {
        var json = System.Text.Encoding.UTF8.GetString(data);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        
        if (doc.RootElement.TryGetProperty("OrderId", out var orderIdElement))
        {
            return $"ORDER-{orderIdElement.GetInt32()}";
        }
        
        return DeduplicationCache.GenerateContentHash(data);
    };
});

services.DecorateWithDeduplication();

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();

// These are considered duplicates because they have the same OrderId
await broker.PublishAsync(new OrderCreatedEvent(123, "John Doe", 99.99m));
await broker.PublishAsync(new OrderCreatedEvent(123, "Jane Smith", 149.99m)); // Duplicate!
```

## Example 4: Monitoring Metrics

```csharp
services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
});

services.DecorateWithDeduplication();

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();

// Publish some messages
for (int i = 0; i < 100; i++)
{
    await broker.PublishAsync(new OrderCreatedEvent(i, $"Customer {i}", i * 10m));
}

// Publish some duplicates
for (int i = 0; i < 20; i++)
{
    await broker.PublishAsync(new OrderCreatedEvent(i, $"Customer {i}", i * 10m));
}

// Get metrics
var decorator = broker as DeduplicationMessageBrokerDecorator;
var metrics = decorator?.GetMetrics();

Console.WriteLine($"Total Messages Checked: {metrics.TotalMessagesChecked}");
Console.WriteLine($"Total Duplicates Detected: {metrics.TotalDuplicatesDetected}");
Console.WriteLine($"Duplicate Detection Rate: {metrics.DuplicateDetectionRate:P2}");
Console.WriteLine($"Current Cache Size: {metrics.CurrentCacheSize}");
Console.WriteLine($"Cache Hit Rate: {metrics.CacheHitRate:P2}");

// Output:
// Total Messages Checked: 120
// Total Duplicates Detected: 20
// Duplicate Detection Rate: 16.67%
// Current Cache Size: 100
// Cache Hit Rate: 16.67%
```

## Example 5: Integration with Retry Logic

```csharp
using Polly;

services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(5);
});

services.DecorateWithDeduplication();

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();

// Define a retry policy
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Publish with retry - deduplication ensures message is only published once
await retryPolicy.ExecuteAsync(async () =>
{
    await broker.PublishAsync(new OrderCreatedEvent(123, "John Doe", 99.99m));
});

// Even if the retry policy executes multiple times due to transient failures,
// deduplication ensures the message is only published once
```

## Example 6: High-Volume Scenario with Cache Management

```csharp
services.AddMessageDeduplication(options =>
{
    options.Enabled = true;
    options.Window = TimeSpan.FromMinutes(2); // Shorter window for high volume
    options.MaxCacheSize = 50_000; // Limit cache size
    options.Strategy = DeduplicationStrategy.ContentHash;
});

services.DecorateWithDeduplication();

var serviceProvider = services.BuildServiceProvider();
var broker = serviceProvider.GetRequiredService<IMessageBroker>();

await broker.StartAsync();

// Simulate high-volume publishing
var tasks = new List<Task>();
for (int i = 0; i < 100_000; i++)
{
    var orderId = i;
    tasks.Add(Task.Run(async () =>
    {
        await broker.PublishAsync(new OrderCreatedEvent(
            orderId, 
            $"Customer {orderId}", 
            orderId * 10m));
    }));
    
    // Throttle to avoid overwhelming the system
    if (tasks.Count >= 1000)
    {
        await Task.WhenAll(tasks);
        tasks.Clear();
    }
}

await Task.WhenAll(tasks);

// Check metrics
var decorator = broker as DeduplicationMessageBrokerDecorator;
var metrics = decorator?.GetMetrics();

Console.WriteLine($"Cache Size: {metrics.CurrentCacheSize}");
Console.WriteLine($"Total Evictions: {metrics.TotalEvictions}");
// Cache will automatically evict old entries using LRU when it exceeds MaxCacheSize
```

## Example 7: Conditional Deduplication

```csharp
// You can enable/disable deduplication at runtime by using different configurations
services.AddMessageDeduplication(options =>
{
    // Read from configuration
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    options.Enabled = config.GetValue<bool>("Deduplication:Enabled");
    options.Window = config.GetValue<TimeSpan>("Deduplication:Window");
});

services.DecorateWithDeduplication();

// In appsettings.json:
// {
//   "Deduplication": {
//     "Enabled": true,
//     "Window": "00:05:00"
//   }
// }
```

## Example 8: Testing Deduplication

```csharp
using Xunit;
using Microsoft.Extensions.DependencyInjection;

public class DeduplicationTests
{
    [Fact]
    public async Task ShouldDiscardDuplicateMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.InMemory;
        });
        
        services.AddMessageDeduplication(options =>
        {
            options.Enabled = true;
            options.Window = TimeSpan.FromMinutes(5);
        });
        
        services.DecorateWithDeduplication();
        
        var serviceProvider = services.BuildServiceProvider();
        var broker = serviceProvider.GetRequiredService<IMessageBroker>();
        
        await broker.StartAsync();
        
        var message = new OrderCreatedEvent(123, "John Doe", 99.99m);
        
        // Act
        await broker.PublishAsync(message);
        await broker.PublishAsync(message); // Duplicate
        
        // Assert
        var decorator = broker as DeduplicationMessageBrokerDecorator;
        var metrics = decorator?.GetMetrics();
        
        Assert.Equal(2, metrics.TotalMessagesChecked);
        Assert.Equal(1, metrics.TotalDuplicatesDetected);
        Assert.Equal(0.5, metrics.DuplicateDetectionRate);
    }
}
```
