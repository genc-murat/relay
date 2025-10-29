# Contract Validation Observability

This directory contains observability components for the contract validation system, including structured logging, metrics, OpenTelemetry tracing, and health checks.

## Components

### ContractValidationActivitySource

Provides OpenTelemetry ActivitySource for distributed tracing of validation operations.

**Activities Created:**
- `ContractValidationPipeline.Handle` - Overall pipeline execution
- `ContractValidator.ValidateRequest` - Request validation
- `ContractValidator.ValidateResponse` - Response validation
- `SchemaResolver.Resolve` - Schema resolution
- `ValidationEngine.Validate` - Custom validator execution

**Tags:**
- `request_type` - The type of request being validated
- `response_type` - The type of response being validated
- `is_valid` - Whether validation passed
- `error_count` - Number of validation errors
- `duration_ms` - Duration in milliseconds
- `cache_hit` - Whether schema was found in cache
- `provider_type` - The provider that resolved the schema

### ContractValidationMetrics

Provides OpenTelemetry metrics for monitoring validation performance and health.

**Metrics:**
- `contract_validation_total` (Counter) - Total number of validations performed
- `contract_validation_errors_total` (Counter) - Total number of validation errors
- `contract_validation_duration_ms` (Histogram) - Validation duration in milliseconds
- `schema_resolution_total` (Counter) - Total schema resolution attempts
- `schema_resolution_duration_ms` (Histogram) - Schema resolution duration
- `custom_validator_executions_total` (Counter) - Custom validator executions
- `contract_validation_active` (Gauge) - Currently active validations

**Dimensions:**
- `request_type` - The type being validated
- `validation_target` - "request" or "response"
- `is_valid` - Validation result
- `provider_type` - Schema provider used
- `success` - Operation success status

### ValidationEventIds

Defines structured logging event IDs for consistent log filtering and monitoring.

**Event Categories:**
- 1000-1999: Validation events
- 2000-2199: Schema resolution events
- 2100-2199: Schema cache events
- 2200-2299: Schema discovery events
- 3000-3999: Custom validator events
- 4000-4999: Timeout events
- 5000-5999: Performance warnings
- 6000-6999: Configuration errors

### ContractValidationHealthCheck

Provides health check endpoint for monitoring validation system health.

**Health Indicators:**
- Schema cache size and utilization
- Cache hit rate
- Error counts by type
- System availability

**Health Status:**
- Healthy: System operating normally
- Degraded: Cache nearly full or low hit rate
- Unhealthy: System errors or failures

## Usage

### Registering Observability Components

```csharp
// Add metrics
services.AddContractValidationMetrics();

// Add health checks
services.AddHealthChecks()
    .AddContractValidationHealthCheck(
        name: "contract_validation",
        tags: new[] { "ready", "validation" });

// Configure OpenTelemetry
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource(ContractValidationActivitySource.SourceName))
    .WithMetrics(builder => builder
        .AddMeter("Relay.Core.ContractValidation"));
```

### Structured Logging

All validation components emit structured logs with consistent event IDs:

```csharp
// Example log output
[Information] ValidationEventIds.ValidationCompleted (1001): 
  Request validation completed for GetUserQuery. 
  IsValid: true, ErrorCount: 0, Duration: 15ms
```

### Querying Metrics

```promql
# Average validation duration
rate(contract_validation_duration_ms_sum[5m]) / 
rate(contract_validation_duration_ms_count[5m])

# Validation error rate
rate(contract_validation_errors_total[5m])

# Schema cache hit rate
rate(schema_cache_hits[5m]) / 
(rate(schema_cache_hits[5m]) + rate(schema_cache_misses[5m]))

# Active validations
contract_validation_active
```

### Distributed Tracing

View validation traces in your OpenTelemetry backend (Jaeger, Zipkin, etc.):

```
ContractValidationPipeline.Handle (50ms)
├─ SchemaResolver.Resolve (5ms)
│  └─ FileSystemSchemaProvider.TryGetSchema (4ms)
├─ ContractValidator.ValidateRequest (20ms)
├─ Handler Execution (15ms)
└─ ContractValidator.ValidateResponse (10ms)
```

### Health Check Endpoint

```bash
# Check validation system health
curl http://localhost:5000/health

# Response
{
  "status": "Healthy",
  "results": {
    "contract_validation": {
      "status": "Healthy",
      "description": "Contract validation system is healthy",
      "data": {
        "cache_size": 150,
        "cache_max_size": 1000,
        "cache_hit_rate": 0.95,
        "cache_total_requests": 1000,
        "cache_evictions": 5,
        "total_error_types": 3,
        "top_errors": "CV004:10, CV005:5, CV006:2"
      }
    }
  }
}
```

## Performance Monitoring

### Performance Thresholds

The system automatically logs warnings when performance thresholds are exceeded:

- Validation duration > 100ms: Performance warning logged
- Schema resolution > 50ms: Diagnostic log emitted
- Cache hit rate < 50%: Health check degraded

### Performance Metrics Dashboard

Recommended metrics to monitor:

1. **Validation Throughput**: `rate(contract_validation_total[1m])`
2. **Error Rate**: `rate(contract_validation_errors_total[1m])`
3. **P95 Latency**: `histogram_quantile(0.95, contract_validation_duration_ms)`
4. **Cache Effectiveness**: Cache hit rate percentage
5. **Active Validations**: Current concurrent validations

## Troubleshooting

### High Validation Latency

Check logs for `PerformanceWarning` events:
```
[Warning] ValidationEventIds.PerformanceWarning (5000): 
  Request validation took 150ms for GetUserQuery, 
  which exceeds the 100ms threshold
```

### Low Cache Hit Rate

Check health endpoint for cache metrics:
- Increase cache size if frequently evicting
- Review schema naming conventions
- Check for schema versioning issues

### Validation Timeouts

Check logs for `ValidationTimeout` events:
```
[Error] ValidationEventIds.ValidationTimeout (4000): 
  Request validation timed out after 5s for GetUserQuery
```

Consider:
- Increasing validation timeout
- Simplifying complex schemas
- Optimizing custom validators

## Best Practices

1. **Enable Metrics in Production**: Always enable metrics for production monitoring
2. **Configure Appropriate Log Levels**: Use Information level for production, Debug for development
3. **Monitor Health Checks**: Integrate with your monitoring system (Prometheus, Datadog, etc.)
4. **Set Up Alerts**: Alert on high error rates, timeouts, and degraded health
5. **Use Distributed Tracing**: Enable OpenTelemetry for end-to-end request tracing
6. **Review Performance Metrics**: Regularly review P95/P99 latencies and optimize as needed
