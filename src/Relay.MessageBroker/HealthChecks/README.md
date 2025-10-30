# Message Broker Health Checks

Comprehensive health check implementation for Relay.MessageBroker that integrates with ASP.NET Core health checks infrastructure.

## Features

- **Broker Connectivity Check**: Verifies the message broker is operational with configurable timeout (default 2 seconds)
- **Circuit Breaker State**: Monitors circuit breaker state and reports unhealthy when open
- **Connection Pool Metrics**: Tracks active, idle, and total connections with pool exhaustion detection
- **Configurable Intervals**: Minimum 5-second intervals for health check execution
- **Detailed Diagnostics**: Custom response writers for comprehensive health information
- **ASP.NET Core Integration**: Seamless integration with built-in health checks infrastructure

## Installation

The health check components are included in the `Relay.MessageBroker` package. Ensure you have the following NuGet packages:

```xml
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />
<PackageReference Include="Microsoft.AspNetCore.Diagnostics.HealthChecks" />
```

## Basic Usage

### 1. Register Health Checks

```csharp
using Relay.MessageBroker.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add message broker
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    // ... other configuration
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks();

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

### 2. Configure Health Check Options

```csharp
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(
        name: "MessageBroker",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "messagebroker", "ready" },
        timeout: TimeSpan.FromSeconds(5),
        configureOptions: options =>
        {
            options.Interval = TimeSpan.FromSeconds(30);
            options.ConnectivityTimeout = TimeSpan.FromSeconds(2);
            options.IncludeCircuitBreakerState = true;
            options.IncludeConnectionPoolMetrics = true;
        });
```

### 3. Use Detailed Response Writer

```csharp
using Relay.MessageBroker.HealthChecks;

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
});
```

## Advanced Configuration

### With Typed Connection Pool

If you're using a specific connection type with connection pooling:

```csharp
using RabbitMQ.Client;

builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks<IConnection>(
        name: "RabbitMQ",
        tags: new[] { "rabbitmq", "messagebroker" },
        configureOptions: options =>
        {
            options.IncludeConnectionPoolMetrics = true;
        });
```

### Multiple Health Check Endpoints

```csharp
// Liveness probe - basic check
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // Exclude all checks for basic liveness
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse
});

// Readiness probe - full checks
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
});
```

### Custom Tags and Filtering

```csharp
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(
        tags: new[] { "messagebroker", "critical", "ready" });

// Only check critical services
app.MapHealthChecks("/health/critical", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("critical")
});
```

## Health Check Response

### Simple Response

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "checks": [
    {
      "name": "MessageBroker",
      "status": "Healthy",
      "description": "Message broker is healthy"
    }
  ]
}
```

### Detailed Response

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "totalDuration": 45.23,
  "entries": {
    "MessageBroker": {
      "status": "Healthy",
      "description": "Message broker is healthy",
      "duration": 45.23,
      "data": {
        "broker_connected": true,
        "broker_type": "RabbitMQMessageBroker",
        "circuit_breaker_state": "Closed",
        "circuit_breaker_failure_count": 0,
        "circuit_breaker_success_count": 1523,
        "pool_active_connections": 5,
        "pool_idle_connections": 10,
        "pool_total_connections": 15,
        "pool_wait_time_ms": 2.5,
        "check_timestamp": "2024-01-15T10:30:00.000Z"
      },
      "tags": ["messagebroker", "ready"]
    }
  }
}
```

### Unhealthy Response

```json
{
  "status": "Unhealthy",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "totalDuration": 2005.67,
  "entries": {
    "MessageBroker": {
      "status": "Unhealthy",
      "description": "Message broker is unhealthy: Circuit breaker is open",
      "duration": 2005.67,
      "data": {
        "broker_connected": false,
        "broker_type": "RabbitMQMessageBroker",
        "circuit_breaker_state": "Open",
        "circuit_breaker_failure_count": 15,
        "circuit_breaker_success_count": 1523,
        "check_timestamp": "2024-01-15T10:30:00.000Z"
      },
      "tags": ["messagebroker", "ready"]
    }
  }
}
```

## Health Check Criteria

The health check reports **Unhealthy** when:

1. **Broker Connectivity Fails**: Cannot connect to the broker within the timeout period
2. **Circuit Breaker is Open**: The circuit breaker has tripped due to failures
3. **Connection Pool Exhausted**: All connections are in use (warning logged)

The health check reports **Healthy** when:

1. Broker is connected and operational
2. Circuit breaker is in Closed or Half-Open state
3. Connection pool has available capacity

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Interval` | `TimeSpan` | 30 seconds | Interval between health checks (minimum 5 seconds) |
| `ConnectivityTimeout` | `TimeSpan` | 2 seconds | Timeout for broker connectivity checks |
| `IncludeCircuitBreakerState` | `bool` | `true` | Include circuit breaker state in health checks |
| `IncludeConnectionPoolMetrics` | `bool` | `true` | Include connection pool metrics in health checks |
| `Name` | `string` | "MessageBroker" | Name of the health check |
| `Tags` | `string[]` | ["messagebroker", "ready"] | Tags for filtering health checks |

## Integration with Monitoring

### Kubernetes Probes

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: my-app
spec:
  containers:
  - name: app
    image: my-app:latest
    livenessProbe:
      httpGet:
        path: /health/live
        port: 8080
      initialDelaySeconds: 30
      periodSeconds: 10
    readinessProbe:
      httpGet:
        path: /health/ready
        port: 8080
      initialDelaySeconds: 10
      periodSeconds: 5
```

### Prometheus Monitoring

Use health check metrics with Prometheus:

```csharp
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks()
    .ForwardToPrometheus();
```

### Application Insights

```csharp
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks()
    .AddApplicationInsightsPublisher();
```

## Best Practices

1. **Use Appropriate Timeouts**: Set connectivity timeout based on your network latency
2. **Tag Your Checks**: Use tags to organize checks for different probe types
3. **Monitor Pool Exhaustion**: Watch for connection pool exhaustion warnings
4. **Separate Liveness and Readiness**: Use different endpoints for different probe types
5. **Configure Intervals**: Balance between responsiveness and overhead
6. **Use Detailed Responses in Dev**: Enable detailed diagnostics in development environments
7. **Keep Production Simple**: Use simple responses in production for performance

## Troubleshooting

### Health Check Always Returns Unhealthy

1. Check if the message broker is actually running
2. Verify connectivity timeout is sufficient for your network
3. Check circuit breaker configuration and state
4. Review logs for specific error messages

### Connection Pool Exhausted Warnings

1. Increase `MaxPoolSize` in connection pool options
2. Review application code for connection leaks
3. Monitor connection usage patterns
4. Consider implementing connection pooling if not already enabled

### Health Check Timeout

1. Increase the health check timeout parameter
2. Reduce connectivity timeout if it's too long
3. Check for network latency issues
4. Review broker performance

## Requirements Satisfied

This implementation satisfies the following requirements from the specification:

- **6.1**: Health check endpoint returns status within 2 seconds
- **6.2**: Reports health status for broker connectivity, circuit breaker state, and resource utilization
- **6.3**: Reports unhealthy status with diagnostic information when broker is unavailable
- **6.4**: Supports configurable health check intervals with minimum 5 seconds
- **6.5**: Integrates with ASP.NET Core health checks infrastructure

## See Also

- [ASP.NET Core Health Checks Documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Circuit Breaker Pattern](../CircuitBreaker/README.md)
- [Connection Pooling](../ConnectionPool/README.md)
