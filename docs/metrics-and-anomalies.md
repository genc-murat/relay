# Metrics & Anomaly Detection

This document explains how Relay collects metrics, detects performance anomalies, and how you can configure memory limits and thresholds.

## Providers

- Default provider: `Relay.Core.Telemetry.DefaultMetricsProvider`
  - In-memory storage for:
    - Handler executions
    - Notification publishes
    - Streaming operations
    - Timing breakdowns
  - Basic anomalies:
    - Slow execution (relative to average)
    - High failure rate
  - Overridable caps (defaults):
    - `MaxRecordsPerHandler = 1000`
    - `MaxTimingBreakdowns = 10000`

- Enhanced provider: `Relay.Core.Telemetry.EnhancedMetricsProvider`
  - Extends default anomalies and adds:
    - `TimeoutExceeded`: when execution duration exceeds a hard `TimeoutThreshold`
    - `UnusualItemCount`: flags streaming operations with item counts well above the recent average
  - Uses robust average (trims top outlier) when evaluating slow executions
  - Honors configurable caps and thresholds via `MetricsProviderOptions`

## Configuration: MetricsProviderOptions

```csharp
public class MetricsProviderOptions
{
    public double SlowExecutionThreshold { get; set; } = 2.0;              // x times average
    public double HighFailureRateThreshold { get; set; } = 0.10;           // fraction (0.10 = 10%)
    public TimeSpan AnomalyDetectionWindow { get; set; } = TimeSpan.FromMinutes(15);
    public int MinSampleSizeForAnomalyDetection { get; set; } = 10;        // baseline required, except hard timeouts
    public int RecentExecutionsForAnomalyCheck { get; set; } = 5;          // sliding window for checks
    public TimeSpan TimeoutThreshold { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRecordsPerHandler { get; set; } = 1000;                  // memory cap
    public int MaxTimingBreakdowns { get; set; } = 10000;                  // memory cap
    public bool EnableAdvancedAnomalyDetection { get; set; } = true;       // reserved for future features
    public bool EnableRealTimeAnomalyDetection { get; set; } = true;       // reserved for future features
}
```

- Hard timeouts are always checked against `TimeoutThreshold`, even when fewer than `MinSampleSizeForAnomalyDetection` samples exist.
- Slow/Failure/UnusualItemCount require more than `MinSampleSizeForAnomalyDetection` samples.

## Anomaly Types

- `SlowExecution`: execution is slower than `SlowExecutionThreshold × average` in the recent window.
- `HighFailureRate`: failure rate across the window exceeds `HighFailureRateThreshold`.
- `TimeoutExceeded`: duration exceeds `TimeoutThreshold`.
- `UnusualItemCount` (streaming): recent item count is a large multiple of the average.

Each anomaly includes:

- `OperationId`, `RequestType`, `HandlerName`
- `ActualDuration`, `ExpectedDuration` (if applicable)
- `Severity` (higher is worse)

## Memory Limits

- `DefaultMetricsProvider` caps are virtual and enforced on insert to avoid unbounded memory growth.
- `EnhancedMetricsProvider` overrides these to values from `MetricsProviderOptions`.

## Timing Breakdowns

- Use `IMetricsProvider.RecordTimingBreakdown(TimingBreakdown)` to store detailed per-operation timings and metadata.
- Retrieval via `GetTimingBreakdown(operationId)`.

## Endpoint Metadata Registry (tests)

- `EndpointMetadataRegistry` is now test-safe: `Clear()` initializes a fresh per-test scope using `AsyncLocal` to avoid cross-test contamination.
- Production scenarios typically register endpoints at startup; test code should call `Clear()` before registering per-test metadata.

## Usage Examples

Register EnhancedMetricsProvider with options:

```csharp
services.Configure<MetricsProviderOptions>(o =>
{
    o.TimeoutThreshold = TimeSpan.FromSeconds(5);
    o.SlowExecutionThreshold = 2.0;
    o.HighFailureRateThreshold = 0.15;
    o.MinSampleSizeForAnomalyDetection = 5;
    o.RecentExecutionsForAnomalyCheck = 3;
    o.MaxRecordsPerHandler = 500;
    o.MaxTimingBreakdowns = 2000;
});
services.AddSingleton<IMetricsProvider, EnhancedMetricsProvider>();
```

Detect anomalies over a lookback period:

```csharp
var anomalies = metricsProvider.DetectAnomalies(TimeSpan.FromHours(1));
foreach (var a in anomalies)
{
    logger.LogWarning("{Type} on {Handler}: {Description} (severity {Severity:F2})",
        a.Type, a.HandlerName, a.Description, a.Severity);
}
```
