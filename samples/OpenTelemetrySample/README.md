# OpenTelemetry Integration Sample

This sample demonstrates comprehensive **OpenTelemetry** integration with Relay, showing how to implement distributed tracing, metrics collection, and observability across your application.

## What is OpenTelemetry?

OpenTelemetry is an open-source observability framework that provides:
- **Traces**: Distributed request tracing across services
- **Metrics**: Performance and business metrics
- **Logs**: Structured logging with context

It's vendor-neutral and supports multiple backend systems (Jaeger, Prometheus, Grafana, Datadog, etc.).

## Features Demonstrated

- ✅ Distributed tracing across handlers and services
- ✅ Automatic instrumentation for ASP.NET Core and HTTP calls
- ✅ Custom activity sources and spans
- ✅ Metrics collection and Prometheus export
- ✅ Error tracking and exception recording
- ✅ Performance monitoring
- ✅ Trace context propagation

## Architecture

```
API Request
    └─> Controller (Activity)
        └─> Handler (Activity)
            ├─> Service 1 (Activity)
            ├─> Service 2 (Activity)
            └─> Service 3 (Activity)
```

Each component creates its own span (activity), creating a complete trace tree.

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- (Optional) Docker for Jaeger UI
- (Optional) Prometheus for metrics

### Running the Sample

1. Start the application:
   ```bash
   cd samples/OpenTelemetrySample
   dotnet run
   ```

2. Access Swagger UI:
   ```
   https://localhost:7xxx/swagger
   ```

3. View metrics:
   ```
   https://localhost:7xxx/metrics
   ```

### (Optional) Run Jaeger for Trace Visualization

```bash
docker run -d --name jaeger \
  -p 6831:6831/udp \
  -p 16686:16686 \
  jaegertracing/all-in-one:latest
```

Then access Jaeger UI: `http://localhost:16686`

## API Endpoints

### 1. Create Order (Full Tracing)

**POST** `/api/orders`

Demonstrates complete distributed tracing through multiple layers.

Request:
```json
{
  "customerId": "CUST-001",
  "productId": "PROD-001",
  "quantity": 2,
  "price": 50.00
}
```

**Trace Flow:**
```
POST /api/orders (Controller)
  └─> CreateOrder (Handler)
      ├─> CheckInventory (InventoryService)
      ├─> ProcessPayment (PaymentHandler)
      │   └─> PaymentGateway (PaymentService)
      └─> SendEmail (EmailHandler)
          └─> SMTP (EmailService)
```

### 2. Slow Operation

**GET** `/api/orders/slow`

Demonstrates performance monitoring for slow operations.

### 3. Error Operation

**GET** `/api/orders/error`

Demonstrates error tracking and exception recording.

### 4. Complex Operation

**POST** `/api/orders/complex`

Demonstrates manual span creation with custom tags and events.

## Understanding the Traces

### Trace Structure

```
Trace ID: abc123...
├─ Span: POST /api/orders [200ms]
│  ├─ Tags: http.method=POST, http.status_code=200
│  ├─ Span: CreateOrder [180ms]
│  │  ├─ Tags: customer.id=CUST-001, order.id=ORDER-123
│  │  ├─ Span: CheckInventory [50ms]
│  │  │  └─ Tags: product.id=PROD-001, is.available=true
│  │  ├─ Span: ProcessPayment [100ms]
│  │  │  └─ Tags: amount=100.00, transaction.id=TXN-456
│  │  └─ Span: SendEmail [30ms]
│  │     └─ Tags: email.to=customer@example.com
```

### Activity Sources

The sample defines multiple activity sources:

- **Relay.API**: API controller activities
- **Relay.OrderProcessing**: Order handling activities
- **Relay.PaymentProcessing**: Payment activities
- **Relay.InventoryService**: Inventory operations
- **Relay.EmailService**: Email operations

## Configuration

### Basic OpenTelemetry Setup

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("MyService", "1.0.0");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Relay.*")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Relay.*")
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    });
```

### Enable in Relay

```csharp
builder.Services.AddRelay(options =>
{
    options.ScanAssemblies(typeof(Program).Assembly);
    options.EnableOpenTelemetry = true; // Enable OTel integration
});
```

## Exporters

### 1. Console Exporter (Default)

Exports traces to console output for development.

```csharp
.AddConsoleExporter()
```

### 2. Jaeger Exporter

Exports traces to Jaeger for visualization.

```csharp
.AddJaegerExporter(options =>
{
    options.AgentHost = "localhost";
    options.AgentPort = 6831;
})
```

### 3. OTLP Exporter

Exports to any OpenTelemetry-compatible backend.

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
})
```

### 4. Prometheus Exporter

Exposes metrics in Prometheus format.

```csharp
.AddPrometheusExporter()
```

Access metrics at: `/metrics`

## Creating Custom Spans

### Method 1: Using ActivitySource

```csharp
public class MyService
{
    private static readonly ActivitySource ActivitySource = 
        new("MyApp.MyService");

    public async Task DoWorkAsync()
    {
        using var activity = ActivitySource.StartActivity("DoWork");
        activity?.SetTag("user.id", userId);
        
        try
        {
            // Your work here
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Method 2: Nested Spans

```csharp
using var parent = ActivitySource.StartActivity("ParentOperation");

