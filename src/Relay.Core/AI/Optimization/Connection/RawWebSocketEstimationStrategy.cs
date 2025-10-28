using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class RawWebSocketEstimationStrategy(
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

            // Strategy 1: Try to get stored WebSocket metrics
            connectionCount = TryGetStoredWebSocketMetrics();
            if (connectionCount > 0)
            {
                var maxLimit = _options.MaxEstimatedWebSocketConnections / 4;
                connectionCount = Math.Min(connectionCount, maxLimit);
                _logger.LogTrace("Raw WebSocket connections from stored metrics: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 2: Estimate from request patterns and upgrade frequency
            connectionCount = EstimateWebSocketFromUpgradePatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Raw WebSocket connections from upgrade patterns: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 3: Historical pattern-based estimation
            connectionCount = EstimateWebSocketFromHistoricalPatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Raw WebSocket connections from historical patterns: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 4: Fallback estimation from active requests
            connectionCount = EstimateWebSocketFromActiveRequests();

            _logger.LogDebug("Raw WebSocket connections estimated: {Count}", connectionCount);
            return connectionCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error estimating raw WebSocket connections");
            return 0;
        }
    }

    private int TryGetStoredWebSocketMetrics()
    {
        try
        {
            var metricNames = new[]
            {
                "WebSocketConnections",
                "RawWebSocketConnections",
                "ws-current-connections",
                "websocket-connections"
            };

            foreach (var metricName in metricNames)
            {
                var recentMetrics = _timeSeriesDb.GetRecentMetrics(metricName, 10);
                if (recentMetrics.Count != 0)
                {
                    // Use weighted average of recent values
                    var weights = Enumerable.Range(1, recentMetrics.Count).Select(i => (double)i).ToArray();
                    var weightedSum = recentMetrics.Select((m, i) => m.Value * weights[i]).Sum();
                    var totalWeight = weights.Sum();

                    return (int)(weightedSum / totalWeight);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error reading stored WebSocket metrics");
            return 0;
        }
    }

    private int EstimateWebSocketFromUpgradePatterns()
    {
        try
        {
            // Track WebSocket upgrade requests from request analytics
            // Long-lived connections (>10 seconds average execution time) are likely WebSockets
            var upgradeRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalSeconds > 10) // Long-lived connections
                .Sum(a => a.ConcurrentExecutionPeaks);

            if (upgradeRequests == 0)
                return 0;

            // WebSocket connections are long-lived, estimate based on concurrent peaks
            var estimatedConnections = (int)(upgradeRequests * 0.3); // ~30% are likely WebSockets

            // Apply time-of-day adjustment
            var hourOfDay = DateTime.UtcNow.Hour;
            var timeOfDayFactor = CalculateTimeOfDayWebSocketFactor(hourOfDay);
            estimatedConnections = (int)(estimatedConnections * timeOfDayFactor);

            return Math.Max(0, estimatedConnections);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating WebSocket from upgrade patterns");
            return 0;
        }
    }

    private int EstimateWebSocketFromHistoricalPatterns()
    {
        try
        {
            var historicalData = _timeSeriesDb.GetRecentMetrics("WebSocketConnections", 100);

            if (historicalData.Count < 20)
                return 0;

            // Find similar time periods (same hour of day ±1 hour)
            var currentHour = DateTime.UtcNow.Hour;
            var similarTimeData = historicalData
                .Where(m => Math.Abs(m.Timestamp.Hour - currentHour) <= 1)
                .ToList();

            if (similarTimeData.Count != 0)
            {
                // Use median of similar time periods
                var sortedValues = similarTimeData.Select(m => m.Value).OrderBy(v => v).ToList();
                var median = sortedValues[sortedValues.Count / 2];

                // Apply current load adjustment
                var loadLevel = ClassifyCurrentLoadLevel();
                var loadFactor = GetLoadBasedConnectionAdjustment(loadLevel);

                return (int)(median * loadFactor);
            }

            // Fallback: Use overall EMA
            var ema = _utilities.CalculateEMA([.. historicalData.Select(m => (double)m.Value)], alpha: 0.3);
            return Math.Max(0, (int)ema);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating WebSocket from historical patterns");
            return 0;
        }
    }

    private int EstimateWebSocketFromActiveRequests()
    {
        try
        {
            // Fallback: Estimate based on active request patterns
            var activeRequests = GetActiveRequestCount();
            var longLivedRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalSeconds > 5)
                .Sum(a => a.ConcurrentExecutionPeaks);

            // Assume 20% of long-lived requests are WebSockets
            var estimated = (int)(longLivedRequests * 0.2);

            // Fallback to 5% of all active requests if no long-lived data
            if (estimated == 0 && activeRequests > 0)
            {
                estimated = Math.Max(1, activeRequests / 20);
            }

            return Math.Min(estimated, _options.MaxEstimatedWebSocketConnections / 4);
        }
        catch
        {
            return 0;
        }
    }

    private double CalculateTimeOfDayWebSocketFactor(int hourOfDay)
    {
        // WebSocket usage patterns by time of day
        if (hourOfDay >= 9 && hourOfDay <= 17)
            return 1.4; // Business hours - higher usage
        else if (hourOfDay >= 18 && hourOfDay <= 22)
            return 1.1; // Evening - moderate usage
        else if (hourOfDay >= 23 || hourOfDay <= 6)
            return 0.6; // Night - lower usage
        else
            return 0.9; // Early morning - low usage
    }

    private LoadLevel ClassifyCurrentLoadLevel()
    {
        try
        {
            var cpuUsage = _systemMetrics.CalculateMemoryUsage(); // Note: would use CPU if available
            var throughput = _systemMetrics.CalculateCurrentThroughput();

            // Simple load classification
            if (throughput > 100 || cpuUsage > 0.8)
                return LoadLevel.High;
            else if (throughput > 50 || cpuUsage > 0.6)
                return LoadLevel.Medium;
            else if (throughput > 10 || cpuUsage > 0.3)
                return LoadLevel.Low;
            else
                return LoadLevel.Idle;
        }
        catch
        {
            return LoadLevel.Medium;
        }
    }

    private double GetLoadBasedConnectionAdjustment(LoadLevel level)
    {
        return level switch
        {
            LoadLevel.Critical => 1.3,  // 30% more connections under stress
            LoadLevel.High => 1.2,      // 20% more connections
            LoadLevel.Medium => 1.0,    // Normal
            LoadLevel.Low => 0.9,       // 10% fewer
            LoadLevel.Idle => 0.8,      // 20% fewer
            _ => 1.0
        };
    }

    // Delegates to SystemMetricsCalculator
    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
}
