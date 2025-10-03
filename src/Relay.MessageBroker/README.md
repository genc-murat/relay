# Relay.MessageBroker

Enterprise-grade message broker integration for Relay Framework with support for multiple message brokers.

## Features

- ✅ **RabbitMQ Support** - Full-featured RabbitMQ integration with exchanges, queues, and routing
- ✅ **Kafka Support** - Apache Kafka integration with topics, partitions, and consumer groups
- ✅ **Azure Service Bus Support** - Microsoft Azure Service Bus integration for cloud-native messaging
- ✅ **AWS SQS/SNS Support** - Amazon Web Services messaging services integration
- ✅ **NATS Support** - High-performance NATS messaging with JetStream support
- ✅ **Redis Streams Support** - Redis-based messaging with consumer groups
- ✅ **Easy Configuration** - Simple and intuitive API for setup and configuration
- ✅ **Automatic Serialization** - JSON serialization out of the box
- ✅ **Flexible Publishing** - Support for routing keys, headers, priorities, and expiration
- ✅ **Reliable Subscriptions** - Manual and automatic acknowledgment modes
- ✅ **Retry Policies** - Built-in retry mechanisms with exponential backoff
- ✅ **Hosted Service** - Automatic lifecycle management with .NET hosting
- ✅ **Testing Support** - In-memory broker for unit testing

## Supported Message Brokers

| Broker | Status | Use Case |
|--------|--------|----------|
| **RabbitMQ** | ✅ Production Ready | General-purpose messaging, routing patterns |
| **Apache Kafka** | ✅ Production Ready | Event streaming, high-throughput scenarios |
| **Azure Service Bus** | 🚧 In Development | Cloud-native Azure applications |
| **AWS SQS/SNS** | 🚧 In Development | Cloud-native AWS applications |
| **NATS** | 🚧 In Development | Microservices, edge computing, IoT |
| **Redis Streams** | 🚧 In Development | Real-time messaging, simple pub/sub |

## Installation

```bash
dotnet add package Relay.MessageBroker
```

## Quick Start

### RabbitMQ Configuration

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add RabbitMQ message broker
builder.Services.AddRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
    options.VirtualHost = "/";
    options.PrefetchCount = 10;
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

