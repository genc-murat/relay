# Bulkhead Pattern Examples

This document provides practical examples of using the Bulkhead pattern with Relay.MessageBroker.

## Table of Contents

- [Basic Setup](#basic-setup)
- [Configuration Examples](#configuration-examples)
- [Usage Examples](#usage-examples)
- [Advanced Scenarios](#advanced-scenarios)
- [Monitoring and Metrics](#monitoring-and-metrics)

## Basic Setup

### Minimal Configuration

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.Bulkhead;

var builder = WebApplication.CreateBuilder(args);

// Add message broker
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
});

// Add bulkhead with default settings
builder.Services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = true;
});

// Decorate the message broker
builder.Services.DecorateMessageBrokerWithBulkhead();

var app = builder.Build();
app.Run();
```

### Full Configuration

```csharp
builder.Services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 100;
    options.MaxQueuedOperations = 1000;
    options.AcquisitionTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.DecorateMessageBrokerWithBulkhead();
```

## Configuration Examples

### High-Throughput System

For systems that need to handle high message volumes:

```csharp
builder.Services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 500;  // Allow many concurrent operations
    options.MaxQueuedOperations = 5000;     // Large queue for bursts
    options.AcquisitionTimeout = TimeSpan.FromMinutes(1);
});
```

### Resource-Constrained System

For systems with limited resources:

```csharp
builder.Services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 20;   // Limit concurrent operations
    options.MaxQueuedOperations = 100;      // Small queue
    options.AcquisitionTimeout = TimeSpan.FromSeconds(10);
});
```

### Development Environment

For development with relaxed limits:

```csharp
builder.Services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = false;  // Disable in development
});
```

## Usage Examples

### Example 1: Basic Publishing with Bulkhead

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMessageBroker messageBroker, ILogger<OrderService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task CreateOrderAsync(Order order)
    {
        try
        {
            // Publish is automatically protected by bulkhead
            await _messageBroker.PublishAsync(new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                Total = order.Total
            });

            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogWarning(
                "Failed to publish order event due to bulkhead rejection. " +
                "Active: {Active}, Queued: {Queued}",
                ex.ActiveOperations,
                ex.QueuedOperations);

            // Handle rejection - maybe store for later retry
            throw new InvalidOperationException("System is currently overloaded", ex);
        }
    }
}
```

### Example 2: Subscribing with Bulkhead Protection

```csharp
public class OrderProcessor : BackgroundService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(IMessageBroker messageBroker, ILogger<OrderProcessor> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe with bulkhead protection
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                try
                {
                    // Handler is automatically protected by bulkhead
                    await ProcessOrderAsync(message, ct);
                    _logger.LogInformation("Processed order {OrderId}", message.OrderId);
                }
                catch (BulkheadRejectedException ex)
                {
                    _logger.LogWarning(
                        "Order processing rejected by bulkhead. " +
                        "Active: {Active}, Queued: {Queued}",
                        ex.ActiveOperations,
                        ex.QueuedOperations);

                    // Reject without requeue - let broker retry mechanism handle it
                    await context.Reject(requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order {OrderId}", message.OrderId);
                    throw;
                }
            },
            cancellationToken: stoppingToken);

        await _messageBroker.StartAsync(stoppingToken);
    }

    private async Task ProcessOrderAsync(OrderCreatedEvent order, CancellationToken ct)
    {
        // Simulate processing
        await Task.Delay(100, ct);
        // Process order...
    }
}
```

### Example 3: Retry Logic for Rejected Operations

```csharp
public class ResilientPublisher
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<ResilientPublisher> _logger;

    public ResilientPublisher(IMessageBroker messageBroker, ILogger<ResilientPublisher> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task PublishWithRetryAsync<TMessage>(
        TMessage message,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                await _messageBroker.PublishAsync(message, cancellationToken: cancellationToken);
                _logger.LogInformation(
                    "Successfully published message of type {MessageType} on attempt {Attempt}",
                    typeof(TMessage).Name,
                    attempt + 1);
                return;
            }
            catch (BulkheadRejectedException ex)
            {
                attempt++;

                if (attempt >= maxRetries)
                {
                    _logger.LogError(
                        "Failed to publish message after {MaxRetries} attempts. " +
                        "Active: {Active}, Queued: {Queued}",
                        maxRetries,
                        ex.ActiveOperations,
                        ex.QueuedOperations);
                    throw;
                }

                // Exponential backoff
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                _logger.LogWarning(
                    "Bulkhead rejected publish attempt {Attempt}. Retrying in {Delay}ms",
                    attempt,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
```

