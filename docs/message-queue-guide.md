# Message Queue Integration

Relay provides built-in support for message queue integration. This feature allows you to publish and consume messages through various message queue systems.

## 🚀 Quick Start

### 1. Enable Message Queue Integration

To enable message queue integration, call `AddRelayMessageQueue()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayMessageQueue(); // Enable message queue integration
```

### 2. Define Messages

Create message types for your message queue:

```csharp
public record CreateUserMessage(string Name, string Email);
public record UpdateUserMessage(Guid UserId, string Name, string Email);
public record DeleteUserMessage(Guid UserId);
```

### 3. Define Message Handlers

Create message handlers by applying the `MessageQueueAttribute`:

```csharp
public class UserMessageHandler
{
    [MessageQueue("user-queue")]
    public async ValueTask HandleCreateUser(CreateUserMessage message, CancellationToken cancellationToken)
    {
        // Handle the message
        Console.WriteLine($"Creating user: {message.Name} ({message.Email})");
    }

    [MessageQueue("user-queue")]
    public async ValueTask HandleUpdateUser(UpdateUserMessage message, CancellationToken cancellationToken)
    {
        // Handle the message
        Console.WriteLine($"Updating user {message.UserId}: {message.Name} ({message.Email})");
    }
}
```

### 4. Use Message Queue Integration

Use the message queue publisher to send messages:

```csharp
// Get the message queue publisher
var publisher = serviceProvider.GetRequiredService<IMessageQueuePublisher>();

// Publish a message
await publisher.PublishAsync("user-queue", new CreateUserMessage("John Doe", "john.doe@example.com"));
```

## 🎯 Key Features

### Message Queue Attribute

The `MessageQueueAttribute` enables message queue integration for specific handlers:

```csharp
[MessageQueue("user-queue", ExchangeName = "user-exchange", RoutingKey = "user.create")]
public async ValueTask HandleCreateUser(CreateUserMessage message, CancellationToken cancellationToken)
{
    // Handle the message
}
```

### Message Queue Publisher

The `IMessageQueuePublisher` interface provides methods for publishing messages:

```csharp
public interface IMessageQueuePublisher
{
    ValueTask PublishAsync(string queueName, object message, CancellationToken cancellationToken = default);
    ValueTask PublishAsync(string exchangeName, string routingKey, object message, CancellationToken cancellationToken = default);
}
```

### Message Queue Consumer

The `IMessageQueueConsumer` interface provides methods for consuming messages:

```csharp
public interface IMessageQueueConsumer
{
    ValueTask StartConsumingAsync(string queueName, Func<object, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken = default);
    ValueTask StartConsumingAsync(string exchangeName, string routingKey, Func<object, CancellationToken, ValueTask> messageHandler, CancellationToken cancellationToken = default);
    ValueTask StopConsumingAsync(CancellationToken cancellationToken = default);
}
```

### Message Wrapper

The `MessageWrapper` class provides a standardized format for messages:

```csharp
public class MessageWrapper
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MessageType { get; set; }
    public string Content { get; set; }
    public string? CorrelationId { get; set; }
    public string? ReplyTo { get; set; }
    public IDictionary<string, object> Properties { get; set; }
}
```

## 🛠️ Advanced Configuration

### Message Queue Implementation

Register a custom message queue implementation:

```csharp
services.AddTransient<IMessageQueuePublisher, RabbitMqPublisher>();
services.AddTransient<IMessageQueueConsumer, RabbitMqConsumer>();
```

### Message Queue Options

Configure message queue options:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultMessageQueueOptions.EnableMessageQueueIntegration = true;
    options.DefaultMessageQueueOptions.AutoAck = false;
    options.DefaultMessageQueueOptions.PrefetchCount = 10;
});
```

### Retry Handling

Configure automatic retry for failed message processing:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultMessageQueueOptions.EnableAutomaticRetry = true;
    options.DefaultMessageQueueOptions.MaxRetryAttempts = 5;
});
```

## ⚡ Performance

Message queue integration is designed to be efficient and scalable:

- **In-Memory Implementation**: Fast in-memory message queue for development
- **Async Support**: Fully asynchronous implementation
- **Batch Processing**: Support for batch message processing
- **Connection Pooling**: Efficient connection management for external message queues

## 🧪 Testing

Message queue integration can be tested using the in-memory implementation or by mocking the interfaces.