using (var child1 = ActivitySource.StartActivity("ChildOperation1"))
{
    // Work 1
}

using (var child2 = ActivitySource.StartActivity("ChildOperation2"))
{
    // Work 2
}
```

## Adding Custom Tags and Events

### Tags

Tags add metadata to spans:

```csharp
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.total", total);
activity?.SetTag("customer.tier", "premium");
```

### Events

Events mark specific points in time:

```csharp
activity?.AddEvent(new ActivityEvent("OrderValidated"));
activity?.AddEvent(new ActivityEvent("PaymentProcessed", 
    tags: new ActivityTagsCollection
    {
        { "transaction.id", transactionId },
        { "amount", amount }
    }));
```

### Exception Recording

```csharp
catch (Exception ex)
{
    activity?.RecordException(ex);
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
}
```

## Metrics Collection

### Counter

```csharp
private static readonly Counter<long> OrderCounter = 
    meter.CreateCounter<long>("orders.created");

OrderCounter.Add(1, new("status", "success"));
```

### Histogram

```csharp
private static readonly Histogram<double> OrderAmount = 
    meter.CreateHistogram<double>("order.amount");

OrderAmount.Record(total, new("currency", "USD"));
```

### Gauge

```csharp
private static readonly ObservableGauge<int> ActiveOrders = 
    meter.CreateObservableGauge("orders.active", () => GetActiveOrderCount());
```

## Querying Traces in Jaeger

1. Open Jaeger UI: `http://localhost:16686`
2. Select service: `OpenTelemetry.Sample`
3. Click "Find Traces"
4. Filter by:
   - Operation: e.g., "POST /api/orders"
   - Tags: e.g., `customer.id=CUST-001`
   - Duration: e.g., `> 100ms`

## Prometheus Metrics

Access metrics endpoint:
```
curl http://localhost:5000/metrics
```

Example metrics:
```
# HELP http_server_request_duration_seconds HTTP request duration
# TYPE http_server_request_duration_seconds histogram
http_server_request_duration_seconds_bucket{le="0.005"} 10
http_server_request_duration_seconds_bucket{le="0.01"} 25
http_server_request_duration_seconds_count 100
http_server_request_duration_seconds_sum 15.5
```

## Correlation IDs

OpenTelemetry automatically propagates trace context using W3C Trace Context headers:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
tracestate: rojo=00f067aa0ba902b7
```

These headers allow trace continuity across services.

## Production Best Practices

### 1. Sampling

Don't trace every request in production:

```csharp
.WithTracing(tracing =>
{
    tracing.SetSampler(new ParentBasedSampler(
        new TraceIdRatioBasedSampler(0.1))); // 10% sampling
})
```

### 2. Batch Processing

Export traces in batches:

```csharp
.AddOtlpExporter(options =>
{
    options.ExportProcessorType = ExportProcessorType.Batch;
    options.BatchExportProcessorOptions = new()
    {
        MaxQueueSize = 2048,
        ScheduledDelayMilliseconds = 5000,
        MaxExportBatchSize = 512
    };
})
```

### 3. Resource Attributes

Add service metadata:

```csharp
.ConfigureResource(resource =>
{
    resource
        .AddService("MyService", "1.0.0")
        .AddAttributes(new[]
        {
            new KeyValuePair<string, object>("deployment.environment", "production"),
            new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
        });
})
```

### 4. Sensitive Data

Avoid logging sensitive information:

```csharp
.AddAspNetCoreInstrumentation(options =>
{
    options.Filter = (context) => 
    {
        // Don't trace health checks
        return !context.Request.Path.StartsWithSegments("/health");
    };
    
    options.EnrichWithHttpRequest = (activity, request) =>
    {
        // Don't log authorization headers
        activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
    };
})
```

## Troubleshooting

### No Traces Appearing

1. Verify activity sources are registered:
   ```csharp
   .AddSource("Relay.*")
   .AddSource("MyApp.*")
   ```

2. Check if activities are started:
   ```csharp
   using var activity = ActivitySource.StartActivity("OperationName");
   if (activity == null)
   {
       // ActivitySource not listening
   }
   ```

3. Ensure exporters are configured

### High Memory Usage

- Reduce batch queue size
- Increase export frequency
- Enable sampling
- Limit tag cardinality

### Missing Child Spans

- Ensure `using` statements for proper disposal
- Check async/await patterns
- Verify activity context propagation

## Integration with Other Tools

### Grafana

```yaml
# docker-compose.yml
services:
  grafana:
    image: grafana/grafana
    ports:
      - 3000:3000
```

### Elastic APM

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("https://apm.elastic.co:443");
    options.Headers = "Authorization=Bearer <token>";
})
```

### Azure Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();
.AddAzureMonitorTraceExporter(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
})
```

## Related Samples

- **DistributedTracingSample**: Advanced tracing scenarios
- **ObservabilitySample**: Complete observability stack
- **MessageBroker.Sample**: Message broker tracing

## Learn More

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Relay OpenTelemetry Integration](../../docs/observability/opentelemetry.md)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)

## License

This sample is part of the Relay project and follows the same license.
