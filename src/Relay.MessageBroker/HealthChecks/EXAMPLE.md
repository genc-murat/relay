# Message Broker Health Checks - Examples

This document provides practical examples for implementing health checks in various scenarios.

## Example 1: Basic Web API with Health Checks

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add message broker
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost:5672";
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks();

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

## Example 2: Kubernetes-Ready Application

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure message broker
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.ConnectionString = "localhost:9092";
});

// Add circuit breaker
builder.Services.AddCircuitBreaker(options =>
{
    options.FailureThreshold = 5;
    options.SuccessThreshold = 2;
    options.Timeout = TimeSpan.FromSeconds(30);
});

// Add connection pooling
builder.Services.AddConnectionPool<IConnection>(options =>
{
    options.MinPoolSize = 5;
    options.MaxPoolSize = 50;
});

// Add health checks with detailed configuration
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks<IConnection>(
        name: "kafka-broker",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "kafka", "messagebroker", "ready" },
        timeout: TimeSpan.FromSeconds(5),
        configureOptions: options =>
        {
            options.Interval = TimeSpan.FromSeconds(10);
            options.ConnectivityTimeout = TimeSpan.FromSeconds(2);
            options.IncludeCircuitBreakerState = true;
            options.IncludeConnectionPoolMetrics = true;
        });

var app = builder.Build();

// Liveness probe - simple check
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse
});

// Readiness probe - full checks
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
});

app.Run();
```

## Example 3: Microservices with Multiple Brokers

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Primary message broker (RabbitMQ)
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://rabbitmq:5672";
});

// Add health checks for primary broker
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(
        name: "primary-broker",
        tags: new[] { "primary", "messagebroker", "ready" },
        configureOptions: options =>
        {
            options.Name = "RabbitMQ Primary";
            options.ConnectivityTimeout = TimeSpan.FromSeconds(2);
        });

var app = builder.Build();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
});

app.MapHealthChecks("/health/primary", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("primary"),
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
});

app.Run();
```

## Example 4: Development vs Production Configuration

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.AzureServiceBus;
    options.ConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
});

// Configure health checks based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHealthChecks()
        .AddMessageBrokerHealthChecks(
            configureOptions: options =>
            {
                options.Interval = TimeSpan.FromSeconds(5);
                options.ConnectivityTimeout = TimeSpan.FromSeconds(5);
            });
}
else
{
    builder.Services.AddHealthChecks()
        .AddMessageBrokerHealthChecks(
            configureOptions: options =>
            {
                options.Interval = TimeSpan.FromSeconds(30);
                options.ConnectivityTimeout = TimeSpan.FromSeconds(2);
            });
}

var app = builder.Build();

// Use detailed response in development, simple in production
var responseWriter = builder.Environment.IsDevelopment()
    ? MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
    : MessageBrokerHealthCheckResponseWriter.WriteSimpleResponse;

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = responseWriter
});

app.Run();
```

## Example 5: Custom Health Check Logic

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost:5672";
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(
        failureStatus: HealthStatus.Degraded, // Report as degraded instead of unhealthy
        configureOptions: options =>
        {
            options.IncludeCircuitBreakerState = true;
            options.IncludeConnectionPoolMetrics = true;
        })
    .AddCheck("custom-check", () =>
    {
        // Add custom health check logic
        return HealthCheckResult.Healthy("Custom check passed");
    });

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.Run();
```

## Example 6: Health Check UI Integration

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;
using HealthChecks.UI.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost:5672";
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks();

// Add health checks UI
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(10);
    setup.AddHealthCheckEndpoint("Message Broker", "/health");
}).AddInMemoryStorage();

var app = builder.Build();

// Map health check endpoint with UI client response writer
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Map health checks UI
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
});

app.Run();
```

## Example 7: Monitoring with Application Insights

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.AzureServiceBus;
    options.ConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
});

// Add health checks with Application Insights publisher
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(
        tags: new[] { "messagebroker", "critical" })
    .AddApplicationInsightsPublisher();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = MessageBrokerHealthCheckResponseWriter.WriteDetailedResponse
});

app.Run();
```

## Example 8: Testing Health Checks

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using Xunit;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy_WhenBrokerIsOperational()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task HealthCheck_ReturnsDetailedDiagnostics()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("broker_connected", content);
        Assert.Contains("circuit_breaker_state", content);
        Assert.Contains("pool_active_connections", content);
    }
}
```

## Example 9: Docker Compose Health Checks

```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MessageBroker__ConnectionString=amqp://rabbitmq:5672
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 40s
    depends_on:
      rabbitmq:
        condition: service_healthy

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
```

## Example 10: Custom Response Format

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost:5672";
});

builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks();

var app = builder.Build();

// Custom response writer
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            service = "MyService",
            version = "1.0.0",
            checks = report.Entries.Select(e => new
            {
                component = e.Key,
                status = e.Value.Status.ToString(),
                duration = $"{e.Value.Duration.TotalMilliseconds}ms",
                data = e.Value.Data
            })
        };

        await context.Response.WriteAsJsonAsync(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
});

app.Run();
```

## Testing the Health Checks

### Using curl

```bash
# Basic health check
curl http://localhost:8080/health

# Readiness probe
curl http://localhost:8080/health/ready

# Liveness probe
curl http://localhost:8080/health/live

# With verbose output
curl -v http://localhost:8080/health
```

### Using PowerShell

```powershell
# Basic health check
Invoke-RestMethod -Uri "http://localhost:8080/health" -Method Get

# With formatted output
Invoke-RestMethod -Uri "http://localhost:8080/health" -Method Get | ConvertTo-Json -Depth 10
```

### Using HTTPie

```bash
# Basic health check
http GET http://localhost:8080/health

# Pretty print
http GET http://localhost:8080/health --pretty=all
```

## Common Patterns

### Pattern 1: Graceful Degradation

```csharp
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(
        failureStatus: HealthStatus.Degraded, // Don't fail completely
        configureOptions: options =>
        {
            options.IncludeCircuitBreakerState = true;
        });
```

### Pattern 2: Separate Concerns

```csharp
// Infrastructure health
builder.Services.AddHealthChecks()
    .AddMessageBrokerHealthChecks(tags: new[] { "infrastructure" })
    .AddDbContextCheck<AppDbContext>(tags: new[] { "infrastructure" });

// Application health
builder.Services.AddHealthChecks()
    .AddCheck("business-logic", () => HealthCheckResult.Healthy(), tags: new[] { "application" });
```

### Pattern 3: Conditional Checks

```csharp
var healthChecksBuilder = builder.Services.AddHealthChecks();

if (builder.Configuration.GetValue<bool>("Features:MessageBroker"))
{
    healthChecksBuilder.AddMessageBrokerHealthChecks();
}
```

These examples demonstrate various ways to implement and use message broker health checks in different scenarios and environments.