### Kafka Configuration

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add Kafka message broker
builder.Services.AddKafka(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ConsumerGroupId = "my-consumer-group";
    options.AutoOffsetReset = "earliest";
    options.CompressionType = "gzip";
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

### Azure Service Bus Configuration

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Service Bus message broker
builder.Services.AddAzureServiceBus(options =>
{
    options.ConnectionString = "Endpoint=sb://myservicebus.servicebus.windows.net/;SharedAccessKeyName=...";
    options.DefaultEntityName = "relay-messages";
    options.MaxConcurrentCalls = 10;
    options.PrefetchCount = 10;
    options.AutoCompleteMessages = false;
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

### AWS SQS/SNS Configuration

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add AWS SQS/SNS message broker
builder.Services.AddAwsSqsSns(options =>
{
    options.Region = "us-east-1";
    options.DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789/my-queue";
    options.DefaultTopicArn = "arn:aws:sns:us-east-1:123456789:my-topic";
    options.MaxNumberOfMessages = 10;
    options.WaitTimeSeconds = TimeSpan.FromSeconds(20);
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

### NATS Configuration

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add NATS message broker
builder.Services.AddNats(options =>
{
    options.Servers = new[] { "nats://localhost:4222" };
    options.Name = "relay-nats-client";
    options.MaxReconnects = 10;
    options.UseJetStream = true;
    options.StreamName = "RELAY_EVENTS";
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

### Redis Streams Configuration

```csharp
using Relay.MessageBroker;

var builder = WebApplication.CreateBuilder(args);

// Add Redis Streams message broker
builder.Services.AddRedisStreams(options =>
{
    options.ConnectionString = "localhost:6379";
    options.DefaultStreamName = "relay:stream";
    options.ConsumerGroupName = "relay-consumer-group";
    options.ConsumerName = "relay-consumer";
    options.MaxMessagesToRead = 10;
    options.CreateConsumerGroupIfNotExists = true;
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();

var app = builder.Build();
app.Run();
```

## Publishing Messages

### Simple Publishing

```csharp
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderService
{
    private readonly IMessageBroker _messageBroker;

    public OrderService(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    public async Task CreateOrderAsync(Order order)
    {
        // ... create order logic ...

        // Publish event
        await _messageBroker.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            Amount = order.Amount,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

### Publishing with Options

```csharp
await _messageBroker.PublishAsync(
    new OrderCreatedEvent { OrderId = 123, Amount = 99.99m },
    new PublishOptions
    {
        RoutingKey = "orders.created",
        Exchange = "order-events",
        Priority = 5,
        Expiration = TimeSpan.FromMinutes(10),
        Headers = new Dictionary<string, object>
        {
            { "TenantId", "tenant-123" },
            { "SourceSystem", "WebAPI" }
        }
    });
```

## Subscribing to Messages

### Simple Subscription

```csharp
public class OrderEventHandler : IHostedService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<OrderEventHandler> _logger;

    public OrderEventHandler(
        IMessageBroker messageBroker,
        ILogger<OrderEventHandler> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                _logger.LogInformation(
                    "Received order created event for order {OrderId}",
                    message.OrderId);

                // Process the message
                await ProcessOrderAsync(message);

                // Acknowledge the message
                if (context.Acknowledge != null)
                {
                    await context.Acknowledge();
                }
            },
            cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Task ProcessOrderAsync(OrderCreatedEvent order)
    {
        // Your business logic here
        return Task.CompletedTask;
    }
}
```

### Subscription with Options

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        // Process message
        await ProcessOrderAsync(message);
        await context.Acknowledge!();
    },
    new SubscriptionOptions
    {
        QueueName = "order-processing-queue",
        RoutingKey = "orders.*",
        Exchange = "order-events",
        PrefetchCount = 20,
        Durable = true,
        AutoAck = false
    });
```

## Message Context

The `MessageContext` provides metadata about the message:

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        // Access metadata
        var messageId = context.MessageId;
        var correlationId = context.CorrelationId;
        var timestamp = context.Timestamp;
        var customHeader = context.Headers?["TenantId"];

        // Process message
        await ProcessOrderAsync(message);

        // Acknowledge or reject
        if (success)
        {
            await context.Acknowledge!();
        }
        else
        {
            // Reject and requeue
            await context.Reject!(requeue: true);
        }
    });
```

## Advanced Configuration

### Custom Message Broker Options

```csharp
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.DefaultExchange = "my-app-events";
    options.DefaultRoutingKeyPattern = "{MessageType}";
    options.AutoPublishResults = true;
    options.EnableSerialization = true;
    options.SerializerType = MessageSerializerType.Json;

    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "rabbitmq.example.com",
        Port = 5672,
        UserName = "myapp",
        Password = "secretpassword",
        VirtualHost = "/production",
        UseSsl = true,
        PrefetchCount = 50,
        ExchangeType = "topic"
    };

    options.RetryPolicy = new RetryPolicy
    {
        MaxAttempts = 5,
        InitialDelay = TimeSpan.FromSeconds(2),
        MaxDelay = TimeSpan.FromMinutes(5),
        BackoffMultiplier = 2.0,
        UseExponentialBackoff = true
    };
});
```

### Kafka Advanced Configuration

```csharp
builder.Services.AddKafka(options =>
{
    options.BootstrapServers = "kafka1:9092,kafka2:9092,kafka3:9092";
    options.ConsumerGroupId = "my-service-consumer-group";
    options.AutoOffsetReset = "earliest";
    options.EnableAutoCommit = false;
    options.SessionTimeout = TimeSpan.FromSeconds(30);
    options.CompressionType = "snappy";
    options.DefaultPartitions = 6;
    options.ReplicationFactor = 3;
});
```

## Testing

Use the `InMemoryMessageBroker` for unit testing:

```csharp
public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_ShouldPublishOrderCreatedEvent()
    {
        // Arrange
        var messageBroker = new InMemoryMessageBroker();
        var service = new OrderService(messageBroker);

        // Act
        await service.CreateOrderAsync(new Order { Id = 123, Amount = 99.99m });

        // Assert
        messageBroker.PublishedMessages.Should().HaveCount(1);
        var publishedMessage = messageBroker.PublishedMessages[0];
        var orderEvent = publishedMessage.Message as OrderCreatedEvent;
        orderEvent.Should().NotBeNull();
        orderEvent!.OrderId.Should().Be(123);
    }
}
```

## Best Practices

### 1. Use Correlation IDs

```csharp
await _messageBroker.PublishAsync(
    event,
    new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            { "CorrelationId", correlationId }
        }
    });
