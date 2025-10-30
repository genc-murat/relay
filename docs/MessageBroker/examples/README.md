# Relay.MessageBroker Examples

This directory contains comprehensive code examples demonstrating the various enhancements to Relay.MessageBroker.

## Examples Overview

### 1. OutboxPatternExample.cs
Demonstrates the Outbox Pattern with SQL Server for reliable message publishing.

**Features:**
- Transactional consistency between database and message broker
- Automatic retry with exponential backoff
- Failed message handling
- Monitoring and diagnostics

**Use Cases:**
- Ensuring message delivery even if broker is temporarily unavailable
- Maintaining consistency between database operations and message publishing
- Preventing dual-write problems

**Run:**
```bash
dotnet run --project OutboxPatternExample
```

### 2. InboxPatternExample.cs
Demonstrates the Inbox Pattern with PostgreSQL for idempotent message processing.

**Features:**
- Duplicate message detection and filtering
- Custom idempotency keys
- Automatic cleanup of old entries
- Statistics and monitoring

**Use Cases:**
- Preventing duplicate message processing
- Handling at-least-once delivery semantics
- Ensuring exactly-once processing semantics

**Run:**
```bash
dotnet run --project InboxPatternExample
```

### 3. ComprehensiveExample.cs
Demonstrates multiple enhancements working together in a production-like scenario.

**Features:**
- Message encryption with Azure Key Vault
- Authentication and authorization with JWT
- Rate limiting with per-tenant limits
- Distributed tracing with Jaeger
- Batch processing with compression
- Connection pooling
- Message deduplication
- Health checks
- Prometheus metrics

**Use Cases:**
- Production deployments requiring multiple enhancements
- Secure message handling with sensitive data
- High-performance scenarios
- Multi-tenant applications

**Run:**
```bash
dotnet run --project ComprehensiveExample
```

## Additional Examples

### Connection Pooling
See `src/Relay.MessageBroker/ConnectionPool/EXAMPLE.md` for connection pooling examples.

### Batch Processing
See `src/Relay.MessageBroker/Batch/EXAMPLE.md` for batch processing examples.

### Message Deduplication
See `src/Relay.MessageBroker/Deduplication/EXAMPLE.md` for deduplication examples.

### Health Checks
See `src/Relay.MessageBroker/HealthChecks/EXAMPLE.md` for health check examples.

### Metrics and Telemetry
See `src/Relay.MessageBroker/Metrics/EXAMPLE.md` for metrics examples.

### Distributed Tracing
See `src/Relay.MessageBroker/DistributedTracing/EXAMPLE.md` for tracing examples.

### Message Encryption
See `src/Relay.MessageBroker/Security/EXAMPLE.md` for encryption examples.

### Authentication and Authorization
See `src/Relay.MessageBroker/Security/AUTHENTICATION_EXAMPLE.md` for authentication examples.

### Rate Limiting
See `src/Relay.MessageBroker/RateLimit/EXAMPLE.md` for rate limiting examples.

### Bulkhead Pattern
See `src/Relay.MessageBroker/Bulkhead/EXAMPLE.md` for bulkhead examples.

### Poison Message Handling
See `src/Relay.MessageBroker/PoisonMessage/EXAMPLE.md` for poison message examples.

### Backpressure Management
See `src/Relay.MessageBroker/Backpressure/EXAMPLE.md` for backpressure examples.

## Prerequisites

### Required Software
- .NET 8.0 SDK or later
- Docker (for running message brokers and databases)

### Optional Software
- SQL Server (for Outbox pattern)
- PostgreSQL (for Inbox pattern)
- RabbitMQ (message broker)
- Jaeger (distributed tracing)
- Prometheus (metrics)
- Grafana (visualization)

## Quick Start

### 1. Start Infrastructure with Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourStrong@Passw0rd

  postgres:
    image: postgres:15
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: messagebroker

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "6831:6831/udp"
      - "16686:16686"

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_PASSWORD: admin
```

```bash
docker-compose up -d
```

### 2. Configure Connection Strings

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=MessageBroker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True",
    "PostgresConnection": "Host=localhost;Port=5432;Database=messagebroker;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "Azure": {
    "KeyVault": {
      "Url": "https://your-keyvault.vault.azure.net/"
    }
  },
  "Jwt": {
    "Issuer": "https://auth.example.com",
    "Audience": "message-broker",
    "SigningKey": "your-secret-signing-key-min-32-chars"
  }
}
```

### 3. Run Migrations

