# Relay Diagnostics Guide

This guide covers the comprehensive diagnostics capabilities built into Relay, helping you monitor, debug, and optimize your applications.

## Overview

Relay's diagnostics system provides:
- Real-time request tracing with AsyncLocal context
- Handler registry inspection and validation
- Performance metrics collection and analysis
- Runtime health checks and configuration validation
- RESTful diagnostic endpoints for external monitoring

## Getting Started

### Basic Setup

Enable diagnostics in your application startup:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRelay()
        .AddDiagnostics();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Diagnostic endpoints will be automatically registered
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        // Relay diagnostic endpoints are available at /relay/*
    });
}
```

### Advanced Configuration

Customize diagnostics behavior:

```csharp
services.AddRelay()
    .AddDiagnostics(options =>
    {
        options.EnableRequestTracing = true;
        options.EnablePerformanceMetrics = true;
        options.ExposeEndpoints = true;
        options.TraceBufferSize = 1000;
        options.MetricsRetentionPeriod = TimeSpan.FromHours(24);
    });
```

## Request Tracing

### Automatic Tracing

Relay automatically traces request execution when diagnostics are enabled:

```csharp
public class OrderHandler
{
    [Handle]
    public async ValueTask<OrderResponse> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Relay automatically traces:
        // - Handler entry/exit
        // - Pipeline execution
        // - Exception handling
        // - Performance metrics
        
        var order = new Order(request.CustomerId, request.Items);
        await _repository.SaveAsync(order, cancellationToken);
        
        return new OrderResponse { OrderId = order.Id };
    }
}
```

### Manual Trace Steps

Add custom trace steps for detailed debugging:

```csharp
public class OrderHandler
{
    private readonly IRequestTracer _tracer;
    private readonly IOrderRepository _repository;
    
    public OrderHandler(IRequestTracer tracer, IOrderRepository repository)
    {
        _tracer = tracer;
        _repository = repository;
    }
    
    [Handle]
    public async ValueTask<OrderResponse> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        _tracer.RecordStep("Validation started", new { 
            CustomerId = request.CustomerId,
            ItemCount = request.Items.Count 
        });
        
        ValidateRequest(request);
        
        _tracer.RecordStep("Validation completed");
        
        _tracer.RecordStep("Order creation started");
        var order = new Order(request.CustomerId, request.Items);
        
        _tracer.RecordStep("Saving to repository");
        await _repository.SaveAsync(order, cancellationToken);
        
        _tracer.RecordStep("Order processing completed", new { 
            OrderId = order.Id,
            TotalAmount = order.TotalAmount 
        });
        
        return new OrderResponse { OrderId = order.Id };
    }
}
```

### Trace Context

Access trace information from anywhere in your request pipeline:

```csharp
public class AuditPipeline : IPipelineBehavior
{
    private readonly IRequestTracer _tracer;
    private readonly IAuditService _auditService;
    
    public AuditPipeline(IRequestTracer tracer, IAuditService auditService)
    {
        _tracer = tracer;
        _auditService = auditService;
    }
    
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var traceId = _tracer.CurrentTrace?.Id;
        
        _tracer.RecordStep("Audit logging started", new { TraceId = traceId });
        
        try
        {
            var response = await next();
            
            await _auditService.LogSuccessAsync(request, response, traceId, cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            await _auditService.LogErrorAsync(request, ex, traceId, cancellationToken);
            throw;
        }
    }
}
```

## Handler Registry Inspection

### Programmatic Access

Inspect your handler registry at runtime:

```csharp
public class SystemInfoController : ControllerBase
{
    private readonly IRelayDiagnostics _diagnostics;
    
    public SystemInfoController(IRelayDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }
    
    [HttpGet("system/handlers")]
    public async Task<ActionResult<HandlerRegistryInfo>> GetHandlers()
    {
        var registry = await _diagnostics.GetHandlerRegistryAsync();
        
        return Ok(new
        {
            TotalHandlers = registry.Handlers.Count,
            HandlersByType = registry.Handlers.GroupBy(h => h.RequestType).ToDictionary(g => g.Key, g => g.Count()),
            Handlers = registry.Handlers.Select(h => new
            {
                h.RequestType,
                h.ResponseType,
                h.HandlerType,
                h.Name,
                h.IsAsync,
                PipelineCount = h.Pipelines.Count
            })
        });
    }
    