```

### 2. Handle Errors Gracefully

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        try
        {
            await ProcessOrderAsync(message);
            await context.Acknowledge!();
        }
        catch (TransientException ex)
        {
            _logger.LogWarning(ex, "Transient error, requeuing message");
            await context.Reject!(requeue: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error, dead-lettering message");
            await context.Reject!(requeue: false);
        }
    });
```

### 3. Use Idempotent Handlers

```csharp
public class IdempotentOrderHandler
{
    private readonly IMessageBroker _messageBroker;
    private readonly ICache _cache;

    public async Task HandleAsync(OrderCreatedEvent message, MessageContext context)
    {
        var messageId = context.MessageId!;

        // Check if already processed
        if (await _cache.ExistsAsync(messageId))
        {
            _logger.LogInformation("Message {MessageId} already processed", messageId);
            await context.Acknowledge!();
            return;
        }

        // Process message
        await ProcessOrderAsync(message);

        // Mark as processed
        await _cache.SetAsync(messageId, true, TimeSpan.FromDays(7));
        await context.Acknowledge!();
    }
}
```

### 4. Use Dead Letter Queues

Configure dead letter exchanges in RabbitMQ for failed messages:

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    handler,
    new SubscriptionOptions
    {
        QueueName = "orders-queue",
        Durable = true,
        // RabbitMQ will automatically use DLX if configured on the queue
    });
```

## Performance Tips

1. **Batch Processing**: Process multiple messages in batches when possible
2. **Prefetch Count**: Adjust `PrefetchCount` based on your processing speed
3. **Compression**: Use compression for large messages (Kafka)
4. **Partitioning**: Design Kafka topics with appropriate partitioning strategy
5. **Connection Pooling**: Reuse connections and channels
6. **Async Processing**: Always use async/await for I/O operations

## Monitoring and Observability

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        using var activity = ActivitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", message.OrderId);
        activity?.SetTag("message.id", context.MessageId);

        var sw = Stopwatch.StartNew();
        try
        {
            await ProcessOrderAsync(message);
            _metrics.RecordProcessingTime(sw.Elapsed);
            _metrics.IncrementProcessedCount();
            await context.Acknowledge!();
        }
        catch (Exception ex)
        {
            _metrics.IncrementErrorCount();
            _logger.LogError(ex, "Error processing order {OrderId}", message.OrderId);
            throw;
        }
    });
```

## Architecture Patterns

### Event-Driven Architecture

```csharp
// Service A publishes events
await _messageBroker.PublishAsync(new OrderCreatedEvent { ... });

// Service B subscribes and processes
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(async (evt, ctx, ct) =>
{
    await _inventoryService.ReserveItemsAsync(evt.OrderId);
    await ctx.Acknowledge!();
});

// Service C also subscribes
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(async (evt, ctx, ct) =>
{
    await _notificationService.SendOrderConfirmationAsync(evt.OrderId);
    await ctx.Acknowledge!();
});
```

### Saga Pattern

```csharp
public class OrderSagaOrchestrator
{
    private readonly IMessageBroker _messageBroker;

    public async Task StartSagaAsync(CreateOrderCommand command)
    {
        var sagaId = Guid.NewGuid().ToString();

        // Step 1: Create order
        await _messageBroker.PublishAsync(
            new CreateOrderEvent { SagaId = sagaId, ... },
            new PublishOptions
            {
                Headers = new Dictionary<string, object> { { "SagaId", sagaId } }
            });
    }

    public async Task SubscribeToSagaEventsAsync()
    {
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(HandleOrderCreated);
        await _messageBroker.SubscribeAsync<PaymentProcessedEvent>(HandlePaymentProcessed);
        await _messageBroker.SubscribeAsync<InventoryReservedEvent>(HandleInventoryReserved);
        await _messageBroker.SubscribeAsync<SagaFailedEvent>(HandleSagaFailed);
    }
}
```

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please read CONTRIBUTING.md for details.

## Support

- GitHub Issues: [Report bugs and request features](https://github.com/your-org/relay/issues)
- Documentation: [Full documentation](https://docs.relay.dev)
- Examples: [Sample applications](https://github.com/your-org/relay/tree/main/samples)
