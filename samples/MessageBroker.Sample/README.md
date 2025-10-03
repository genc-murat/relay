# Relay MessageBroker Sample

This sample demonstrates how to use Relay.MessageBroker with RabbitMQ and Kafka for event-driven architectures.

## Features Demonstrated

- Publishing events to RabbitMQ
- Subscribing to events with manual acknowledgment
- Error handling and retry mechanisms
- Message context usage
- Correlation IDs
- Multiple subscribers for the same event

## Prerequisites

### Option 1: RabbitMQ (Docker)

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Access management UI: http://localhost:15672 (guest/guest)

### Option 2: Kafka (Docker)

```bash
# Start Kafka with Docker Compose
docker-compose up -d
```

## Running the Sample

### RabbitMQ

```bash
dotnet run -- --broker rabbitmq
```

### Kafka

```bash
dotnet run -- --broker kafka
```

### In-Memory (for testing)

```bash
dotnet run -- --broker inmemory
```

## Sample Output

```
[INFO] Message broker started
[INFO] Subscribed to OrderCreatedEvent
[INFO] Subscribed to OrderCompletedEvent
[INFO] Publishing order created event for order 1
[INFO] Received OrderCreatedEvent for order 1
[INFO] Processing order 1...
[INFO] Publishing order completed event for order 1
[INFO] Received OrderCompletedEvent for order 1
[INFO] Sending notification for order 1
```

## Code Structure

- `Program.cs` - Application setup and configuration
- `Events/` - Event definitions (OrderCreatedEvent, OrderCompletedEvent)
- `Handlers/` - Event handlers (OrderEventHandler, NotificationHandler)
- `Services/` - Business services (OrderService)

## Key Concepts

### 1. Event Definition

```csharp
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. Publishing Events

```csharp
await _messageBroker.PublishAsync(
    new OrderCreatedEvent { OrderId = 1, Amount = 99.99m },
    new PublishOptions
    {
        RoutingKey = "orders.created",
        Headers = new Dictionary<string, object>
        {
            { "CorrelationId", correlationId }
        }
    });
```

### 3. Subscribing to Events

```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        _logger.LogInformation("Received order {OrderId}", message.OrderId);
        await ProcessOrderAsync(message);
        await context.Acknowledge!();
    });
```

## Testing

Run the integration tests:

```bash
dotnet test
```

## Troubleshooting

### RabbitMQ Connection Issues

1. Ensure RabbitMQ is running: `docker ps`
2. Check connectivity: `telnet localhost 5672`
3. Check logs: `docker logs rabbitmq`

### Kafka Connection Issues

1. Ensure Kafka is running: `docker-compose ps`
2. Check broker list: `kafka-topics.sh --bootstrap-server localhost:9092 --list`
3. Check logs: `docker-compose logs kafka`
