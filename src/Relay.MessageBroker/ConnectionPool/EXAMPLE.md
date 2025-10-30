# Connection Pool Usage Examples

## Basic Usage with RabbitMQ

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.RabbitMQ;
using RabbitMQ.Client;

// Configure services
var services = new ServiceCollection();

// Add message broker with connection pooling
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest",
        VirtualHost = "/"
    };
    
    // Configure connection pool
    options.ConnectionPool = new ConnectionPoolOptions
    {
        Enabled = true,
        MinPoolSize = 5,
        MaxPoolSize = 50,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
        ValidationInterval = TimeSpan.FromSeconds(30),
        IdleTimeout = TimeSpan.FromMinutes(5)
    };
});

// Optionally register a connection pool for RabbitMQ connections
services.AddConnectionPool(
    (sp, ct) => RabbitMQConnectionPoolIntegration.CreateConnectionFactory(
        sp.GetRequiredService<IOptions<MessageBrokerOptions>>().Value,
        sp.GetService<ILogger<IConnection>>()
    )(ct),
    new ConnectionPoolOptions { MinPoolSize = 5, MaxPoolSize = 50 },
    RabbitMQConnectionPoolIntegration.CreateConnectionValidator(),
    RabbitMQConnectionPoolIntegration.CreateConnectionDisposer()
);

var serviceProvider = services.BuildServiceProvider();

// Use the connection pool
var connectionPool = serviceProvider.GetRequiredService<IConnectionPool<IConnection>>();

await using var pooledConnection = await connectionPool.AcquireAsync();
var connection = pooledConnection.Connection;

// Use the connection
var channel = await connection.CreateChannelAsync();
// ... perform operations ...

// Connection is automatically released when disposed
```

## Basic Usage with Kafka

```csharp
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.ConnectionPool;
using Relay.MessageBroker.Kafka;

var services = new ServiceCollection();

services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.Kafka = new KafkaOptions
    {
        BootstrapServers = "localhost:9092",
        ConsumerGroupId = "my-consumer-group"
    };
    
    options.ConnectionPool = new ConnectionPoolOptions
    {
        Enabled = true,
        MinPoolSize = 3,
        MaxPoolSize = 20
    };
});

// Register Kafka producer pool
services.AddConnectionPool(
    (sp, ct) => KafkaConnectionPoolIntegration.CreateProducerFactory(
        sp.GetRequiredService<IOptions<MessageBrokerOptions>>().Value,
        sp.GetService<ILogger<IProducer<string, byte[]>>>()
    )(ct),
    new ConnectionPoolOptions { MinPoolSize = 3, MaxPoolSize = 20 },
    KafkaConnectionPoolIntegration.CreateProducerValidator(),
    KafkaConnectionPoolIntegration.CreateProducerDisposer()
);

var serviceProvider = services.BuildServiceProvider();

var producerPool = serviceProvider.GetRequiredService<IConnectionPool<IProducer<string, byte[]>>>();

await using var pooledProducer = await producerPool.AcquireAsync();
var producer = pooledProducer.Connection;

// Use the producer
await producer.ProduceAsync("my-topic", new Message<string, byte[]>
{
    Key = "key",
    Value = Encoding.UTF8.GetBytes("message")
});
```

## Basic Usage with Azure Service Bus

```csharp
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.AzureServiceBus;
using Relay.MessageBroker.ConnectionPool;

var services = new ServiceCollection();

services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.AzureServiceBus;
    options.AzureServiceBus = new AzureServiceBusOptions
    {
        ConnectionString = "Endpoint=sb://...",
        DefaultEntityName = "my-queue"
    };
    
    options.ConnectionPool = new ConnectionPoolOptions
    {
        Enabled = true,
        MinPoolSize = 2,
        MaxPoolSize = 10
    };
});

// Register Azure Service Bus client pool
services.AddConnectionPool(
    (sp, ct) => AzureServiceBusConnectionPoolIntegration.CreateClientFactory(
        sp.GetRequiredService<IOptions<MessageBrokerOptions>>().Value,
        sp.GetService<ILogger<ServiceBusClient>>()
    )(ct),
    new ConnectionPoolOptions { MinPoolSize = 2, MaxPoolSize = 10 },
    AzureServiceBusConnectionPoolIntegration.CreateClientValidator(),
    AzureServiceBusConnectionPoolIntegration.CreateClientDisposer()
);

var serviceProvider = services.BuildServiceProvider();

var clientPool = serviceProvider.GetRequiredService<IConnectionPool<ServiceBusClient>>();