    [HttpGet("system/handlers/{requestType}")]
    public async Task<ActionResult<HandlerInfo>> GetHandler(string requestType)
    {
        var registry = await _diagnostics.GetHandlerRegistryAsync();
        var handler = registry.Handlers.FirstOrDefault(h => h.RequestType == requestType);
        
        if (handler == null)
            return NotFound($"No handler found for request type: {requestType}");
            
        return Ok(handler);
    }
}
```

### Configuration Validation

Validate your Relay configuration:

```csharp
public class HealthController : ControllerBase
{
    private readonly IRelayDiagnostics _diagnostics;
    
    public HealthController(IRelayDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }
    
    [HttpGet("health/relay")]
    public async Task<ActionResult<ValidationResult>> ValidateConfiguration()
    {
        var result = await _diagnostics.ValidateConfigurationAsync();
        
        if (!result.IsValid)
            return BadRequest(result);
            
        return Ok(result);
    }
}
```

## Performance Metrics

### Built-in Metrics

Relay automatically collects performance metrics:

```csharp
public class MetricsController : ControllerBase
{
    private readonly IRelayDiagnostics _diagnostics;
    
    public MetricsController(IRelayDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }
    
    [HttpGet("metrics/performance")]
    public async Task<ActionResult> GetPerformanceMetrics()
    {
        var metrics = await _diagnostics.GetPerformanceMetricsAsync();
        
        return Ok(new
        {
            TotalRequests = metrics.TotalRequests,
            AverageExecutionTime = metrics.AverageExecutionTime,
            RequestsPerSecond = metrics.RequestsPerSecond,
            ErrorRate = metrics.ErrorRate,
            HandlerMetrics = metrics.HandlerMetrics.Select(hm => new
            {
                hm.HandlerType,
                hm.RequestCount,
                hm.AverageExecutionTime,
                hm.ErrorCount,
                hm.LastExecuted
            })
        });
    }
}
```

### Custom Metrics

Add custom performance tracking:

```csharp
public class OrderHandler
{
    private readonly IRequestTracer _tracer;
    
    [Handle]
    public async ValueTask<OrderResponse> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        using var performanceScope = _tracer.StartPerformanceScope("order-processing");
        
        // Your handler logic here
        performanceScope.RecordMetric("items-processed", request.Items.Count);
        performanceScope.RecordMetric("customer-tier", request.CustomerTier);
        
        return new OrderResponse();
    }
}
```

## Diagnostic Endpoints

### Available Endpoints

When diagnostics are enabled, the following endpoints are automatically available:

#### GET /relay/handlers
Returns information about all registered handlers:

```json
{
  "totalHandlers": 15,
  "handlers": [
    {
      "requestType": "CreateOrderRequest",
      "responseType": "OrderResponse",
      "handlerType": "OrderHandler",
      "name": null,
      "isAsync": true,
      "pipelines": ["ValidationPipeline", "AuditPipeline"]
    }
  ]
}
```

#### GET /relay/metrics
Returns performance metrics:

```json
{
  "totalRequests": 1250,
  "averageExecutionTime": "00:00:00.0234567",
  "requestsPerSecond": 45.2,
  "errorRate": 0.02,
  "handlerMetrics": [
    {
      "handlerType": "OrderHandler",
      "requestCount": 450,
      "averageExecutionTime": "00:00:00.0156789",
      "errorCount": 2,
      "lastExecuted": "2023-12-01T10:30:00Z"
    }
  ]
}
```

#### GET /relay/health
Returns configuration validation results:

```json
{
  "isValid": true,
  "validationResults": [
    {
      "category": "HandlerRegistration",
      "isValid": true,
      "message": "All handlers properly registered"
    },
    {
      "category": "DependencyInjection",
      "isValid": true,
      "message": "All dependencies can be resolved"
    }
  ]
}
```

#### GET /relay/traces/{traceId}
Returns detailed trace information for a specific request:

```json
{
  "traceId": "abc123",
  "startTime": "2023-12-01T10:30:00Z",
  "endTime": "2023-12-01T10:30:00.156Z",
  "totalDuration": "00:00:00.0156789",
  "steps": [
    {
      "name": "Handler execution started",
      "timestamp": "2023-12-01T10:30:00Z",
      "duration": "00:00:00.0001234",
      "metadata": {
        "handlerType": "OrderHandler",
        "requestType": "CreateOrderRequest"
      }
    }
  ]
}
```

### Securing Diagnostic Endpoints

Protect diagnostic endpoints in production:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddRelay()
        .AddDiagnostics(options =>
        {
            options.ExposeEndpoints = !env.IsProduction();
            options.RequireAuthorization = true;
            options.AuthorizationPolicy = "DiagnosticsAccess";
        });
        
    services.AddAuthorization(options =>
    {
        options.AddPolicy("DiagnosticsAccess", policy =>
            policy.RequireRole("Administrator", "Developer"));
    });
}
```

