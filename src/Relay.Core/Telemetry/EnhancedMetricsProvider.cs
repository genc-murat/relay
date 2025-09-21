using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.Telemetry;

/// <summary>
/// Enhanced metrics provider with advanced anomaly detection and configurable thresholds
/// </summary>
public class EnhancedMetricsProvider : DefaultMetricsProvider
{
    private readonly MetricsProviderOptions _options;

    public EnhancedMetricsProvider(IOptions<MetricsProviderOptions> options, ILogger<EnhancedMetricsProvider>? logger = null)
        : base(logger)
    {
        _options = options.Value;
    }

    public EnhancedMetricsProvider(MetricsProviderOptions options, ILogger<EnhancedMetricsProvider>? logger = null)
        : base(logger)
    {
        _options = options;
    }

    public override IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
    {
        // Use enhanced anomaly detection with configurable thresholds
        var anomalies = new List<PerformanceAnomaly>();
        var cutoffTime = DateTimeOffset.UtcNow - lookbackPeriod;

        // Get base anomalies from parent class
        var baseAnomalies = base.DetectAnomalies(lookbackPeriod).ToList();
        anomalies.AddRange(baseAnomalies);

        // Timeout exceeded detection for handler executions (recent window)
        var handlerGroups = GetHandlerExecutionsSnapshot(cutoffTime);
        foreach (var executions in handlerGroups)
        {
            // Always detect hard timeouts regardless of sample size
            foreach (var execution in executions.Skip(Math.Max(0, executions.Count - _options.RecentExecutionsForAnomalyCheck)))
            {
                if (execution.Duration > _options.TimeoutThreshold)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        OperationId = execution.OperationId,
                        RequestType = execution.RequestType,
                        HandlerName = execution.HandlerName,
                        Type = AnomalyType.TimeoutExceeded,
                        Description = $"Execution exceeded timeout threshold of {_options.TimeoutThreshold.TotalMilliseconds:F0}ms (actual {execution.Duration.TotalMilliseconds:F0}ms)",
                        ActualDuration = execution.Duration,
                        ExpectedDuration = _options.TimeoutThreshold,
                        Severity = Math.Max(1.0, execution.Duration.TotalMilliseconds / Math.Max(1.0, _options.TimeoutThreshold.TotalMilliseconds))
                    });
                }
            }

            if (executions.Count <= _options.MinSampleSizeForAnomalyDetection)
            {
                continue;
            }

            var recent = executions.Skip(Math.Max(0, executions.Count - _options.RecentExecutionsForAnomalyCheck));
            var orderedDurations = executions.Select(e => e.Duration).OrderBy(d => d).ToList();
            if (orderedDurations.Count > 1)
            {
                orderedDurations.RemoveAt(orderedDurations.Count - 1); // trim top outlier
            }
            var avg = TimeSpan.FromTicks((long)orderedDurations.Average(d => d.Ticks));
            foreach (var execution in recent)
            {
                // Slow execution based on configurable multiplier
                if (execution.Duration > TimeSpan.FromTicks((long)(avg.Ticks * _options.SlowExecutionThreshold)))
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        OperationId = execution.OperationId,
                        RequestType = execution.RequestType,
                        HandlerName = execution.HandlerName,
                        Type = AnomalyType.SlowExecution,
                        Description = $"Execution took {execution.Duration.TotalMilliseconds:F0}ms vs avg {avg.TotalMilliseconds:F0}ms",
                        ActualDuration = execution.Duration,
                        ExpectedDuration = avg,
                        Severity = Math.Max(1.0, execution.Duration.TotalMilliseconds / Math.Max(1.0, avg.TotalMilliseconds))
                    });
                }
            }

            // High failure rate over the window
            var total = executions.Count;
            var failures = executions.Count(e => !e.Success);
            var failureRate = total > 0 ? (double)failures / total : 0.0;
            if (total >= _options.MinSampleSizeForAnomalyDetection && failureRate >= _options.HighFailureRateThreshold)
            {
                var sample = executions[0];
                anomalies.Add(new PerformanceAnomaly
                {
                    RequestType = sample.RequestType,
                    HandlerName = sample.HandlerName,
                    Type = AnomalyType.HighFailureRate,
                    Description = $"Failure rate {failureRate * 100:F1}% exceeds threshold {_options.HighFailureRateThreshold * 100:F1}%",
                    Severity = failureRate
                });
            }
        }

        // Unusual item count detection for streaming operations
        var streamGroups = GetStreamingOperationsSnapshot(cutoffTime);
        foreach (var ops in streamGroups)
        {
            if (ops.Count <= _options.MinSampleSizeForAnomalyDetection)
            {
                continue;
            }

            var avg = ops.Average(o => (double)o.ItemCount);
            var threshold = Math.Max(avg * _options.SlowExecutionThreshold, avg * 2); // use slow threshold as a multiplier baseline
            var recent = ops.Skip(Math.Max(0, ops.Count - _options.RecentExecutionsForAnomalyCheck));
            foreach (var op in recent)
            {
                if (op.ItemCount > threshold)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        OperationId = op.OperationId,
                        RequestType = op.RequestType,
                        HandlerName = op.HandlerName,
                        Type = AnomalyType.UnusualItemCount,
                        Description = $"Streaming item count {op.ItemCount} is unusually high vs avg {avg:F0}",
                        ActualDuration = op.Duration,
                        ExpectedDuration = TimeSpan.FromMilliseconds(avg),
                        Severity = Math.Max(1.0, op.ItemCount / Math.Max(1.0, avg))
                    });
                }
            }
        }

        return anomalies.OrderByDescending(a => a.Severity);
    }

    // Override caps to honor options
    protected override int MaxRecordsPerHandler => _options.MaxRecordsPerHandler;
    protected override int MaxTimingBreakdowns => _options.MaxTimingBreakdowns;
}