await using var pooledClient = await clientPool.AcquireAsync();
var client = pooledClient.Connection;

// Use the client
var sender = client.CreateSender("my-queue");
await sender.SendMessageAsync(new ServiceBusMessage("Hello"));
```

## Monitoring Connection Pool Metrics

```csharp
var connectionPool = serviceProvider.GetRequiredService<IConnectionPool<IConnection>>();

// Get current metrics
var metrics = connectionPool.GetMetrics();

Console.WriteLine($"Active Connections: {metrics.ActiveConnections}");
Console.WriteLine($"Idle Connections: {metrics.IdleConnections}");
Console.WriteLine($"Total Connections: {metrics.TotalConnections}");
Console.WriteLine($"Waiting Threads: {metrics.WaitingThreads}");
Console.WriteLine($"Average Wait Time: {metrics.AverageWaitTimeMs}ms");
Console.WriteLine($"Total Created: {metrics.TotalConnectionsCreated}");
Console.WriteLine($"Total Disposed: {metrics.TotalConnectionsDisposed}");
```

## Custom Connection Pool

You can create a custom connection pool for any connection type:

```csharp
// Define your connection type
public class MyCustomConnection
{
    public bool IsConnected { get; set; }
    public void Close() { }
}

// Create a connection pool
var customPool = new ConnectionPoolManager<MyCustomConnection>(
    connectionFactory: async ct =>
    {
        // Create and return a new connection
        return new MyCustomConnection { IsConnected = true };
    },
    options: new ConnectionPoolOptions
    {
        MinPoolSize = 5,
        MaxPoolSize = 20,
        ConnectionTimeout = TimeSpan.FromSeconds(5),
        ValidationInterval = TimeSpan.FromSeconds(30),
        IdleTimeout = TimeSpan.FromMinutes(5)
    },
    logger: loggerFactory.CreateLogger<ConnectionPoolManager<MyCustomConnection>>(),
    connectionValidator: async conn =>
    {
        // Validate the connection
        return conn.IsConnected;
    },
    connectionDisposer: async conn =>
    {
        // Clean up the connection
        conn.Close();
    }
);

// Use the pool
await using var pooledConnection = await customPool.AcquireAsync();
var connection = pooledConnection.Connection;

// Use the connection
// ...

// Connection is automatically released when disposed
```

## Configuration Best Practices

### Development Environment
```csharp
options.ConnectionPool = new ConnectionPoolOptions
{
    Enabled = true,
    MinPoolSize = 2,      // Low minimum for development
    MaxPoolSize = 10,     // Low maximum for development
    ConnectionTimeout = TimeSpan.FromSeconds(10),
    ValidationInterval = TimeSpan.FromMinutes(1),
    IdleTimeout = TimeSpan.FromMinutes(2)
};
```

### Production Environment
```csharp
options.ConnectionPool = new ConnectionPoolOptions
{
    Enabled = true,
    MinPoolSize = 10,     // Higher minimum for production
    MaxPoolSize = 100,    // Higher maximum for production
    ConnectionTimeout = TimeSpan.FromSeconds(5),
    ValidationInterval = TimeSpan.FromSeconds(30),
    IdleTimeout = TimeSpan.FromMinutes(5)
};
```

### High-Throughput Scenarios
```csharp
options.ConnectionPool = new ConnectionPoolOptions
{
    Enabled = true,
    MinPoolSize = 20,     // Even higher minimum
    MaxPoolSize = 200,    // Even higher maximum
    ConnectionTimeout = TimeSpan.FromSeconds(3),
    ValidationInterval = TimeSpan.FromSeconds(15),
    IdleTimeout = TimeSpan.FromMinutes(10)
};
```

## Troubleshooting

### High Wait Times
If `AverageWaitTimeMs` is consistently high:
- Increase `MaxPoolSize`
- Check if connections are being held too long
- Verify connection validation isn't too slow

### Many Idle Connections
If `IdleConnections` is consistently high:
- Decrease `MinPoolSize`
- Decrease `IdleTimeout` to remove idle connections faster
- Consider if your workload has changed

### Connection Exhaustion
If you see timeout exceptions:
- Increase `MaxPoolSize`
- Increase `ConnectionTimeout`
- Check for connection leaks (not disposing properly)
- Monitor `WaitingThreads` metric

### Validation Failures
If connections are frequently invalid:
- Check network stability
- Adjust `ValidationInterval` (more frequent validation)
- Adjust `IdleTimeout` (remove idle connections sooner)
- Check broker health