## Integration with Monitoring Systems

### Application Insights

```csharp
public class ApplicationInsightsTelemetryPipeline : IPipelineBehavior
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IRequestTracer _tracer;
    
    public ApplicationInsightsTelemetryPipeline(TelemetryClient telemetryClient, IRequestTracer tracer)
    {
        _telemetryClient = telemetryClient;
        _tracer = tracer;
    }
    
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var traceId = _tracer.CurrentTrace?.Id;
        
        using var operation = _telemetryClient.StartOperation<RequestTelemetry>($"Relay: {typeof(TRequest).Name}");
        operation.Telemetry.Properties["TraceId"] = traceId;
        
        try
        {
            var response = await next();
            operation.Telemetry.Success = true;
            return response;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            _telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                ["TraceId"] = traceId,
                ["RequestType"] = typeof(TRequest).Name
            });
            throw;
        }
    }
}
```

### Prometheus Metrics

```csharp
public class PrometheusMetricsPipeline : IPipelineBehavior
{
    private static readonly Counter RequestCounter = Metrics
        .CreateCounter("relay_requests_total", "Total number of Relay requests", "handler", "status");
        
    private static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("relay_request_duration_seconds", "Duration of Relay requests", "handler");
    
    public async ValueTask<TResponse> Handle<TRequest, TResponse>(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var handlerName = typeof(TRequest).Name;
        
        using var timer = RequestDuration.WithLabels(handlerName).NewTimer();
        
        try
        {
            var response = await next();
            RequestCounter.WithLabels(handlerName, "success").Inc();
            return response;
        }
        catch
        {
            RequestCounter.WithLabels(handlerName, "error").Inc();
            throw;
        }
    }
}
```

## Best Practices

### Performance Considerations

1. **Buffer Size**: Configure appropriate trace buffer sizes for your workload
2. **Retention Period**: Set reasonable metrics retention periods to avoid memory issues
3. **Sampling**: Consider implementing sampling for high-volume applications
4. **Async Operations**: Use async methods for diagnostic operations to avoid blocking

### Security

1. **Disable in Production**: Consider disabling diagnostic endpoints in production
2. **Authorization**: Always require authorization for diagnostic endpoints
3. **Sensitive Data**: Avoid logging sensitive information in trace steps
4. **Rate Limiting**: Implement rate limiting on diagnostic endpoints

### Monitoring

1. **Health Checks**: Regularly monitor the `/relay/health` endpoint
2. **Performance Alerts**: Set up alerts based on performance metrics
3. **Error Tracking**: Monitor error rates and investigate spikes
4. **Capacity Planning**: Use metrics for capacity planning and scaling decisions

## Troubleshooting

### Common Issues

**Diagnostics not working**
- Verify `AddDiagnostics()` is called in service registration
- Check that diagnostic endpoints are enabled
- Ensure proper authorization if required

**Missing trace information**
- Confirm request tracing is enabled
- Check that `IRequestTracer` is properly injected
- Verify AsyncLocal context is preserved across async operations

**Performance impact**
- Review trace buffer size configuration
- Consider implementing sampling for high-volume scenarios
- Monitor memory usage and adjust retention periods

For more troubleshooting information, see the main [Troubleshooting Guide](troubleshooting.md).