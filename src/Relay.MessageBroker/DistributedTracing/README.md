# Distributed Tracing

The Distributed Tracing feature provides end-to-end tracing capabilities for message broker operations using OpenTelemetry and W3C Trace Context standards.

## Features

- **W3C Trace Context Propagation**: Automatic injection and extraction of trace context in message headers
- **OpenTelemetry Integration**: Full support for OpenTelemetry tracing with ActivitySource
- **Multiple Exporters**: Support for OTLP, Jaeger, and Zipkin exporters
- **Configurable Sampling**: Control trace sampling rates from 0.0 to 1.0
- **Rich Span Attributes**: Automatic capture of message metadata (type, size, broker type, routing key, etc.)
- **Parent-Child Relationships**: Proper span hierarchy for publish and consume operations

## Configuration

### Basic Setup

```csharp
services.AddMessageBrokerDistributedTracing(options =>
{
    options.EnableTracing = true;
    options.ServiceName = "MyService";
    options.SamplingRate = 1.0; // Sample all traces
});
```

### With OpenTelemetry Exporters

```csharp
services.AddMessageBrokerOpenTelemetry(options =>
{
    options.ServiceName = "MyService";
    options.SamplingRate = 0.1; // Sample 10% of traces
    
    // OTLP Exporter (e.g., for OpenTelemetry Collector)
    options.OtlpExporter = new OtlpExporterOptions
    {
        Enabled = true,
        Endpoint = "http://localhost:4317",
        Protocol = "grpc"
    };
    
    // Jaeger Exporter
    options.JaegerExporter = new JaegerExporterOptions
    {
        Enabled = true,
        AgentHost = "localhost",
        AgentPort = 6831
    };
    
    // Zipkin Exporter
    options.ZipkinExporter = new ZipkinExporterOptions
    {
        Enabled = true,
        Endpoint = "http://localhost:9411/api/v2/spans"
    };
});
```

## How It Works

### Publishing Messages

When you publish a message, the distributed tracing decorator:

1. Creates a new span with `ActivityKind.Producer`
2. Sets span attributes (message type, broker type, routing key, etc.)
3. Injects W3C Trace Context into message headers
4. Executes the publish operation
5. Records the processing duration and status

```csharp
await messageBroker.PublishAsync(new OrderCreated
{
    OrderId = "12345",
    Amount = 99.99m
});
// Trace context is automatically injected into message headers
```

### Consuming Messages

When you consume a message, the distributed tracing decorator:

1. Extracts W3C Trace Context from message headers
2. Creates a child span with `ActivityKind.Consumer` linked to the parent
3. Sets span attributes
4. Executes the message handler
5. Records the processing duration and status

```csharp
await messageBroker.SubscribeAsync<OrderCreated>(async (message, context, ct) =>
{
    // This handler runs within a traced span
    // The span is automatically linked to the publisher's span
    await ProcessOrder(message);
});
```

## Span Attributes

The following attributes are automatically captured:

- `messaging.message_type`: The message type name
- `messaging.message_size`: The message size in bytes (for publish)
- `messaging.broker_type`: The broker implementation (rabbitmq, kafka, etc.)
- `messaging.routing_key`: The routing key or topic
- `messaging.exchange`: The exchange name (for RabbitMQ)
- `messaging.correlation_id`: The correlation ID
- `messaging.processing_duration_ms`: The processing duration in milliseconds
- `messaging.operation`: The operation type (publish, consume)
- `messaging.destination`: The destination queue or topic
- `messaging.system`: The messaging system type

## W3C Trace Context Format

The trace context is propagated using the W3C Trace Context standard:

**traceparent header format:**
```
00-{trace-id}-{span-id}-{trace-flags}
```

Example:
```
00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
```

**tracestate header** (optional):
```
vendor1=value1,vendor2=value2
```

## Sampling Strategies

### Sample All Traces (Development)
```csharp
options.SamplingRate = 1.0;
```

### Sample 10% of Traces (Production)
```csharp
options.SamplingRate = 0.1;
```

### No Sampling (Disabled)
```csharp
options.SamplingRate = 0.0;
// or
options.EnableTracing = false;
```

## Exporter Configuration

### OTLP (OpenTelemetry Protocol)

Best for sending traces to OpenTelemetry Collector or compatible backends:

```csharp
options.OtlpExporter = new OtlpExporterOptions
{
    Enabled = true,
    Endpoint = "http://otel-collector:4317",
    Protocol = "grpc", // or "http/protobuf"
    Headers = new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer token123"
    }
};
```

### Jaeger

Direct export to Jaeger:

```csharp
options.JaegerExporter = new JaegerExporterOptions
{
    Enabled = true,
    AgentHost = "jaeger-agent",
    AgentPort = 6831,
    MaxPacketSize = 65000
};
```

### Zipkin

Direct export to Zipkin:

```csharp
options.ZipkinExporter = new ZipkinExporterOptions
{
    Enabled = true,
    Endpoint = "http://zipkin:9411/api/v2/spans",
    UseShortTraceIds = false
};
```

## Viewing Traces

### Jaeger UI

1. Start Jaeger: `docker run -d -p 16686:16686 -p 6831:6831/udp jaegertracing/all-in-one:latest`
2. Open browser: `http://localhost:16686`
3. Select your service name
4. View traces and spans

### Zipkin UI

1. Start Zipkin: `docker run -d -p 9411:9411 openzipkin/zipkin`
2. Open browser: `http://localhost:9411`
3. Search for traces

## Best Practices

1. **Use Meaningful Service Names**: Set a descriptive service name that identifies your application
2. **Adjust Sampling in Production**: Use lower sampling rates (0.01-0.1) in high-volume production environments
3. **Add Custom Attributes**: Extend spans with business-specific attributes when needed
4. **Monitor Exporter Health**: Ensure your trace exporters are healthy and receiving data
5. **Use Correlation IDs**: Set correlation IDs in PublishOptions for better trace correlation

## Performance Considerations

- Distributed tracing adds minimal overhead (~1-2ms per operation)
- Sampling reduces the performance impact in high-throughput scenarios
- Exporters send data asynchronously to avoid blocking message operations
- Consider using OTLP with OpenTelemetry Collector for better performance and reliability

## Troubleshooting

### Traces Not Appearing

1. Check that `EnableTracing = true`
2. Verify exporter configuration (endpoint, port)
3. Check sampling rate (must be > 0.0)
4. Ensure the exporter backend is running and accessible

### Missing Parent-Child Relationships

1. Verify W3C Trace Context headers are present in messages
2. Check that the consumer is extracting trace context correctly
3. Ensure both publisher and consumer have tracing enabled

### High Memory Usage

1. Reduce sampling rate
2. Configure exporter batch settings
3. Use OTLP with OpenTelemetry Collector as a buffer
