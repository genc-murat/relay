# Distributed Tracing Examples

## Example 1: Basic Setup with Console Exporter

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.DistributedTracing;

var builder = WebApplication.CreateBuilder(args);

// Add message broker with distributed tracing
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
});

// Add distributed tracing with console exporter (for development)
builder.Services.AddMessageBrokerOpenTelemetry(options =>
{
    options.ServiceName = "OrderService";
    options.EnableTracing = true;
    options.SamplingRate = 1.0; // Sample all traces in development
});

var app = builder.Build();
app.Run();
```

## Example 2: Production Setup with Jaeger

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.DistributedTracing;

var builder = WebApplication.CreateBuilder(args);

// Add message broker
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.Kafka;
    options.ConnectionString = "localhost:9092";
});

// Add distributed tracing with Jaeger exporter
builder.Services.AddMessageBrokerOpenTelemetry(options =>
{
    options.ServiceName = "PaymentService";
    options.EnableTracing = true;
    options.SamplingRate = 0.1; // Sample 10% of traces in production
    
    options.JaegerExporter = new JaegerExporterOptions
    {
        Enabled = true,
        AgentHost = "jaeger-agent",
        AgentPort = 6831
    };
});

var app = builder.Build();
app.Run();
```

## Example 3: Multi-Exporter Setup

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.DistributedTracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.AzureServiceBus;
    options.ConnectionString = "Endpoint=sb://...";
});

// Configure multiple exporters
builder.Services.AddMessageBrokerOpenTelemetry(options =>
{
    options.ServiceName = "NotificationService";
    options.EnableTracing = true;
    options.SamplingRate = 0.5;
    
    // Export to OpenTelemetry Collector
    options.OtlpExporter = new OtlpExporterOptions
    {
        Enabled = true,
        Endpoint = "http://otel-collector:4317",
        Protocol = "grpc"
    };
    
    // Also export to Jaeger for visualization
    options.JaegerExporter = new JaegerExporterOptions
    {
        Enabled = true,
        AgentHost = "localhost",
        AgentPort = 6831
    };
    
    // And Zipkin for compatibility
    options.ZipkinExporter = new ZipkinExporterOptions
    {
        Enabled = true,
        Endpoint = "http://localhost:9411/api/v2/spans"
    };
});

var app = builder.Build();
app.Run();
```

## Example 4: Publishing with Trace Context

```csharp
public class OrderController : ControllerBase
{
    private readonly IMessageBroker _messageBroker;

    public OrderController(IMessageBroker messageBroker)
    {
        _messageBroker = messageBroker;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = new OrderCreated
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Publish message - trace context is automatically injected
        await _messageBroker.PublishAsync(order, new PublishOptions
        {
            RoutingKey = "orders.created"
        });

        return Ok(new { orderId = order.OrderId });
    }
}
```

## Example 5: Consuming with Trace Context

```csharp
public class OrderConsumerService : BackgroundService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<OrderConsumerService> _logger;

    public OrderConsumerService(
        IMessageBroker messageBroker,
        ILogger<OrderConsumerService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to messages - trace context is automatically extracted
        await _messageBroker.SubscribeAsync<OrderCreated>(
            async (message, context, ct) =>
            {
                _logger.LogInformation(
                    "Processing order {OrderId} with correlation {CorrelationId}",
                    message.OrderId,
                    context.CorrelationId);

                // Process the order
                await ProcessOrder(message);

                // Acknowledge the message
                if (context.Acknowledge != null)
                {
                    await context.Acknowledge();
                }
            },
            new SubscriptionOptions
            {
                QueueName = "order-processing"
            },
            stoppingToken);
    }

    private async Task ProcessOrder(OrderCreated order)
    {
        // Business logic here
        // This runs within the traced span
        await Task.Delay(100); // Simulate processing
    }
}
```

## Example 6: Microservices Trace Flow

### Service A (Order Service) - Publisher

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;

    public async Task CreateOrder(CreateOrderRequest request)
    {
        // Create order
        var order = new Order { /* ... */ };
        
        // Publish OrderCreated event
        // Trace ID: 0af7651916cd43dd8448eb211c80319c
        // Span ID: b7ad6b7169203331
        await _messageBroker.PublishAsync(new OrderCreated
        {
            OrderId = order.Id,
            Amount = order.Amount
        });
    }
}
```

### Service B (Payment Service) - Consumer & Publisher

