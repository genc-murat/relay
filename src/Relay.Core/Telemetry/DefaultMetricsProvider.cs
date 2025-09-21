using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Telemetry;

/// <summary>
/// Default implementation of IMetricsProvider with in-memory storage and anomaly detection
/// </summary>
public class DefaultMetricsProvider : IMetricsProvider
{
    private readonly ConcurrentDictionary<string, List<HandlerExecutionMetrics>> _handlerExecutions = new();
    private readonly ConcurrentDictionary<string, List<NotificationPublishMetrics>> _notificationPublishes = new();
    private readonly ConcurrentDictionary<string, List<StreamingOperationMetrics>> _streamingOperations = new();
    private readonly ConcurrentDictionary<string, TimingBreakdown> _timingBreakdowns = new();
    
    private readonly ILogger<DefaultMetricsProvider>? _logger;
    
    // Configuration for anomaly detection
    private readonly TimeSpan _anomalyDetectionWindow = TimeSpan.FromMinutes(15);
    private readonly double _slowExecutionThreshold = 2.0; // 2x average
    private readonly double _highFailureRateThreshold = 0.1; // 10% failure rate

    // Overridable caps for memory usage
    protected virtual int MaxRecordsPerHandler => 1000;
    protected virtual int MaxTimingBreakdowns => 10000;
    
    public DefaultMetricsProvider(ILogger<DefaultMetricsProvider>? logger = null)
    {
        _logger = logger;
    }
    
    public void RecordHandlerExecution(HandlerExecutionMetrics metrics)
    {
        var key = GetHandlerKey(metrics.RequestType, metrics.HandlerName);
        _handlerExecutions.AddOrUpdate(key, 
            new List<HandlerExecutionMetrics> { metrics },
            (_, existing) => 
            {
                lock (existing)
                {
                    existing.Add(metrics);
                    // Keep only recent entries to prevent memory growth
                    if (existing.Count > MaxRecordsPerHandler)
                    {
                        existing.RemoveRange(0, existing.Count - MaxRecordsPerHandler);
                    }
                    return existing;
                }
            });
        
        _logger?.LogDebug("Recorded handler execution: {RequestType} -> {ResponseType} in {Duration}ms (Success: {Success})", 
            metrics.RequestType.Name, metrics.ResponseType?.Name, metrics.Duration.TotalMilliseconds, metrics.Success);
    }
    