```bash
# For Outbox pattern
dotnet ef database update --context OutboxDbContext

# For Inbox pattern
dotnet ef database update --context InboxDbContext
```

### 4. Run Examples

```bash
# Run Outbox example
cd examples/OutboxPatternExample
dotnet run

# Run Inbox example
cd examples/InboxPatternExample
dotnet run

# Run comprehensive example
cd examples/ComprehensiveExample
dotnet run
```

## Testing Examples

### Unit Testing

```csharp
public class OutboxPatternTests
{
    [Fact]
    public async Task OutboxPattern_StoresAndProcessesMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOutboxPattern(options => options.Enabled = true);
        services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
        var provider = services.BuildServiceProvider();
        
        var messageBroker = provider.GetRequiredService<IMessageBroker>();
        var outboxStore = provider.GetRequiredService<IOutboxStore>();
        
        // Act
        await messageBroker.PublishAsync(new OrderCreatedEvent { OrderId = 123 });
        
        // Assert
        var pending = await outboxStore.GetPendingAsync(10);
        Assert.Single(pending);
    }
}
```

### Integration Testing

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task EndToEnd_WithAllFeatures()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        
        // Act
        var response = await client.PostAsync("/orders", 
            new StringContent(JsonSerializer.Serialize(new { orderId = 123 })));
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Monitoring Examples

### View Metrics

```bash
# Prometheus metrics
curl http://localhost:5000/metrics

# Health check
curl http://localhost:5000/health
```

### View Traces

Open Jaeger UI: http://localhost:16686

### View Dashboards

Open Grafana: http://localhost:3000 (admin/admin)

## Common Patterns

### Pattern 1: Reliable Messaging

```csharp
// Use Outbox + Inbox for guaranteed exactly-once processing
services.AddOutboxPattern(options => options.Enabled = true);
services.AddInboxPattern(options => options.Enabled = true);
```

### Pattern 2: High Performance

```csharp
// Use Connection Pool + Batch Processing + Deduplication
services.AddConnectionPooling<IConnection>(options => { });
services.AddBatchProcessing(options => options.Enabled = true);
services.AddDeduplication(options => options.Enabled = true);
```

### Pattern 3: Secure Messaging

```csharp
// Use Encryption + Authentication + Rate Limiting
services.AddMessageEncryption(options => options.EnableEncryption = true);
services.AddMessageBrokerSecurity(options => options.EnableAuthentication = true);
services.AddRateLimiting(options => options.Enabled = true);
```

### Pattern 4: Observable System

```csharp
// Use Health Checks + Metrics + Distributed Tracing
services.AddMessageBrokerHealthChecks();
services.AddMessageBrokerMetrics(options => options.EnableMetrics = true);
services.AddDistributedTracing(options => options.EnableTracing = true);
```

### Pattern 5: Resilient System

```csharp
// Use Bulkhead + Poison Message Handling + Backpressure
services.AddBulkhead(options => options.Enabled = true);
services.AddPoisonMessageHandling(options => options.Enabled = true);
services.AddBackpressure(options => options.Enabled = true);
```

## Troubleshooting

### Example Not Running

1. Check if all required services are running:
   ```bash
   docker ps
   ```

2. Check connection strings in appsettings.json

3. Run migrations:
   ```bash
   dotnet ef database update
   ```

### Messages Not Being Processed

1. Check if message broker is running:
   ```bash
   curl http://localhost:15672  # RabbitMQ management UI
   ```

2. Check logs for errors:
   ```bash
   dotnet run --verbosity detailed
   ```

3. Verify configuration:
   ```csharp
   var options = serviceProvider.GetRequiredService<IOptions<MessageBrokerOptions>>();
   Console.WriteLine(JsonSerializer.Serialize(options.Value));
   ```

### Performance Issues

1. Enable metrics and check:
   ```bash
   curl http://localhost:5000/metrics | grep relay_
   ```

2. Check connection pool metrics
3. Verify batch processing is enabled
4. Check for slow consumers

## Additional Resources

- [Getting Started Guide](../GETTING_STARTED.md)
- [Configuration Guide](../CONFIGURATION.md)
- [Best Practices](../BEST_PRACTICES.md)
- [Troubleshooting Guide](../TROUBLESHOOTING.md)
- [Migration Guide](../MIGRATION.md)

## Support

- [GitHub Issues](https://github.com/your-org/relay/issues)
- [Documentation](https://docs.relay.dev)
- [Discussions](https://github.com/your-org/relay/discussions)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](../../../CONTRIBUTING.md) for details.

## License

MIT License - see [LICENSE](../../../LICENSE) for details.