## Advanced Scenarios

### Example 4: Custom Bulkhead Configuration per Environment

```csharp
public static class BulkheadConfiguration
{
    public static IServiceCollection AddEnvironmentSpecificBulkhead(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var environment = configuration["Environment"];

        services.AddMessageBrokerBulkhead(options =>
        {
            options.Enabled = true;

            switch (environment)
            {
                case "Production":
                    options.MaxConcurrentOperations = 500;
                    options.MaxQueuedOperations = 5000;
                    options.AcquisitionTimeout = TimeSpan.FromMinutes(1);
                    break;

                case "Staging":
                    options.MaxConcurrentOperations = 200;
                    options.MaxQueuedOperations = 2000;
                    options.AcquisitionTimeout = TimeSpan.FromSeconds(30);
                    break;

                case "Development":
                    options.MaxConcurrentOperations = 50;
                    options.MaxQueuedOperations = 500;
                    options.AcquisitionTimeout = TimeSpan.FromSeconds(10);
                    break;

                default:
                    options.Enabled = false;
                    break;
            }
        });

        services.DecorateMessageBrokerWithBulkhead();

        return services;
    }
}

// Usage
builder.Services.AddEnvironmentSpecificBulkhead(builder.Configuration);
```

### Example 5: Graceful Degradation

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public async Task CreateOrderAsync(Order order)
    {
        // Save order to database first
        await _repository.SaveAsync(order);

        try
        {
            // Try to publish event
            await _messageBroker.PublishAsync(new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId
            });
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogWarning(
                "Bulkhead rejected order event publish. Storing for later retry. " +
                "OrderId: {OrderId}, Active: {Active}, Queued: {Queued}",
                order.Id,
                ex.ActiveOperations,
                ex.QueuedOperations);

            // Store event for later retry via outbox pattern
            await StoreEventForRetryAsync(order.Id);

            // Don't fail the order creation - graceful degradation
        }
    }

    private async Task StoreEventForRetryAsync(Guid orderId)
    {
        // Store in outbox table for later processing
        await _repository.SaveOutboxEventAsync(new OutboxEvent
        {
            EventType = nameof(OrderCreatedEvent),
            EntityId = orderId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

## Monitoring and Metrics

### Example 6: Metrics Collection

```csharp
public class BulkheadMetricsCollector : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulkheadMetricsCollector> _logger;

    public BulkheadMetricsCollector(
        IServiceProvider serviceProvider,
        ILogger<BulkheadMetricsCollector> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get the bulkhead decorator
                var messageBroker = _serviceProvider.GetService<IMessageBroker>();
                if (messageBroker is BulkheadMessageBrokerDecorator decorator)
                {
                    var publishMetrics = decorator.GetPublishMetrics();
                    var subscribeMetrics = decorator.GetSubscribeMetrics();

                    _logger.LogInformation(
                        "Bulkhead Metrics - " +
                        "Publish: Active={PublishActive}, Queued={PublishQueued}, Rejected={PublishRejected} | " +
                        "Subscribe: Active={SubscribeActive}, Queued={SubscribeQueued}, Rejected={SubscribeRejected}",
                        publishMetrics.ActiveOperations,
                        publishMetrics.QueuedOperations,
                        publishMetrics.RejectedOperations,
                        subscribeMetrics.ActiveOperations,
                        subscribeMetrics.QueuedOperations,
                        subscribeMetrics.RejectedOperations);

                    // Send metrics to monitoring system
                    await SendMetricsAsync(publishMetrics, subscribeMetrics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting bulkhead metrics");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task SendMetricsAsync(
        BulkheadMetrics publishMetrics,
        BulkheadMetrics subscribeMetrics)
    {
        // Send to your monitoring system (Prometheus, Application Insights, etc.)
        await Task.CompletedTask;
    }
}

// Register the collector
builder.Services.AddHostedService<BulkheadMetricsCollector>();
```

### Example 7: Health Check Integration

```csharp
public class BulkheadHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public BulkheadHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messageBroker = _serviceProvider.GetService<IMessageBroker>();
            if (messageBroker is not BulkheadMessageBrokerDecorator decorator)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Bulkhead not configured"));
            }

            var publishMetrics = decorator.GetPublishMetrics();
            var subscribeMetrics = decorator.GetSubscribeMetrics();

            var data = new Dictionary<string, object>
            {
                ["publish_active"] = publishMetrics.ActiveOperations,
                ["publish_queued"] = publishMetrics.QueuedOperations,
                ["publish_rejected"] = publishMetrics.RejectedOperations,
                ["subscribe_active"] = subscribeMetrics.ActiveOperations,
                ["subscribe_queued"] = subscribeMetrics.QueuedOperations,
                ["subscribe_rejected"] = subscribeMetrics.RejectedOperations
            };

            // Check if rejection rate is too high
            var totalPublish = publishMetrics.ExecutedOperations + publishMetrics.RejectedOperations;
            var publishRejectionRate = totalPublish > 0
                ? (double)publishMetrics.RejectedOperations / totalPublish
                : 0;

            if (publishRejectionRate > 0.1) // More than 10% rejection rate
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High bulkhead rejection rate: {publishRejectionRate:P}",
                    data: data));
            }

            // Check if queue is consistently full
            if (publishMetrics.QueuedOperations >= publishMetrics.MaxQueuedOperations * 0.9)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Bulkhead queue is nearly full",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Bulkhead is healthy", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Error checking bulkhead health",
                ex));
        }
    }
}

// Register the health check
builder.Services.AddHealthChecks()
    .AddCheck<BulkheadHealthCheck>("bulkhead");
```

### Example 8: Alert on High Rejection Rate

```csharp
public class BulkheadAlertService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulkheadAlertService> _logger;
    private readonly IAlertService _alertService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messageBroker = _serviceProvider.GetService<IMessageBroker>();
                if (messageBroker is BulkheadMessageBrokerDecorator decorator)
                {
                    var publishMetrics = decorator.GetPublishMetrics();

                    // Calculate rejection rate
                    var total = publishMetrics.ExecutedOperations + publishMetrics.RejectedOperations;
                    if (total > 100) // Only alert if we have enough samples
                    {
                        var rejectionRate = (double)publishMetrics.RejectedOperations / total;

                        if (rejectionRate > 0.05) // More than 5% rejection rate
                        {
                            await _alertService.SendAlertAsync(
                                "High Bulkhead Rejection Rate",
                                $"Rejection rate: {rejectionRate:P}, " +
                                $"Active: {publishMetrics.ActiveOperations}, " +
                                $"Queued: {publishMetrics.QueuedOperations}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulkhead alert service");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## Complete Example Application

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Relay.MessageBroker;
using Relay.MessageBroker.Bulkhead;

var builder = WebApplication.CreateBuilder(args);

// Add message broker with bulkhead
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = builder.Configuration.GetConnectionString("RabbitMQ");
});

builder.Services.AddMessageBrokerBulkhead(options =>
{
    options.Enabled = true;
    options.MaxConcurrentOperations = 100;
    options.MaxQueuedOperations = 1000;
    options.AcquisitionTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.DecorateMessageBrokerWithBulkhead();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<BulkheadHealthCheck>("bulkhead");

// Add services
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<OrderProcessor>();
builder.Services.AddHostedService<BulkheadMetricsCollector>();

var app = builder.Build();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map API endpoints
app.MapPost("/orders", async (Order order, OrderService service) =>
{
    try
    {
        await service.CreateOrderAsync(order);
        return Results.Ok();
    }
    catch (BulkheadRejectedException ex)
    {
        return Results.StatusCode(503); // Service Unavailable
    }
});

app.Run();
```