/// <summary>
/// Configuration options for the enhanced metrics provider
/// </summary>
public class MetricsProviderOptions
{
    /// <summary>
    /// Threshold for detecting slow executions (multiplier of average)
    /// </summary>
    public double SlowExecutionThreshold { get; set; } = 2.0;

    /// <summary>
    /// Threshold for detecting high failure rates (percentage)
    /// </summary>
    public double HighFailureRateThreshold { get; set; } = 0.1;

    /// <summary>
    /// Time window for anomaly detection
    /// </summary>
    public TimeSpan AnomalyDetectionWindow { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum number of metrics to keep in memory per handler
    /// </summary>
    public int MaxMetricsPerHandler { get; set; } = 1000;

    /// <summary>
    /// Enable advanced anomaly detection features
    /// </summary>
    public bool EnableAdvancedAnomalyDetection { get; set; } = true;

    /// <summary>
    /// Minimum number of executions required for anomaly detection
    /// </summary>
    public int MinExecutionsForAnomalyDetection { get; set; } = 10;

    /// <summary>
    /// Timeout threshold for detecting timeout exceeded anomalies
    /// </summary>
    public TimeSpan TimeoutThreshold { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Minimum sample size for anomaly detection
    /// </summary>
    public int MinSampleSizeForAnomalyDetection { get; set; } = 10;

    /// <summary>
    /// Number of recent executions to check for anomalies
    /// </summary>
    public int RecentExecutionsForAnomalyCheck { get; set; } = 5;

    /// <summary>
    /// Enable real-time anomaly detection
    /// </summary>
    public bool EnableRealTimeAnomalyDetection { get; set; } = true;

    /// <summary>
    /// Maximum number of records per handler
    /// </summary>
    public int MaxRecordsPerHandler { get; set; } = 1000;

    /// <summary>
    /// Maximum number of timing breakdowns to keep
    /// </summary>
    public int MaxTimingBreakdowns { get; set; } = 10000;
}