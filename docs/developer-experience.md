# Relay Developer Experience Features

This document covers the developer experience improvements available in Relay, including diagnostics, compile-time validation, and enhanced testing utilities.

## Diagnostics and Runtime Inspection

Relay provides comprehensive diagnostics capabilities to help you understand and debug your application's behavior at runtime.

### Request Tracing

The request tracing system captures detailed execution information for each request processed through Relay.

```csharp
// Enable diagnostics in your startup
services.AddRelay()
    .AddDiagnostics();

// Access tracing information
public class MyHandler
{
    private readonly IRequestTracer _tracer;
    
    public MyHandler(IRequestTracer tracer)
    {
        _tracer = tracer;
    }
    
    [Handle]
    public async ValueTask<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        _tracer.RecordStep("Processing started", new { RequestId = request.Id });
        
        // Your handler logic here
        
        _tracer.RecordStep("Processing completed");
        return new MyResponse();
    }
}
```

### Diagnostic Endpoints

Relay automatically exposes diagnostic endpoints when diagnostics are enabled:

- `GET /relay/handlers` - Lists all registered handlers with their metadata
- `GET /relay/metrics` - Provides performance metrics and statistics
- `GET /relay/health` - Configuration validation and health checks

### Handler Registry Inspection

You can inspect the handler registry at runtime:

```csharp
public class DiagnosticsController : ControllerBase
{
    private readonly IRelayDiagnostics _diagnostics;
    
    public DiagnosticsController(IRelayDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }
    
    [HttpGet("handlers")]
    public async Task<HandlerRegistryInfo> GetHandlers()
    {
        return await _diagnostics.GetHandlerRegistryAsync();
    }
}
```

## Compile-Time Validation

The Relay analyzer provides compile-time validation to catch common issues before runtime.

### Handler Signature Validation

The analyzer validates that your handler methods have correct signatures:

```csharp
// ✅ Valid handler
[Handle]
public ValueTask<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
{
    // Implementation
}

// ❌ Invalid - missing CancellationToken (will show compiler warning)
[Handle]
public ValueTask<MyResponse> Handle(MyRequest request)
{
    // Implementation
}

// ❌ Invalid - wrong return type (will show compiler error)
[Handle]
public string Handle(MyRequest request, CancellationToken cancellationToken)
{
    // Implementation
}
```

### Duplicate Handler Detection

The analyzer detects when multiple handlers are registered for the same request type:

```csharp
// ❌ This will cause a compilation error
public class FirstHandler
{
    [Handle]
    public ValueTask<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

public class SecondHandler
{
    [Handle] // Error: Duplicate handler for MyRequest
    public ValueTask<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// ✅ Use named handlers to resolve conflicts
public class FirstHandler
{
    [Handle(Name = "primary")]
    public ValueTask<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

public class SecondHandler
{
    [Handle(Name = "secondary")]
    public ValueTask<MyResponse> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Enhanced Testing Utilities

Relay provides powerful testing utilities to make testing your handlers easier and more reliable.

### RelayTestBuilder

The fluent test builder API simplifies test setup:

```csharp
[Test]
public async Task Should_Process_Request_Successfully()
{
    // Arrange
    var relay = new RelayTestBuilder()
        .WithHandler<MyRequest, MyResponse>((req, ct) => 
            ValueTask.FromResult(new MyResponse { Message = "Success" }))
        .WithMockHandler<AnotherRequest, AnotherResponse>()
        .Build();
    
    // Act
    var response = await relay.Send(new MyRequest { Id = 1 });
    
    // Assert
    Assert.That(response.Message, Is.EqualTo("Success"));
}
```

### RelayAssertions

Specialized assertion methods for common Relay testing patterns:

```csharp
[Test]
public async Task Should_Execute_Pipeline_In_Correct_Order()
{
    // Arrange
    var relay = new RelayTestBuilder()
        .WithHandler<MyRequest, MyResponse>(handler)
        .WithPipeline<LoggingPipeline>()
        .WithPipeline<ValidationPipeline>()
        .Build();
    
    // Act
    await relay.Send(new MyRequest());
    
    // Assert
    RelayAssertions.AssertPipelineOrder(relay, 
        typeof(ValidationPipeline), 
        typeof(LoggingPipeline));
}