```csharp
public class PaymentService
{
    private readonly IMessageBroker _messageBroker;

    public async Task SetupSubscription()
    {
        await _messageBroker.SubscribeAsync<OrderCreated>(
            async (message, context, ct) =>
            {
                // This span is a child of the OrderCreated publish span
                // Trace ID: 0af7651916cd43dd8448eb211c80319c (same)
                // Span ID: c8be7c8270304442 (new child span)
                
                // Process payment
                var payment = await ProcessPayment(message);
                
                // Publish PaymentProcessed event
                // This creates another child span
                // Trace ID: 0af7651916cd43dd8448eb211c80319c (same)
                // Span ID: d9cf8d9381415553 (new child span)
                await _messageBroker.PublishAsync(new PaymentProcessed
                {
                    OrderId = message.OrderId,
                    PaymentId = payment.Id
                });
            });
    }
}
```

### Service C (Fulfillment Service) - Consumer

```csharp
public class FulfillmentService
{
    private readonly IMessageBroker _messageBroker;

    public async Task SetupSubscription()
    {
        await _messageBroker.SubscribeAsync<PaymentProcessed>(
            async (message, context, ct) =>
            {
                // This span is a child of the PaymentProcessed publish span
                // All spans share the same Trace ID
                // Trace ID: 0af7651916cd43dd8448eb211c80319c (same)
                // Span ID: eadf9ea492526664 (new child span)
                
                await FulfillOrder(message.OrderId);
            });
    }
}
```

The complete trace shows the flow:
```
Order Service (publish OrderCreated)
  └─> Payment Service (consume OrderCreated)
        └─> Payment Service (publish PaymentProcessed)
              └─> Fulfillment Service (consume PaymentProcessed)
```

## Example 7: Custom Span Attributes

```csharp
using System.Diagnostics;

public class OrderProcessor
{
    private readonly IMessageBroker _messageBroker;

    public async Task ProcessOrder(Order order)
    {
        await _messageBroker.PublishAsync(new OrderCreated
        {
            OrderId = order.Id,
            Amount = order.Amount
        }, new PublishOptions
        {
            RoutingKey = "orders.created",
            Headers = new Dictionary<string, object>
            {
                // Add custom business context
                ["tenant-id"] = order.TenantId,
                ["region"] = order.Region,
                ["priority"] = order.Priority
            }
        });

        // Add custom attributes to the current span
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("order.id", order.Id);
            activity.SetTag("order.amount", order.Amount);
            activity.SetTag("order.items_count", order.Items.Count);
        }
    }
}
```

## Example 8: Environment-Based Configuration

```csharp
using Relay.MessageBroker;
using Relay.MessageBroker.DistributedTracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = builder.Configuration["MessageBroker:ConnectionString"];
});

// Configure tracing based on environment
builder.Services.AddMessageBrokerOpenTelemetry(options =>
{
    options.ServiceName = builder.Configuration["ServiceName"] ?? "MyService";
    options.EnableTracing = builder.Configuration.GetValue<bool>("Tracing:Enabled", true);
    
    // Use different sampling rates per environment
    options.SamplingRate = builder.Environment.IsDevelopment() ? 1.0 : 0.1;
    
    // Configure exporters from configuration
    var jaegerHost = builder.Configuration["Tracing:Jaeger:Host"];
    if (!string.IsNullOrEmpty(jaegerHost))
    {
        options.JaegerExporter = new JaegerExporterOptions
        {
            Enabled = true,
            AgentHost = jaegerHost,
            AgentPort = builder.Configuration.GetValue<int>("Tracing:Jaeger:Port", 6831)
        };
    }
});

var app = builder.Build();
app.Run();
```

**appsettings.Development.json:**
```json
{
  "ServiceName": "OrderService",
  "Tracing": {
    "Enabled": true,
    "Jaeger": {
      "Host": "localhost",
      "Port": 6831
    }
  }
}
```

**appsettings.Production.json:**
```json
{
  "ServiceName": "OrderService",
  "Tracing": {
    "Enabled": true,
    "Jaeger": {
      "Host": "jaeger-agent.monitoring.svc.cluster.local",
      "Port": 6831
    }
  }
}
```

## Example 9: Docker Compose Setup

```yaml
version: '3.8'

services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # Jaeger UI
      - "6831:6831/udp"  # Jaeger agent
    environment:
      - COLLECTOR_ZIPKIN_HOST_PORT=:9411

  order-service:
    build: ./OrderService
    environment:
      - ServiceName=OrderService
      - Tracing__Enabled=true
      - Tracing__Jaeger__Host=jaeger
      - Tracing__Jaeger__Port=6831
      - MessageBroker__ConnectionString=amqp://rabbitmq
    depends_on:
      - jaeger
      - rabbitmq

  payment-service:
    build: ./PaymentService
    environment:
      - ServiceName=PaymentService
      - Tracing__Enabled=true
      - Tracing__Jaeger__Host=jaeger
      - Tracing__Jaeger__Port=6831
      - MessageBroker__ConnectionString=amqp://rabbitmq
    depends_on:
      - jaeger
      - rabbitmq

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
```

Access Jaeger UI at `http://localhost:16686` to view traces across services.
