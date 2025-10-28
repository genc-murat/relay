using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class LongPollingEstimationStrategy(
    ILogger logger,
    Relay.Core.AI.AIOptimizationOptions options,
    ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
    TimeSeriesDatabase timeSeriesDb,
    Relay.Core.AI.SystemMetricsCalculator systemMetrics,
    ConnectionMetricsUtilities utilities) : IConnectionEstimationStrategy
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Relay.Core.AI.AIOptimizationOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
    private readonly TimeSeriesDatabase _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
    private readonly Relay.Core.AI.SystemMetricsCalculator _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
    private readonly ConnectionMetricsUtilities _utilities = utilities ?? throw new ArgumentNullException(nameof(utilities));

    public int EstimateConnections()
    {
        try
        {
            var connectionCount = 0;

            // Strategy 1: Check stored long-polling metrics
            connectionCount = TryGetStoredLongPollingMetrics();
            if (connectionCount > 0)
            {
                var maxLimit = _options.MaxEstimatedWebSocketConnections / 6;
                connectionCount = Math.Min(connectionCount, maxLimit);
                return connectionCount;
            }

            // Strategy 2: Estimate from request patterns
            connectionCount = EstimateLongPollingFromRequestPatterns();

            // Strategy 3: Fallback estimation
            if (connectionCount == 0)
            {
                connectionCount = EstimateLongPollingAsFallback();
            }

            _logger.LogTrace("Long-polling connections estimated: {Count}", connectionCount);
            return connectionCount;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating long-polling connections");
            return 0;
        }
    }

    private int TryGetStoredLongPollingMetrics()
    {
        try
        {
            var metricNames = new[] { "longpolling_connections", "long_polling_connections", "polling_connections" };

            foreach (var metricName in metricNames)
            {
                var recentMetrics = _timeSeriesDb.GetRecentMetrics(metricName, 10);
                if (recentMetrics.Count > 0)
                {
                    return (int)recentMetrics.Average(m => m.Value);
                }
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateLongPollingFromRequestPatterns()
    {
        try
        {
            // Long-polling connections are characterized by:
            // - Moderate execution time (seconds to minutes)
            // - High concurrent peaks
            // - Specific polling patterns

            var pollingRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalSeconds > 30 && // Longer than typical requests
                           a.AverageExecutionTime.TotalMinutes < 5 &&   // But not extremely long
                           a.ConcurrentExecutionPeaks > 0)
                .Sum(a => a.ConcurrentExecutionPeaks);

            // Long-polling typically represents 40-60% of such requests
            return (int)(pollingRequests * 0.5);
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateLongPollingAsFallback()
    {
        try
        {
            // Long-polling is used as WebSocket fallback
            // Estimate based on active requests and system load
            var activeRequests = _systemMetrics.GetActiveRequestCount();
            var throughput = _systemMetrics.CalculateCurrentThroughput();

            // Higher throughput suggests more real-time needs, potentially more long-polling
            var baseEstimate = Math.Max(1, activeRequests / 10); // 10% baseline

            // Adjust based on throughput
            var throughputMultiplier = Math.Min(2.0, throughput / 50.0); // Cap at 2x
            var estimate = (int)(baseEstimate * throughputMultiplier);

            return Math.Min(estimate, _options.MaxEstimatedWebSocketConnections / 6);
        }
        catch
        {
            return 0;
        }
    }
}
