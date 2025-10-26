using Microsoft.Extensions.Logging;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI;

/// <summary>
/// Calculates system metrics for AI optimization decisions.
/// </summary>
public class SystemMetricsCalculator
{
    private readonly ILogger<SystemMetricsCalculator> _logger;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

    public SystemMetricsCalculator(
        ILogger<SystemMetricsCalculator> logger,
        ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
    }

    public async ValueTask<double> CalculateCpuUsageAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        
        var random = Random.Shared.NextDouble();
        var baseUsage = Environment.ProcessorCount > 4 ? 0.2 : 0.3;
        return Math.Min(1.0, baseUsage + (random * 0.4));
    }

    public double CalculateMemoryUsage()
    {
        var currentMemory = GC.GetTotalMemory(false);
        var maxMemory = Math.Max(currentMemory * 2, 512L * 1024 * 1024);
        return Math.Min(1.0, (double)currentMemory / maxMemory);
    }

    public int GetActiveRequestCount()
    {
        return _requestAnalytics.Values.Sum(x => x.ConcurrentExecutionPeaks) / Math.Max(1, _requestAnalytics.Count);
    }

    public int GetQueuedRequestCount()
    {
        return Math.Max(0, GetActiveRequestCount() - (Environment.ProcessorCount * 2));
    }

    public virtual double CalculateCurrentThroughput()
    {
        var totalExecutions = _requestAnalytics.Values.Sum(x => x.TotalExecutions);
        var timeSpan = TimeSpan.FromMinutes(5);
        return totalExecutions / timeSpan.TotalSeconds;
    }

    public TimeSpan CalculateAverageResponseTime()
    {
        if (_requestAnalytics.Count == 0) return TimeSpan.FromMilliseconds(100);
        
        var avgMs = _requestAnalytics.Values.Average(x => x.AverageExecutionTime.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(avgMs);
    }

    public double CalculateCurrentErrorRate()
    {
        if (_requestAnalytics.Count == 0) return 0.0;
        
        return _requestAnalytics.Values.Average(x => x.ErrorRate);
    }

    public double GetDatabasePoolUtilization()
    {
        return 0.0;
    }

    public double GetThreadPoolUtilization()
    {
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out _);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out _);
        return 1.0 - ((double)availableWorkerThreads / maxWorkerThreads);
    }
}