[Test]
public async Task Should_Complete_Within_Performance_Threshold()
{
    // Arrange
    var relay = new RelayTestBuilder()
        .WithHandler<MyRequest, MyResponse>(handler)
        .Build();
    
    // Act & Assert
    await RelayAssertions.AssertPerformance(
        () => relay.Send(new MyRequest()),
        maxDuration: TimeSpan.FromMilliseconds(100),
        maxAllocations: 1024);
}
```

### Enhanced RelayTestHarness

The test harness now includes trace capture and performance measurement:

```csharp
[Test]
public async Task Should_Capture_Execution_Trace()
{
    // Arrange
    using var harness = new RelayTestHarness();
    harness.RegisterHandler<MyRequest, MyResponse>(handler);
    
    // Act
    var response = await harness.Send(new MyRequest());
    
    // Assert
    var trace = harness.GetExecutionTrace();
    Assert.That(trace.Steps, Has.Count.GreaterThan(0));
    Assert.That(trace.TotalDuration, Is.LessThan(TimeSpan.FromSeconds(1)));
}

[Test]
public async Task Should_Measure_Performance_Metrics()
{
    // Arrange
    using var harness = new RelayTestHarness();
    harness.RegisterHandler<MyRequest, MyResponse>(handler);
    
    // Act
    var metrics = await harness.MeasurePerformance(
        () => harness.Send(new MyRequest()),
        iterations: 100);
    
    // Assert
    Assert.That(metrics.AverageExecutionTime, Is.LessThan(TimeSpan.FromMilliseconds(10)));
    Assert.That(metrics.TotalAllocatedBytes, Is.LessThan(1024));
}
```

## Configuration

### Enabling Diagnostics

Add diagnostics to your service collection:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRelay()
        .AddDiagnostics(options =>
        {
            options.EnableRequestTracing = true;
            options.EnablePerformanceMetrics = true;
            options.ExposeEndpoints = true;
        });
}
```

### Analyzer Configuration

The Relay analyzer is automatically included when you reference the Relay.SourceGenerator package. You can configure it through an `.editorconfig` file:

```ini
[*.cs]
# Configure Relay analyzer rules
dotnet_diagnostic.RELAY001.severity = error    # Missing handler
dotnet_diagnostic.RELAY002.severity = warning  # Invalid signature
dotnet_diagnostic.RELAY003.severity = error    # Duplicate handler
```

## Best Practices

### Testing

1. **Use the test builder for simple scenarios**: The fluent API makes test setup clear and concise
2. **Leverage assertions for complex validations**: Use RelayAssertions for pipeline order and performance testing
3. **Capture traces for debugging**: Use the test harness trace capture to understand execution flow
4. **Measure performance regularly**: Include performance assertions in your test suite

### Diagnostics

1. **Enable diagnostics in development**: Always enable diagnostics in development environments
2. **Use structured logging**: Record meaningful trace steps with structured data
3. **Monitor the diagnostic endpoints**: Set up monitoring for the health and metrics endpoints
4. **Review handler registry**: Regularly inspect the handler registry to ensure correct configuration

### Compile-Time Validation

1. **Treat analyzer warnings as errors**: Configure your build to treat Relay analyzer warnings as errors
2. **Use named handlers for conflicts**: When you need multiple handlers for the same request type, use named handlers
3. **Follow signature conventions**: Always include CancellationToken parameters and use appropriate return types
4. **Keep handlers focused**: Each handler should have a single responsibility

## Troubleshooting

### Common Issues

**Handler not found at runtime**
- Check that the handler is properly registered
- Verify the handler signature matches the request type
- Use the `/relay/handlers` endpoint to inspect registered handlers

**Analyzer warnings not showing**
- Ensure Relay.SourceGenerator package is referenced
- Check that the analyzer is enabled in your IDE
- Verify .editorconfig settings are correct

**Test failures with RelayTestBuilder**
- Ensure all dependencies are properly mocked
- Check that handler registrations match your expectations
- Use trace capture to debug execution flow

For more detailed troubleshooting information, see the [Troubleshooting Guide](troubleshooting.md).