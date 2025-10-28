using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class ServerSentEventEstimationStrategy(
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
            // Server-Sent Events are typically used as WebSocket fallbacks
            // Estimate based on long-polling patterns and browser compatibility

            var connectionCount = 0;

            // Strategy 1: Check stored SSE metrics
            connectionCount = TryGetStoredSSEMetrics();
            if (connectionCount > 0)
                return connectionCount;

            // Strategy 2: Estimate from request patterns
            connectionCount = EstimateSSEFromRequestPatterns();

            // Strategy 3: Fallback to percentage of total connections
            if (connectionCount == 0)
            {
                connectionCount = EstimateSSEAsFallback();
            }

            _logger.LogTrace("SSE connections estimated: {Count}", connectionCount);
            return connectionCount;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating SSE connections");
            return 0;
        }
    }

    private int TryGetStoredSSEMetrics()
    {
        try
        {
            var recentMetrics = _timeSeriesDb.GetRecentMetrics("sse_connections", 10);
            if (recentMetrics.Count > 0)
            {
                // Use simple average of recent values
                return (int)recentMetrics.Average(m => m.Value);
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateSSEFromRequestPatterns()
    {
        try
        {
            // SSE connections are typically very long-lived (>1 minute average)
            // and have specific concurrent execution patterns
            var sseRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalMinutes > 1) // Very long-lived connections
                .Sum(a => a.ConcurrentExecutionPeaks);

            // SSE typically represents 10-20% of long-polling connections
            var estimated = (int)(sseRequests * 0.15);
            
            // Return at least 1 if there are any matching SSE-like requests
            return sseRequests > 0 ? Math.Max(1, estimated) : 0;
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateSSEAsFallback()
    {
        try
        {
            // SSE is used when WebSockets aren't available
            // Estimate as 5-10% of active requests during peak times
            var activeRequests = _systemMetrics.GetActiveRequestCount();
            var hourOfDay = DateTime.UtcNow.Hour;

            // More SSE usage during business hours when compatibility issues might arise
            var multiplier = (hourOfDay >= 9 && hourOfDay <= 17) ? 0.08 : 0.03;

            return (int)(activeRequests * multiplier);
        }
        catch
        {
            return 0;
        }
    }
}