    public void RecordNotificationPublish(NotificationPublishMetrics metrics)
    {
        var key = metrics.NotificationType.FullName ?? metrics.NotificationType.Name;
        _notificationPublishes.AddOrUpdate(key,
            new List<NotificationPublishMetrics> { metrics },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(metrics);
                    if (existing.Count > MaxRecordsPerHandler)
                    {
                        existing.RemoveRange(0, existing.Count - MaxRecordsPerHandler);
                    }
                    return existing;
                }
            });
        
        _logger?.LogDebug("Recorded notification publish: {NotificationType} to {HandlerCount} handlers in {Duration}ms (Success: {Success})", 
            metrics.NotificationType.Name, metrics.HandlerCount, metrics.Duration.TotalMilliseconds, metrics.Success);
    }
    
    public void RecordStreamingOperation(StreamingOperationMetrics metrics)
    {
        var key = GetHandlerKey(metrics.RequestType, metrics.HandlerName);
        _streamingOperations.AddOrUpdate(key,
            new List<StreamingOperationMetrics> { metrics },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(metrics);
                    if (existing.Count > MaxRecordsPerHandler)
                    {
                        existing.RemoveRange(0, existing.Count - MaxRecordsPerHandler);
                    }
                    return existing;
                }
            });
        
        _logger?.LogDebug("Recorded streaming operation: {RequestType} -> {ResponseType} ({ItemCount} items) in {Duration}ms (Success: {Success})", 
            metrics.RequestType.Name, metrics.ResponseType.Name, metrics.ItemCount, metrics.Duration.TotalMilliseconds, metrics.Success);
    }
    
    public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName = null)
    {
        var key = GetHandlerKey(requestType, handlerName);
        if (!_handlerExecutions.TryGetValue(key, out var executions))
        {
            return new HandlerExecutionStats
            {
                RequestType = requestType,
                HandlerName = handlerName
            };
        }
        
        List<HandlerExecutionMetrics> executionsCopy;
        lock (executions)
        {
            executionsCopy = executions.ToList();
        }
        
        if (executionsCopy.Count == 0)
        {
            return new HandlerExecutionStats
            {
                RequestType = requestType,
                HandlerName = handlerName
            };
        }
        
        var successful = executionsCopy.Where(e => e.Success).ToList();
        var failed = executionsCopy.Where(e => !e.Success).ToList();
        var durations = executionsCopy.Select(e => e.Duration).OrderBy(d => d).ToList();
        
        return new HandlerExecutionStats
        {
            RequestType = requestType,
            HandlerName = handlerName,
            TotalExecutions = executionsCopy.Count,
            SuccessfulExecutions = successful.Count,
            FailedExecutions = failed.Count,
            AverageExecutionTime = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MinExecutionTime = durations.First(),
            MaxExecutionTime = durations.Last(),
            P50ExecutionTime = GetPercentile(durations, 0.5),
            P95ExecutionTime = GetPercentile(durations, 0.95),
            P99ExecutionTime = GetPercentile(durations, 0.99),
            LastExecution = executionsCopy.Max(e => e.Timestamp)
        };
    }
    
    public NotificationPublishStats GetNotificationPublishStats(Type notificationType)
    {
        var key = notificationType.FullName ?? notificationType.Name;
        if (!_notificationPublishes.TryGetValue(key, out var publishes))
        {
            return new NotificationPublishStats
            {
                NotificationType = notificationType
            };
        }
        
        List<NotificationPublishMetrics> publishesCopy;
        lock (publishes)
        {
            publishesCopy = publishes.ToList();
        }
        
        if (publishesCopy.Count == 0)
        {
            return new NotificationPublishStats
            {
                NotificationType = notificationType
            };
        }
        
        var successful = publishesCopy.Where(p => p.Success).ToList();
        var failed = publishesCopy.Where(p => !p.Success).ToList();
        var durations = publishesCopy.Select(p => p.Duration).OrderBy(d => d).ToList();
        
        return new NotificationPublishStats
        {
            NotificationType = notificationType,
            TotalPublishes = publishesCopy.Count,
            SuccessfulPublishes = successful.Count,
            FailedPublishes = failed.Count,
            AveragePublishTime = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)),
            MinPublishTime = durations.First(),
            MaxPublishTime = durations.Last(),
            AverageHandlerCount = publishesCopy.Average(p => p.HandlerCount),
            LastPublish = publishesCopy.Max(p => p.Timestamp)
        };
    }
    
    public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName = null)
    {
        var key = GetHandlerKey(requestType, handlerName);
        if (!_streamingOperations.TryGetValue(key, out var operations))
        {
            return new StreamingOperationStats
            {
                RequestType = requestType,
                HandlerName = handlerName
            };
        }
        
        List<StreamingOperationMetrics> operationsCopy;
        lock (operations)
        {
            operationsCopy = operations.ToList();
        }
        
        if (operationsCopy.Count == 0)
        {
            return new StreamingOperationStats
            {
                RequestType = requestType,
                HandlerName = handlerName
            };
        }
        
        var successful = operationsCopy.Where(o => o.Success).ToList();
        var failed = operationsCopy.Where(o => !o.Success).ToList();
        var totalItems = operationsCopy.Sum(o => o.ItemCount);
        var totalDuration = TimeSpan.FromTicks(operationsCopy.Sum(o => o.Duration.Ticks));
        
        return new StreamingOperationStats
        {
            RequestType = requestType,
            HandlerName = handlerName,
            TotalOperations = operationsCopy.Count,
            SuccessfulOperations = successful.Count,
            FailedOperations = failed.Count,
            AverageOperationTime = TimeSpan.FromTicks((long)operationsCopy.Average(o => o.Duration.Ticks)),
            TotalItemsStreamed = totalItems,
            AverageItemsPerOperation = operationsCopy.Average(o => o.ItemCount),
            ItemsPerSecond = totalDuration.TotalSeconds > 0 ? totalItems / totalDuration.TotalSeconds : 0,
            LastOperation = operationsCopy.Max(o => o.Timestamp)
        };
    }
    
    public virtual IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan lookbackPeriod)
    {
        var anomalies = new List<PerformanceAnomaly>();
        var cutoffTime = DateTimeOffset.UtcNow - lookbackPeriod;
        
        // Detect handler execution anomalies
        foreach (var kvp in _handlerExecutions)
        {
            List<HandlerExecutionMetrics> executions;
            lock (kvp.Value)
            {
                executions = kvp.Value.Where(e => e.Timestamp >= cutoffTime).ToList();
            }
            
            if (executions.Count < 10) continue; // Need sufficient data
            
            var stats = GetHandlerExecutionStats(executions.First().RequestType, executions.First().HandlerName);
            
            // Check for slow executions
            var recentExecutions = executions.Skip(Math.Max(0, executions.Count - 5)).ToList();
            foreach (var execution in recentExecutions)
            {
                if (execution.Duration.TotalMilliseconds > stats.AverageExecutionTime.TotalMilliseconds * _slowExecutionThreshold)
                {
                    anomalies.Add(new PerformanceAnomaly
                    {
                        OperationId = execution.OperationId,
                        RequestType = execution.RequestType,
                        HandlerName = execution.HandlerName,
                        Type = AnomalyType.SlowExecution,
                        Description = $"Handler execution took {execution.Duration.TotalMilliseconds:F2}ms, which is {execution.Duration.TotalMilliseconds / stats.AverageExecutionTime.TotalMilliseconds:F2}x the average",
                        ActualDuration = execution.Duration,
                        ExpectedDuration = stats.AverageExecutionTime,
                        Severity = execution.Duration.TotalMilliseconds / stats.AverageExecutionTime.TotalMilliseconds
                    });
                }
            }
            
            // Check for high failure rate
            if (stats.SuccessRate < (1.0 - _highFailureRateThreshold) && stats.TotalExecutions >= 10)
            {
                anomalies.Add(new PerformanceAnomaly
                {
                    RequestType = executions.First().RequestType,
                    HandlerName = executions.First().HandlerName,
                    Type = AnomalyType.HighFailureRate,
                    Description = $"Handler has a failure rate of {(1.0 - stats.SuccessRate) * 100:F1}%",
                    Severity = 1.0 - stats.SuccessRate
                });
            }
        }
        
        return anomalies.OrderByDescending(a => a.Severity);
    }
    
    public TimingBreakdown GetTimingBreakdown(string operationId)
    {
        return _timingBreakdowns.TryGetValue(operationId, out var breakdown) 
            ? breakdown 
            : new TimingBreakdown { OperationId = operationId };
    }
    
    public void RecordTimingBreakdown(TimingBreakdown breakdown)
    {
        _timingBreakdowns.TryAdd(breakdown.OperationId, breakdown);
        
        // Clean up old breakdowns to prevent memory growth
        if (_timingBreakdowns.Count > MaxTimingBreakdowns)
        {
            var toRemove = _timingBreakdowns.Count - MaxTimingBreakdowns;
            var oldestKeys = _timingBreakdowns.Keys.Take(toRemove);
            foreach (var key in oldestKeys)
            {
                _timingBreakdowns.TryRemove(key, out _);
            }
        }
    }
    
    private static string GetHandlerKey(Type requestType, string? handlerName)
    {
        var typeName = requestType.FullName ?? requestType.Name;
        return handlerName != null ? $"{typeName}:{handlerName}" : typeName;
    }
    
    private static TimeSpan GetPercentile(List<TimeSpan> sortedDurations, double percentile)
    {
        if (sortedDurations.Count == 0) return TimeSpan.Zero;
        if (sortedDurations.Count == 1) return sortedDurations[0];
        
        var index = (int)Math.Ceiling(sortedDurations.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedDurations.Count - 1));
        return sortedDurations[index];
    }

    // Protected snapshot helpers for derived providers
    protected IEnumerable<List<HandlerExecutionMetrics>> GetHandlerExecutionsSnapshot(DateTimeOffset cutoff)
    {
        foreach (var kvp in _handlerExecutions)
        {
            List<HandlerExecutionMetrics> copy;
            lock (kvp.Value)
            {
                copy = kvp.Value.Where(e => e.Timestamp >= cutoff).ToList();
            }
            if (copy.Count > 0)
            {
                yield return copy;
            }
        }
    }

    protected IEnumerable<List<StreamingOperationMetrics>> GetStreamingOperationsSnapshot(DateTimeOffset cutoff)
    {
        foreach (var kvp in _streamingOperations)
        {
            List<StreamingOperationMetrics> copy;
            lock (kvp.Value)
            {
                copy = kvp.Value.Where(e => e.Timestamp >= cutoff).ToList();
            }
            if (copy.Count > 0)
            {
                yield return copy;
            }
        }
    }
}