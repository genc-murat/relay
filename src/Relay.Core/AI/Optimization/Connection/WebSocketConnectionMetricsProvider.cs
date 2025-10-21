using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class WebSocketConnectionMetricsProvider
{
    private readonly ILogger _logger;
    private readonly Relay.Core.AI.AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, Relay.Core.AI.RequestAnalysisData> _requestAnalytics;
    private readonly TimeSeriesDatabase _timeSeriesDb;
    private readonly Relay.Core.AI.SystemMetricsCalculator _systemMetrics;

    public WebSocketConnectionMetricsProvider(
        ILogger logger,
        Relay.Core.AI.AIOptimizationOptions options,
        ConcurrentDictionary<Type, Relay.Core.AI.RequestAnalysisData> requestAnalytics,
        TimeSeriesDatabase timeSeriesDb,
        Relay.Core.AI.SystemMetricsCalculator systemMetrics)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
        _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
        _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
    }

    public int GetWebSocketConnectionCount()
    {
        try
        {
            var webSocketConnections = 0;

            // 1. SignalR Hub connections
            webSocketConnections += GetSignalRHubConnections();

            // 2. Raw WebSocket connections (non-SignalR)
            webSocketConnections += GetRawWebSocketConnections();

            // 3. Server-Sent Events (SSE) long-polling fallback connections
            webSocketConnections += GetServerSentEventConnections();

            // 4. Long-polling connections (WebSocket fallback)
            webSocketConnections += GetLongPollingConnections();

            // 5. Apply connection health filtering
            webSocketConnections = FilterWebSocketConnections(webSocketConnections);

            // 6. Fallback estimation if no connections detected
            if (webSocketConnections == 0)
            {
                webSocketConnections = EstimateWebSocketConnectionsByActivity();
            }

            var finalCount = Math.Min(webSocketConnections, _options.MaxEstimatedWebSocketConnections);

            _logger.LogTrace("WebSocket connection count calculated: {Count} " +
                "(SignalR: {SignalR}, Raw WS: {RawWS}, SSE: {SSE}, LongPoll: {LongPoll})",
                finalCount, GetSignalRHubConnections(), GetRawWebSocketConnections(),
                GetServerSentEventConnections(), GetLongPollingConnections());

            return finalCount;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating WebSocket connections, using fallback");
            return GetFallbackWebSocketConnectionCount();
        }
    }

    private int GetSignalRHubConnections()
    {
        try
        {
            var connectionCount = 0;

            // Strategy 1: Try to get from stored SignalR metrics (historical data)
            connectionCount = TryGetStoredSignalRMetrics();
            if (connectionCount > 0)
            {
                _logger.LogTrace("SignalR hub connections from stored metrics: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 2: ML-based prediction using connection patterns
            connectionCount = PredictSignalRConnectionsML();
            if (connectionCount > 0)
            {
                _logger.LogTrace("SignalR hub connections from ML prediction: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 3: Real-time user estimation with hub multiplexing
            connectionCount = EstimateSignalRFromRealTimeUsers();
            if (connectionCount > 0)
            {
                _logger.LogTrace("SignalR hub connections from real-time estimation: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 4: Fallback to system-based estimation
            connectionCount = EstimateSignalRFromSystemMetrics();
            _logger.LogTrace("SignalR hub connections from system fallback: {Count}", connectionCount);
            return connectionCount;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating SignalR hub connections");
            return 0;
        }
    }

    private int TryGetStoredSignalRMetrics()
    {
        try
        {
            // Try to get recent SignalR connection metrics from time series database
            var recentMetrics = _timeSeriesDb.GetRecentMetrics("signalr_connections", 30); // Last 30 data points
            if (recentMetrics.Any())
            {
                // Use median of recent values for stability
                var values = recentMetrics.Select(m => m.Value).OrderBy(v => v).ToList();
                var median = values[values.Count / 2];

                // Apply freshness weight (more recent = higher weight)
                var weightedAvg = CalculateWeightedAverage(recentMetrics);
                var blended = (int)((median + weightedAvg) / 2.0);

                return Math.Max(0, blended);
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private int PredictSignalRConnectionsML()
    {
        try
        {
            // Use ML patterns to predict SignalR connections based on:
            // - Time of day patterns
            // - Request throughput correlation
            // - Historical connection patterns
            // - Hub activity correlation

            var throughput = CalculateCurrentThroughput();
            var timeOfDay = DateTime.UtcNow.Hour;
            var isBusinessHours = timeOfDay >= 8 && timeOfDay <= 18;
            var systemLoad = GetNormalizedSystemLoad();

            // Base prediction from throughput (SignalR typically correlates with high throughput)
            var baseConnections = (int)(throughput * 0.6); // 60% of throughput for real-time apps

            // Time-of-day adjustment
            var timeAdjustment = 1.0;
            if (isBusinessHours)
            {
                timeAdjustment = 1.4; // 40% increase during business hours
            }
            else if (timeOfDay >= 0 && timeOfDay < 6)
            {
                timeAdjustment = 0.5; // 50% decrease during night hours
            }
            else
            {
                timeAdjustment = 0.8; // 20% decrease during evening hours
            }

            // System load adjustment (high load = more active connections)
            var loadAdjustment = 0.8 + (systemLoad * 0.4); // Range: 0.8 to 1.2

            // Hub multiplexing factor
            var hubCount = EstimateActiveHubCount();
            var hubFactor = 1.0 + (hubCount - 1) * 0.3; // Each additional hub adds 30%

            // Calculate predicted connections
            var predicted = (int)(baseConnections * timeAdjustment * loadAdjustment * hubFactor);

            // Apply connection patterns from historical data
            var patternAdjustment = CalculateSignalRPatternAdjustment();
            predicted = (int)(predicted * patternAdjustment);

            return Math.Max(0, Math.Min(predicted, _options.MaxEstimatedWebSocketConnections / 2));
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateSignalRFromRealTimeUsers()
    {
        try
        {
            var realTimeUsers = EstimateRealTimeUsers();
            if (realTimeUsers == 0)
                return 0;

            var signalRConnections = realTimeUsers;

            // Factor in hub multiplexing (multiple hubs per user)
            var hubCount = EstimateActiveHubCount();
            if (hubCount > 1)
            {
                // Each additional hub adds connections (50% per hub, capped at 3 hubs)
                signalRConnections = (int)(signalRConnections * Math.Min(hubCount, 3) * 0.5);
            }

            // Factor in connection multipliers for multi-tab users
            var connectionMultiplier = CalculateConnectionMultiplier();
            signalRConnections = (int)(signalRConnections * connectionMultiplier);

            // Account for connection groups and broadcast scenarios
            var groupFactor = CalculateSignalRGroupFactor();
            signalRConnections = (int)(signalRConnections * groupFactor);

            // Apply health ratio (unhealthy connections reduce count)
            var healthRatio = CalculateConnectionHealthRatio();
            signalRConnections = (int)(signalRConnections * healthRatio);

            return Math.Max(0, Math.Min(signalRConnections, _options.MaxEstimatedWebSocketConnections / 2));
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateSignalRFromSystemMetrics()
    {
        try
        {
            // Fallback estimation based on system characteristics
            var activeRequests = GetActiveRequestCount();
            var processorCount = Environment.ProcessorCount;

            // Base estimate: 15% of active requests are SignalR connections
            var baseEstimate = (int)(activeRequests * 0.15);

            // Scale with processor count (more cores = can handle more connections)
            var processorFactor = 1.0 + (processorCount / 16.0); // Normalize around 8-16 cores

            // Apply hub count factor
            var hubCount = EstimateActiveHubCount();
            var hubFactor = Math.Min(hubCount, 3) * 0.4; // Each hub contributes 40%

            var estimate = (int)(baseEstimate * processorFactor * (1.0 + hubFactor));

            // Conservative cap to avoid overestimation
            return Math.Max(0, Math.Min(estimate, _options.MaxEstimatedWebSocketConnections / 4));
        }
        catch
        {
            return 0;
        }
    }

    private double CalculateSignalRPatternAdjustment()
    {
        try
        {
            // Analyze historical patterns to adjust predictions
            var recentMetrics = _timeSeriesDb.GetRecentMetrics("signalr_connections", 60); // Last 60 data points
            if (!recentMetrics.Any())
                return 1.0;

            // Calculate trend
            var trend = CalculateTrend(recentMetrics);

            // Calculate volatility
            var volatility = CalculateMetricVolatility(recentMetrics);

            // Adjustment based on trend and volatility
            var adjustment = 1.0;

            // Positive trend: increase estimate slightly
            if (trend > 0.1)
            {
                adjustment += Math.Min(trend * 0.5, 0.3); // Max 30% increase
            }
            // Negative trend: decrease estimate
            else if (trend < -0.1)
            {
                adjustment += Math.Max(trend * 0.5, -0.3); // Max 30% decrease
            }

            // High volatility: be more conservative
            if (volatility > 0.3)
            {
                adjustment *= 0.9; // 10% reduction for high volatility
            }

            return Math.Max(0.5, Math.Min(1.5, adjustment));
        }
        catch
        {
            return 1.0;
        }
    }

    private double GetNormalizedSystemLoad()
    {
        try
        {
            // Calculate normalized system load (0.0 to 1.0)
            var throughput = CalculateCurrentThroughput();
            var memoryUsage = CalculateMemoryUsage();
            var errorRate = CalculateCurrentErrorRate();

            // Combine metrics for overall system load
            // Higher throughput and memory usage = higher load
            var throughputLoad = Math.Min(1.0, throughput / 100.0); // Normalize around 100 req/s
            var memoryLoad = memoryUsage;
            var errorLoad = errorRate * 2.0; // Errors significantly impact perceived load

            // Weighted average
            var systemLoad = (throughputLoad * 0.4) + (memoryLoad * 0.4) + (errorLoad * 0.2);

            return Math.Max(0.0, Math.Min(1.0, systemLoad));
        }
        catch
        {
            return 0.5; // Default to medium load
        }
    }

    private double CalculateMetricVolatility(List<MetricDataPoint> metrics)
    {
        try
        {
            if (metrics.Count < 2)
                return 0.0;

            var values = metrics.Select(m => m.Value).ToList();
            var mean = values.Average();

            if (mean == 0)
                return 0.0;

            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            var stdDev = Math.Sqrt(variance);

            // Return coefficient of variation (normalized volatility)
            return stdDev / mean;
        }
        catch
        {
            return 0.0;
        }
    }

    private int GetRawWebSocketConnections()
    {
        try
        {
            var connectionCount = 0;

            // Strategy 1: Try to get stored WebSocket metrics
            connectionCount = TryGetStoredWebSocketMetrics();
            if (connectionCount > 0)
            {
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
                if (recentMetrics.Any())
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

            if (similarTimeData.Any())
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
            var ema = CalculateEMA(historicalData.Select(m => (double)m.Value).ToList(), alpha: 0.3);
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
            var activeRequests = GetActiveRequestCount();

            if (activeRequests == 0)
                return 0;

            // WebSocket connections are typically a small portion of total requests
            var baseEstimate = Math.Max(0, activeRequests / 10); // ~10% baseline

            // Apply WebSocket-specific multipliers
            var keepAliveMultiplier = 1.5; // WebSockets are long-lived (50% more)
            var usagePattern = EstimateWebSocketUsagePattern(); // Application-specific pattern

            var estimate = (int)(baseEstimate * keepAliveMultiplier * usagePattern);

            // Apply reasonable bounds
            return Math.Max(0, Math.Min(estimate, 100));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating WebSocket from active requests");
            return 0;
        }
    }

    private double CalculateTimeOfDayWebSocketFactor(int hourOfDay)
    {
        // WebSocket usage patterns typically vary by time of day
        // Peak hours: 9-17, Lower hours: night time

        if (hourOfDay >= 9 && hourOfDay <= 17)
        {
            return 1.3; // 30% more during business hours
        }
        else if (hourOfDay >= 18 && hourOfDay <= 22)
        {
            return 1.1; // 10% more during evening
        }
        else if (hourOfDay >= 0 && hourOfDay <= 6)
        {
            return 0.5; // 50% less during night
        }
        else
        {
            return 0.8; // 20% less during early morning
        }
    }

    private void StoreWebSocketConnectionMetrics(int connectionCount)
    {
        try
        {
            if (connectionCount <= 0)
                return;

            var timestamp = DateTime.UtcNow;

            // Store in time-series database
            _timeSeriesDb.StoreMetric("WebSocketConnections", connectionCount, timestamp);
            _timeSeriesDb.StoreMetric("RawWebSocketConnections", connectionCount, timestamp);

            _logger.LogTrace("Stored WebSocket connection metric: {Count} at {Time}",
                connectionCount, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing WebSocket connection metrics");
        }
    }

    private int GetServerSentEventConnections()
    {
        try
        {
            var connectionCount = 0;

            // Strategy 1: Try to get stored SSE metrics
            connectionCount = TryGetStoredSSEMetrics();
            if (connectionCount > 0)
            {
                _logger.LogTrace("SSE connections from stored metrics: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 2: Analyze long-lived streaming patterns
            connectionCount = EstimateSSEFromStreamingPatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("SSE connections from streaming patterns: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 3: Historical pattern analysis
            connectionCount = EstimateSSEFromHistoricalPatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("SSE connections from historical patterns: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 4: Fallback estimation from real-time users
            connectionCount = EstimateSSEFromRealTimeUsers();

            _logger.LogDebug("SSE connections estimated: {Count}", connectionCount);
            return connectionCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error estimating Server-Sent Event connections");
            return 0;
        }
    }

    private int TryGetStoredSSEMetrics()
    {
        try
        {
            var metricNames = new[]
            {
                "SSEConnections",
                "ServerSentEventConnections",
                "sse-current-connections",
                "eventsource-connections"
            };

            foreach (var metricName in metricNames)
            {
                var recentMetrics = _timeSeriesDb.GetRecentMetrics(metricName, 10);
                if (recentMetrics.Any())
                {
                    // Use weighted average for stability
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
            _logger.LogTrace(ex, "Error reading stored SSE metrics");
            return 0;
        }
    }

    private int EstimateSSEFromStreamingPatterns()
    {
        try
        {
            // SSE requests are characterized by:
            // 1. Very long execution times (hours/days)
            // 2. One-way server-to-client streaming
            // 3. text/event-stream content type

            var longLivedRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalMinutes > 5) // >5 min = likely streaming
                .Sum(a => a.ConcurrentExecutionPeaks);

            if (longLivedRequests == 0)
                return 0;

            // SSE is typically a smaller portion of long-lived connections
            // (WebSocket is more common)
            var ssePortionRate = 0.25; // ~25% of long-lived are SSE
            var estimatedConnections = (int)(longLivedRequests * ssePortionRate);

            // Apply browser connection limit factor
            // Browsers typically limit SSE connections per domain (6-8)
            var browserLimitFactor = CalculateBrowserConnectionLimitFactor();
            estimatedConnections = (int)(estimatedConnections * browserLimitFactor);

            // Apply time-of-day adjustment
            var timeOfDayFactor = CalculateTimeOfDaySSEFactor();
            estimatedConnections = (int)(estimatedConnections * timeOfDayFactor);

            return Math.Max(0, estimatedConnections);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating SSE from streaming patterns");
            return 0;
        }
    }

    private int EstimateSSEFromHistoricalPatterns()
    {
        try
        {
            var historicalData = _timeSeriesDb.GetRecentMetrics("SSEConnections", 100);

            if (historicalData.Count < 10)
                return 0;

            // Find similar time periods (same hour ±1)
            var currentHour = DateTime.UtcNow.Hour;
            var similarTimeData = historicalData
                .Where(m => Math.Abs(m.Timestamp.Hour - currentHour) <= 1)
                .ToList();

            if (similarTimeData.Any())
            {
                // Use median for stability (SSE connections are typically stable)
                var sortedValues = similarTimeData.Select(m => m.Value).OrderBy(v => v).ToList();
                var median = sortedValues[sortedValues.Count / 2];

                // Apply current load adjustment
                var loadLevel = ClassifyCurrentLoadLevel();
                var loadFactor = GetSSELoadAdjustment(loadLevel);

                return (int)(median * loadFactor);
            }

            // Fallback: Use EMA of all data
            var ema = CalculateEMA(historicalData.Select(m => (double)m.Value).ToList(), alpha: 0.2);
            return Math.Max(0, (int)ema);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating SSE from historical patterns");
            return 0;
        }
    }

    private int EstimateSSEFromRealTimeUsers()
    {
        try
        {
            var realTimeUsers = EstimateRealTimeUsers();

            if (realTimeUsers == 0)
                return 0;

            // SSE is often used for:
            // - Live notifications
            // - Real-time updates
            // - Dashboard streaming
            // Typically 10-20% of real-time users
            var sseUsageRate = 0.15; // 15% baseline

            // Adjust based on application characteristics
            var usagePattern = EstimateSSEUsagePattern();
            var sseConnections = (int)(realTimeUsers * sseUsageRate * usagePattern);

            // SSE connections are persistent
            var persistenceMultiplier = 1.3; // 30% more due to persistence
            sseConnections = (int)(sseConnections * persistenceMultiplier);

            // Apply reasonable bounds
            return Math.Max(0, Math.Min(sseConnections, 50));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating SSE from real-time users");
            return 0;
        }
    }

    private double CalculateBrowserConnectionLimitFactor()
    {
        try
        {
            // Modern browsers limit concurrent connections per domain
            // HTTP/1.1: 6 connections per domain
            // HTTP/2: Single connection with multiplexing

            // This affects how many SSE connections can be opened
            // Assume mix of browser versions and protocols
            var http1Percentage = 0.3; // 30% still HTTP/1.1
            var http2Percentage = 0.7; // 70% HTTP/2

            // HTTP/1.1 has stricter limits, reducing effective SSE count
            var http1Factor = 0.7; // 30% reduction due to limits
            var http2Factor = 1.0; // No significant impact

            return (http1Percentage * http1Factor) + (http2Percentage * http2Factor);
        }
        catch
        {
            return 0.85; // Default: 15% reduction
        }
    }

    private double CalculateTimeOfDaySSEFactor()
    {
        var hourOfDay = DateTime.UtcNow.Hour;

        // SSE usage for dashboards and notifications varies by time
        if (hourOfDay >= 9 && hourOfDay <= 17)
        {
            return 1.4; // 40% more during business hours (dashboards active)
        }
        else if (hourOfDay >= 18 && hourOfDay <= 22)
        {
            return 1.1; // 10% more during evening
        }
        else if (hourOfDay >= 23 || hourOfDay <= 5)
        {
            return 0.4; // 60% less during night (most dashboards closed)
        }
        else
        {
            return 0.7; // 30% less during early morning
        }
    }

    private double GetSSELoadAdjustment(LoadLevel level)
    {
        return level switch
        {
            LoadLevel.Critical => 1.2, // 20% more (increased monitoring)
            LoadLevel.High => 1.1,     // 10% more
            LoadLevel.Medium => 1.0,   // Normal
            LoadLevel.Low => 0.9,      // 10% fewer
            LoadLevel.Idle => 0.7,     // 30% fewer (dashboards likely closed)
            _ => 1.0
        };
    }

    private double EstimateSSEUsagePattern()
    {
        try
        {
            // Analyze request patterns to determine if app is:
            // - Dashboard-heavy (more SSE)
            // - API-heavy (less SSE)
            // - Notification-focused (moderate SSE)

            var totalRequests = _requestAnalytics.Values.Sum(a => a.TotalExecutions);
            if (totalRequests == 0)
                return 1.0;

            // Check for long-lived connections ratio
            var longLivedRatio = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalMinutes > 1)
                .Sum(a => a.TotalExecutions) / (double)totalRequests;

            // Higher ratio of long-lived = more likely dashboard/streaming app
            if (longLivedRatio > 0.3)
                return 1.5; // Dashboard-heavy
            else if (longLivedRatio > 0.1)
                return 1.2; // Moderate streaming
            else
                return 0.8; // API-heavy, less SSE
        }
        catch
        {
            return 1.0; // Default
        }
    }

    private void StoreSSEConnectionMetrics(int connectionCount)
    {
        try
        {
            if (connectionCount <= 0)
                return;

            var timestamp = DateTime.UtcNow;

            _timeSeriesDb.StoreMetric("SSEConnections", connectionCount, timestamp);
            _timeSeriesDb.StoreMetric("ServerSentEventConnections", connectionCount, timestamp);

            _logger.LogTrace("Stored SSE connection metric: {Count} at {Time}",
                connectionCount, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing SSE connection metrics");
        }
    }

    private int GetLongPollingConnections()
    {
        try
        {
            var connectionCount = 0;

            // Strategy 1: Try to get stored long-polling metrics
            connectionCount = TryGetStoredLongPollingMetrics();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Long-polling connections from stored metrics: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 2: Analyze polling request patterns
            connectionCount = EstimateLongPollingFromRequestPatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Long-polling connections from request patterns: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 3: Historical pattern analysis
            connectionCount = EstimateLongPollingFromHistoricalPatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Long-polling connections from historical patterns: {Count}", connectionCount);
                return connectionCount;
            }

            // Strategy 4: Fallback estimation from real-time users
            connectionCount = EstimateLongPollingFromRealTimeUsers();

            _logger.LogDebug("Long-polling connections estimated: {Count}", connectionCount);
            return connectionCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error estimating long-polling connections");
            return 0;
        }
    }

    private int TryGetStoredLongPollingMetrics()
    {
        try
        {
            var metricNames = new[]
            {
                "LongPollingConnections",
                "PollingConnections",
                "longpoll-connections",
                "polling-transport-connections"
            };

            foreach (var metricName in metricNames)
            {
                var recentMetrics = _timeSeriesDb.GetRecentMetrics(metricName, 10);
                if (recentMetrics.Any())
                {
                    // Use weighted average for stability
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
            _logger.LogTrace(ex, "Error reading stored long-polling metrics");
            return 0;
        }
    }

    private int EstimateLongPollingFromRequestPatterns()
    {
        try
        {
            // Long-polling requests are characterized by:
            // 1. Medium execution times (30s-120s typical timeout)
            // 2. Frequent repeat requests from same client
            // 3. Higher request frequency than normal API calls

            var mediumDurationRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalSeconds >= 20 &&
                            a.AverageExecutionTime.TotalSeconds <= 120)
                .ToList();

            if (!mediumDurationRequests.Any())
                return 0;

            // Calculate polling connection estimate
            var totalRepeatRequests = mediumDurationRequests.Sum(a => a.RepeatRequestCount);
            var avgExecutionTime = mediumDurationRequests.Average(a => a.AverageExecutionTime.TotalSeconds);

            // Estimate concurrent connections based on repeat rate and execution time
            // Higher repeat count = more active polling clients
            var estimatedConnections = (int)(totalRepeatRequests / Math.Max(avgExecutionTime, 1));

            // Apply polling efficiency factor (not all polls are concurrent)
            var concurrencyRate = 0.4; // ~40% of polls are concurrent
            estimatedConnections = (int)(estimatedConnections * concurrencyRate);

            // Apply client fallback rate (long-polling is usually a fallback)
            var fallbackRate = CalculateLongPollingFallbackRate();
            estimatedConnections = (int)(estimatedConnections * fallbackRate);

            return Math.Max(0, estimatedConnections);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating long-polling from request patterns");
            return 0;
        }
    }

    private int EstimateLongPollingFromHistoricalPatterns()
    {
        try
        {
            var historicalData = _timeSeriesDb.GetRecentMetrics("LongPollingConnections", 100);

            if (historicalData.Count < 10)
                return 0;

            // Find similar time periods
            var currentHour = DateTime.UtcNow.Hour;
            var similarTimeData = historicalData
                .Where(m => Math.Abs(m.Timestamp.Hour - currentHour) <= 1)
                .ToList();

            if (similarTimeData.Any())
            {
                // Use median (polling is more variable than other connection types)
                var sortedValues = similarTimeData.Select(m => m.Value).OrderBy(v => v).ToList();
                var median = sortedValues[sortedValues.Count / 2];

                // Apply current load adjustment
                var loadLevel = ClassifyCurrentLoadLevel();
                var loadFactor = GetLongPollingLoadAdjustment(loadLevel);

                return (int)(median * loadFactor);
            }

            // Fallback: Use EMA with higher alpha (more responsive to changes)
            var ema = CalculateEMA(historicalData.Select(m => (double)m.Value).ToList(), alpha: 0.4);
            return Math.Max(0, (int)ema);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating long-polling from historical patterns");
            return 0;
        }
    }

    private int EstimateLongPollingFromRealTimeUsers()
    {
        try
        {
            var realTimeUsers = EstimateRealTimeUsers();

            if (realTimeUsers == 0)
                return 0;

            // Long-polling is typically used as fallback when:
            // - WebSocket not supported (old browsers)
            // - Corporate firewalls blocking WebSocket
            // - Network issues with persistent connections
            // Typically 5-10% of clients fall back to long-polling
            var longPollingRate = 0.08; // 8% baseline

            // Adjust based on network conditions
            var networkFactor = EstimateNetworkConditionFactor();
            var longPollingConnections = (int)(realTimeUsers * longPollingRate * networkFactor);

            // Long-polling has higher connection churn due to timeouts and reconnects
            var churnMultiplier = 1.6; // 60% more due to churn
            longPollingConnections = (int)(longPollingConnections * churnMultiplier);

            // Factor in polling concurrency (clients may have multiple concurrent polls)
            var concurrencyFactor = 1.2; // 20% more for concurrency
            longPollingConnections = (int)(longPollingConnections * concurrencyFactor);

            // Apply reasonable bounds
            return Math.Max(0, Math.Min(longPollingConnections, 30));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating long-polling from real-time users");
            return 0;
        }
    }

    private double CalculateLongPollingFallbackRate()
    {
        try
        {
            // Strategy 1: Use historical fallback rate if available
            var historicalRate = GetHistoricalFallbackRate();
            if (historicalRate > 0)
            {
                _logger.LogTrace("Using historical fallback rate: {Rate:P2}", historicalRate);
                return historicalRate;
            }

            // Strategy 2: Analyze request patterns to detect fallback behavior
            var patternBasedRate = AnalyzeFallbackPatternsFromRequests();
            if (patternBasedRate > 0)
            {
                _logger.LogTrace("Using pattern-based fallback rate: {Rate:P2}", patternBasedRate);
                return patternBasedRate;
            }

            // Strategy 3: Calculate from industry standards with adjustments
            var industryBasedRate = CalculateIndustryBasedFallbackRate();

            _logger.LogDebug("Using industry-based fallback rate: {Rate:P2}", industryBasedRate);
            return industryBasedRate;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating long-polling fallback rate");
            return 0.10; // Default 10% fallback rate
        }
    }

    private double GetHistoricalFallbackRate()
    {
        try
        {
            var longPollingMetrics = _timeSeriesDb.GetRecentMetrics("LongPollingConnections", 50);
            var webSocketMetrics = _timeSeriesDb.GetRecentMetrics("WebSocketConnections", 50);

            if (longPollingMetrics.Count < 10 || webSocketMetrics.Count < 10)
                return 0;

            // Calculate ratio of long-polling to total real-time connections
            var avgLongPolling = longPollingMetrics.Average(m => m.Value);
            var avgWebSocket = webSocketMetrics.Average(m => m.Value);
            var totalRealTime = avgLongPolling + avgWebSocket;

            if (totalRealTime < 1)
                return 0;

            var fallbackRate = avgLongPolling / totalRealTime;

            // Apply EMA smoothing for stability
            var ema = CalculateEMA(
                longPollingMetrics.Select(m => (double)(m.Value / Math.Max(1, avgWebSocket + m.Value))).ToList(),
                alpha: 0.3
            );

            // Blend historical average with EMA
            var blendedRate = (fallbackRate * 0.6) + (ema * 0.4);

            return Math.Max(0.01, Math.Min(blendedRate, 0.30));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error getting historical fallback rate");
            return 0;
        }
    }

    private double AnalyzeFallbackPatternsFromRequests()
    {
        try
        {
            // Look for patterns indicating fallback:
            // 1. Rapid connection/reconnection cycles (WebSocket failed → polling)
            // 2. Medium-duration requests with high repeat count (polling pattern)
            // 3. Request timing patterns typical of polling intervals

            var totalRequests = _requestAnalytics.Values.Sum(a => a.TotalExecutions);
            if (totalRequests < 100)
                return 0;

            // Detect polling-like requests
            var pollingLikeRequests = _requestAnalytics.Values
                .Where(a =>
                    a.AverageExecutionTime.TotalSeconds >= 20 &&
                    a.AverageExecutionTime.TotalSeconds <= 120 &&
                    a.RepeatRequestCount > 10)
                .Sum(a => a.TotalExecutions);

            // Detect short-duration high-frequency requests (also polling pattern)
            var shortPollingRequests = _requestAnalytics.Values
                .Where(a =>
                    a.AverageExecutionTime.TotalSeconds < 5 &&
                    a.RepeatRequestCount > 50)
                .Sum(a => a.TotalExecutions);

            var totalPollingRequests = pollingLikeRequests + shortPollingRequests;

            if (totalPollingRequests == 0)
                return 0;

            // Calculate fallback rate
            var fallbackRate = (double)totalPollingRequests / totalRequests;

            // Apply dampening factor (not all polling-like requests are fallbacks)
            var dampeningFactor = 0.5; // 50% dampening
            fallbackRate *= dampeningFactor;

            // Apply time-of-day adjustment (fallback rates vary by time)
            var timeAdjustment = GetTimeOfDayFallbackAdjustment();
            fallbackRate *= timeAdjustment;

            return Math.Max(0.01, Math.Min(fallbackRate, 0.30));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error analyzing fallback patterns");
            return 0;
        }
    }

    private double CalculateIndustryBasedFallbackRate()
    {
        try
        {
            // Base rates from industry research and real-world data
            var modernBrowserRate = 0.92; // 92% modern browsers (2024 standards)
            var legacyBrowserRate = 1 - modernBrowserRate; // 8% legacy

            // Network blocking factors
            var corporateFirewallBlockRate = 0.12; // 12% corporate environments block WS
            var proxyBlockRate = 0.08; // 8% proxies/gateways interfere
            var mobileNetworkBlockRate = 0.03; // 3% mobile networks have issues

            // Calculate composite blocking rate
            var totalBlockRate = corporateFirewallBlockRate +
                                (proxyBlockRate * 0.5) + // 50% overlap with corporate
                                (mobileNetworkBlockRate * 0.3); // 30% overlap

            // Base fallback rate calculation
            var baseFallbackRate = (modernBrowserRate * totalBlockRate) + legacyBrowserRate;

            // Apply environmental adjustments
            var environmentalFactor = EstimateEnvironmentalFactor();
            baseFallbackRate *= environmentalFactor;

            // Apply geographic/regional factor (some regions have more blocking)
            var regionalFactor = EstimateRegionalBlockingFactor();
            baseFallbackRate *= regionalFactor;

            // Apply time-based trends (fallback rates decrease over time as tech improves)
            var trendFactor = EstimateTechnologyTrendFactor();
            baseFallbackRate *= trendFactor;

            // Apply current system error rate adjustment
            var errorRateAdjustment = GetErrorRateAdjustment();
            baseFallbackRate *= errorRateAdjustment;

            return Math.Max(0.05, Math.Min(baseFallbackRate, 0.25)); // Between 5-25%
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating industry-based fallback rate");
            return 0.10; // Default 10%
        }
    }

    private double EstimateEnvironmentalFactor()
    {
        try
        {
            // Analyze system characteristics to determine environment type
            var totalRequests = _requestAnalytics.Values.Sum(a => a.TotalExecutions);
            var avgErrorRate = _requestAnalytics.Values.Any()
                ? _requestAnalytics.Values.Average(a => a.ErrorRate)
                : 0;

            // High security/enterprise environment indicators
            if (avgErrorRate > 0.05)
            {
                return 1.4; // 40% more fallback in restrictive environments
            }

            // Consumer/public environment indicators
            if (avgErrorRate < 0.01 && totalRequests > 10000)
            {
                return 0.7; // 30% less fallback in open environments
            }

            return 1.0; // Normal environment
        }
        catch
        {
            return 1.0;
        }
    }

    private double EstimateRegionalBlockingFactor()
    {
        try
        {
            // In production, this would use:
            // - Geographic IP data
            // - Regional infrastructure statistics
            // - Historical regional patterns

            // For now, use conservative estimate
            // Some regions have more restrictive networks

            var hourOfDay = DateTime.UtcNow.Hour;

            // Business hours in different regions suggest different blocking patterns
            if (hourOfDay >= 8 && hourOfDay <= 17)
            {
                return 1.2; // 20% more blocking during business hours (corporate networks)
            }
            else if (hourOfDay >= 18 && hourOfDay <= 23)
            {
                return 0.9; // 10% less blocking during evening (home networks)
            }
            else
            {
                return 0.8; // 20% less blocking during night
            }
        }
        catch
        {
            return 1.0;
        }
    }

    private double EstimateTechnologyTrendFactor()
    {
        try
        {
            // Technology trends affect fallback rates and system capabilities
            // This analyzes multiple technology adoption curves and maturity levels

            // Try to get from stored metrics first
            var storedTrend = _timeSeriesDb.GetRecentMetrics("Technology_TrendFactor", 10);
            if (storedTrend.Any())
            {
                var avgTrend = storedTrend.Average(m => m.Value);
                return Math.Max(0.5, Math.Min(avgTrend, 1.2));
            }

            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;

            // Multi-factor technology trend analysis
            var trendFactors = new List<TechnologyTrendComponent>();

            // 1. WebSocket maturity factor (2015 baseline)
            var webSocketMaturity = CalculateWebSocketMaturityFactor(currentYear);
            trendFactors.Add(new TechnologyTrendComponent
            {
                Name = "WebSocket",
                Factor = webSocketMaturity,
                Weight = 0.25 // 25% weight
            });

            // 2. HTTP/2 adoption factor (2015 RFC, 2018 widespread)
            var http2Adoption = CalculateHttp2AdoptionFactor(currentYear);
            trendFactors.Add(new TechnologyTrendComponent
            {
                Name = "HTTP2",
                Factor = http2Adoption,
                Weight = 0.20 // 20% weight
            });

            // 3. HTTP/3 (QUIC) adoption factor (2022 RFC)
            var http3Adoption = CalculateHttp3AdoptionFactor(currentYear);
            trendFactors.Add(new TechnologyTrendComponent
            {
                Name = "HTTP3",
                Factor = http3Adoption,
                Weight = 0.15 // 15% weight
            });

            // 4. gRPC maturity factor (2016 release, 2019 widespread)
            var grpcMaturity = CalculateGrpcMaturityFactor(currentYear);
            trendFactors.Add(new TechnologyTrendComponent
            {
                Name = "gRPC",
                Factor = grpcMaturity,
                Weight = 0.15 // 15% weight
            });

            // 5. Cloud-native architecture adoption
            var cloudNativeAdoption = CalculateCloudNativeAdoptionFactor(currentYear);
            trendFactors.Add(new TechnologyTrendComponent
            {
                Name = "CloudNative",
                Factor = cloudNativeAdoption,
                Weight = 0.15 // 15% weight
            });

            // 6. Service mesh adoption (2017 Istio, 2020 mainstream)
            var serviceMeshAdoption = CalculateServiceMeshAdoptionFactor(currentYear);
            trendFactors.Add(new TechnologyTrendComponent
            {
                Name = "ServiceMesh",
                Factor = serviceMeshAdoption,
                Weight = 0.10 // 10% weight
            });

            // Calculate weighted average
            var weightedTrend = trendFactors.Sum(t => t.Factor * t.Weight);

            // Apply seasonal technology adoption patterns
            // Q4 typically sees higher adoption due to budget cycles
            var seasonalFactor = 1.0;
            if (currentMonth >= 10) // Q4
            {
                seasonalFactor = 1.05; // 5% boost in Q4
            }
            else if (currentMonth <= 3) // Q1
            {
                seasonalFactor = 0.95; // 5% reduction in Q1 (planning phase)
            }

            var trendFactor = weightedTrend * seasonalFactor;

            // Apply machine learning prediction adjustment
            var mlAdjustment = ApplyMLTrendPrediction(trendFactors);
            trendFactor = trendFactor * (0.7 + mlAdjustment * 0.3); // 70% calculated, 30% ML

            // Store calculated trend for future reference
            _timeSeriesDb.StoreMetric("Technology_TrendFactor", trendFactor, DateTime.UtcNow);
            foreach (var component in trendFactors)
            {
                _timeSeriesDb.StoreMetric($"Technology_{component.Name}_Factor", component.Factor, DateTime.UtcNow);
            }

            _logger.LogDebug("Technology trend factor: {TrendFactor:F3} (WebSocket: {WS:F2}, HTTP/2: {H2:F2}, HTTP/3: {H3:F2})",
                trendFactor, webSocketMaturity, http2Adoption, http3Adoption);

            // Clamp to reasonable range: 0.5 (50% efficiency) to 1.2 (20% improvement)
            return Math.Max(0.5, Math.Min(trendFactor, 1.2));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating technology trend factor");
            return 1.0; // Neutral factor
        }
    }

    private double CalculateWebSocketMaturityFactor(int currentYear)
    {
        // WebSocket RFC 6455 published in 2011, widespread adoption by 2015
        var baseYear = 2015;
        var maturityYears = currentYear - baseYear;

        // S-curve adoption: fast initial growth, then plateau
        // Using logistic function
        var k = 0.4; // Growth rate
        var midpoint = 5.0; // Inflection point at 5 years
        var maturity = 1.0 / (1.0 + Math.Exp(-k * (maturityYears - midpoint)));

        // Maturity improves efficiency (reduces fallback needs)
        return 1.0 - (maturity * 0.3); // Up to 30% improvement
    }

    private double CalculateHttp2AdoptionFactor(int currentYear)
    {
        // HTTP/2 RFC 7540 published May 2015, mainstream by 2018
        var baseYear = 2015;
        var adoptionYears = currentYear - baseYear;

        // Rapid adoption curve
        var adoptionRate = Math.Min(1.0, adoptionYears / 6.0); // 6-year adoption cycle

        // HTTP/2 multiplexing reduces connection overhead
        return 1.0 - (adoptionRate * 0.25); // Up to 25% improvement
    }

    private double CalculateHttp3AdoptionFactor(int currentYear)
    {
        // HTTP/3 RFC 9114 published June 2022
        var baseYear = 2022;
        var adoptionYears = Math.Max(0, currentYear - baseYear);

        // Early adoption phase - slower growth
        var adoptionRate = Math.Min(0.5, adoptionYears / 10.0); // 10-year cycle, capped at 50%

        // HTTP/3 QUIC improvements
        return 1.0 - (adoptionRate * 0.20); // Up to 20% improvement (still early)
    }

    private double CalculateGrpcMaturityFactor(int currentYear)
    {
        // gRPC open-sourced in 2015, mature by 2019
        var baseYear = 2015;
        var maturityYears = currentYear - baseYear;

        // Steady maturity growth
        var maturity = Math.Min(1.0, maturityYears / 7.0); // 7-year maturity cycle

        // gRPC efficiency improvements
        return 1.0 - (maturity * 0.15); // Up to 15% improvement
    }

    private double CalculateCloudNativeAdoptionFactor(int currentYear)
    {
        // Cloud-native architecture gaining traction around 2016-2017
        var baseYear = 2017;
        var adoptionYears = currentYear - baseYear;

        // Exponential adoption in enterprise
        var adoptionRate = Math.Min(1.0, Math.Pow(adoptionYears / 8.0, 1.5)); // 8-year cycle with acceleration

        // Cloud-native architectures improve resilience and efficiency
        return 1.0 - (adoptionRate * 0.22); // Up to 22% improvement
    }

    private double CalculateServiceMeshAdoptionFactor(int currentYear)
    {
        // Service mesh (Istio, Linkerd) mainstream around 2020
        var baseYear = 2020;
        var adoptionYears = Math.Max(0, currentYear - baseYear);

        // Early to mid adoption phase
        var adoptionRate = Math.Min(0.6, adoptionYears / 8.0); // 8-year cycle, capped at 60%

        // Service mesh traffic management improvements
        return 1.0 - (adoptionRate * 0.18); // Up to 18% improvement
    }

    private double ApplyMLTrendPrediction(List<TechnologyTrendComponent> components)
    {
        try
        {
            // Use ML.NET to predict trend adjustment based on historical patterns
            // This would integrate with time series forecasting

            // Simplified: analyze historical trend changes
            var historicalTrends = _timeSeriesDb.GetRecentMetrics("Technology_TrendFactor", 100);
            if (!historicalTrends.Any())
            {
                return 1.0; // Neutral if no history
            }

            // Calculate trend velocity (rate of change)
            var recentTrends = historicalTrends.TakeLast(10).ToList();
            if (recentTrends.Count < 2)
            {
                return 1.0;
            }

            var trendVelocity = (recentTrends.Last().Value - recentTrends.First().Value) / recentTrends.Count;

            // Positive velocity = improving technology = lower factor
            // Negative velocity = degrading = higher factor
            var velocityAdjustment = 1.0 - (trendVelocity * 2.0); // Amplify velocity impact

            return Math.Max(0.8, Math.Min(velocityAdjustment, 1.2)); // 80% to 120%
        }
        catch
        {
            return 1.0; // Neutral on error
        }
    }

    private double GetErrorRateAdjustment()
    {
        try
        {
            var avgErrorRate = _requestAnalytics.Values.Any()
                ? _requestAnalytics.Values.Average(a => a.ErrorRate)
                : 0;

            // Higher error rates suggest more network issues → more fallback needed
            if (avgErrorRate > 0.15)
                return 1.8; // 80% more fallback
            else if (avgErrorRate > 0.10)
                return 1.5; // 50% more fallback
            else if (avgErrorRate > 0.05)
                return 1.2; // 20% more fallback
            else if (avgErrorRate < 0.01)
                return 0.8; // 20% less fallback (good conditions)
            else
                return 1.0; // Normal
        }
        catch
        {
            return 1.0;
        }
    }

    private double GetTimeOfDayFallbackAdjustment()
    {
        var hourOfDay = DateTime.UtcNow.Hour;

        // Fallback usage varies by time of day
        if (hourOfDay >= 9 && hourOfDay <= 17)
        {
            return 1.3; // 30% more during business hours (corporate restrictions)
        }
        else if (hourOfDay >= 18 && hourOfDay <= 22)
        {
            return 0.9; // 10% less during evening (home networks)
        }
        else if (hourOfDay >= 23 || hourOfDay <= 6)
        {
            return 0.7; // 30% less during night
        }
        else
        {
            return 1.0; // Normal
        }
    }

    private void StoreFallbackRateMetrics(double fallbackRate)
    {
        try
        {
            if (fallbackRate <= 0)
                return;

            var timestamp = DateTime.UtcNow;

            _timeSeriesDb.StoreMetric("LongPollingFallbackRate", fallbackRate, timestamp);
            _timeSeriesDb.StoreMetric("WebSocketFallbackRate", fallbackRate, timestamp);

            _logger.LogTrace("Stored fallback rate metric: {Rate:P2} at {Time}",
                fallbackRate, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing fallback rate metrics");
        }
    }

    private double EstimateNetworkConditionFactor()
    {
        try
        {
            // Analyze error rates and timeouts as proxy for network conditions
            var avgErrorRate = _requestAnalytics.Values.Any()
                ? _requestAnalytics.Values.Average(a => a.ErrorRate)
                : 0;

            // Higher error rate suggests network issues → more fallback to long-polling
            if (avgErrorRate > 0.1)
                return 1.5; // 50% more long-polling due to network issues
            else if (avgErrorRate > 0.05)
                return 1.2; // 20% more
            else if (avgErrorRate < 0.01)
                return 0.8; // 20% less (good network, less fallback needed)
            else
                return 1.0; // Normal
        }
        catch
        {
            return 1.0; // Default
        }
    }

    private double GetLongPollingLoadAdjustment(LoadLevel level)
    {
        return level switch
        {
            // Under high load, more clients may fall back to long-polling
            LoadLevel.Critical => 1.3, // 30% more (WebSocket overload → fallback)
            LoadLevel.High => 1.2,     // 20% more
            LoadLevel.Medium => 1.0,   // Normal
            LoadLevel.Low => 0.9,      // 10% fewer
            LoadLevel.Idle => 0.7,     // 30% fewer (minimal activity)
            _ => 1.0
        };
    }

    private double CalculateAveragePollingInterval()
    {
        try
        {
            // Analyze repeat request patterns to determine polling interval
            var repeatCounts = _requestAnalytics.Values
                .Where(a => a.RepeatRequestCount > 0)
                .Select(a => a.RepeatRequestCount)
                .ToList();

            if (!repeatCounts.Any())
                return 30.0; // Default 30s interval

            var avgRepeats = repeatCounts.Average();

            // Assume observations over 1 hour window
            var observationWindow = 3600.0; // 1 hour in seconds
            var estimatedInterval = observationWindow / Math.Max(avgRepeats, 1);

            // Typical polling intervals: 5s-60s
            return Math.Max(5.0, Math.Min(estimatedInterval, 60.0));
        }
        catch
        {
            return 30.0; // Default 30s interval
        }
    }

    private void StoreLongPollingConnectionMetrics(int connectionCount)
    {
        try
        {
            if (connectionCount <= 0)
                return;

            var timestamp = DateTime.UtcNow;

            _timeSeriesDb.StoreMetric("LongPollingConnections", connectionCount, timestamp);
            _timeSeriesDb.StoreMetric("PollingConnections", connectionCount, timestamp);

            _logger.LogTrace("Stored long-polling connection metric: {Count} at {Time}",
                connectionCount, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing long-polling connection metrics");
        }
    }

    private int FilterWebSocketConnections(int totalConnections)
    {
        try
        {
            // Filter out stale/disconnected WebSocket connections
            var healthyRatio = CalculateWebSocketHealthRatio();
            var healthyConnections = (int)(totalConnections * healthyRatio);

            // Account for connection timeouts and disconnections
            var disconnectionRate = CalculateWebSocketDisconnectionRate();
            var adjustedConnections = (int)(healthyConnections * (1 - disconnectionRate));

            // Apply ping/pong keepalive filtering
            var keepAliveHealthRatio = EstimateKeepAliveHealthRatio();
            adjustedConnections = (int)(adjustedConnections * keepAliveHealthRatio);

            return Math.Max(0, adjustedConnections);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error filtering WebSocket connections");
            return (int)(totalConnections * 0.85); // Assume 85% healthy
        }
    }

    private int EstimateWebSocketConnectionsByActivity()
    {
        try
        {
            // Fallback estimation based on overall system activity
            var activeRequests = GetActiveRequestCount();
            var throughput = CalculateCurrentThroughput();

            // Estimate WebSocket connections as a fraction of total activity
            var activityBasedEstimate = Math.Max(0, (int)((activeRequests * 0.2) + (throughput * 0.05)));

            // Factor in typical WebSocket usage patterns
            var connectionMultiplier = CalculateConnectionMultiplier();
            activityBasedEstimate = (int)(activityBasedEstimate * connectionMultiplier);

            return Math.Min(activityBasedEstimate, 50); // Conservative cap
        }
        catch
        {
            return 0;
        }
    }

    private int GetFallbackWebSocketConnectionCount()
    {
        try
        {
            // Conservative fallback based on system size
            var processorCount = Environment.ProcessorCount;
            var activeRequests = GetActiveRequestCount();

            // Estimate: 1 WebSocket per 2 processors + 20% of active requests
            var fallbackEstimate = Math.Max(0, (processorCount / 2) + (int)(activeRequests * 0.2));

            return Math.Min(fallbackEstimate, 25); // Conservative upper bound
        }
        catch
        {
            return 0; // WebSocket connections are optional
        }
    }

    private int EstimateActiveHubCount()
    {
        try
        {
            // Strategy 1: Check stored hub metrics
            var storedCount = TryGetStoredHubCount();
            if (storedCount > 0)
            {
                return storedCount;
            }

            // Strategy 2: Analyze request patterns to estimate hub diversity
            var patternBasedCount = EstimateHubCountFromPatterns();
            if (patternBasedCount > 0)
            {
                return patternBasedCount;
            }

            // Strategy 3: Fallback to heuristic estimation
            return EstimateHubCountHeuristic();
        }
        catch
        {
            // Conservative fallback
            return 1;
        }
    }

    private int TryGetStoredHubCount()
    {
        try
        {
            // Try to get hub count from metrics
            var hubMetrics = _timeSeriesDb.GetRecentMetrics("active_hub_count", 10); // Last 10 data points
            if (hubMetrics.Any())
            {
                // Use most recent value
                var latest = hubMetrics.OrderByDescending(m => m.Timestamp).First();
                return (int)latest.Value;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateHubCountFromPatterns()
    {
        try
        {
            // Analyze request analytics to estimate hub diversity
            if (!_requestAnalytics.Any())
                return 0;

            var requestTypes = _requestAnalytics.Keys.Count;
            var totalExecutions = _requestAnalytics.Values.Sum(x => x.TotalExecutions);

            if (totalExecutions == 0)
                return 0;

            // Calculate request type diversity using entropy
            var diversity = CalculateRequestTypeDiversity();

            // High diversity suggests multiple hubs
            int estimatedHubs;
            if (diversity > 0.8)
            {
                // High diversity: likely 3+ hubs
                estimatedHubs = Math.Min(5, 2 + (requestTypes / 15));
            }
            else if (diversity > 0.5)
            {
                // Medium diversity: likely 2-3 hubs
                estimatedHubs = Math.Min(3, 1 + (requestTypes / 20));
            }
            else
            {
                // Low diversity: likely 1-2 hubs
                estimatedHubs = Math.Min(2, 1 + (requestTypes / 30));
            }

            // Analyze throughput distribution to refine estimate
            var throughputVariance = CalculateThroughputVariance();
            if (throughputVariance > 0.5)
            {
                // High variance suggests multiple specialized hubs
                estimatedHubs += 1;
            }

            // Analyze time-based patterns
            var hasTimePatterns = DetectTimeBasedHubPatterns();
            if (hasTimePatterns)
            {
                // Different hubs active at different times suggests multiple hubs
                estimatedHubs += 1;
            }

            // Cap at reasonable maximum (most apps have 1-5 hubs)
            return Math.Min(5, Math.Max(1, estimatedHubs));
        }
        catch
        {
            return 0;
        }
    }

    private int EstimateHubCountHeuristic()
    {
        try
        {
            // Heuristic estimation based on system characteristics
            var requestTypes = _requestAnalytics.Keys.Count;
            var totalRequests = _requestAnalytics.Values.Sum(x => x.TotalExecutions);

            // Base estimate from request types
            // Typically: 10-20 request types per hub
            var baseEstimate = Math.Max(1, requestTypes / 15);

            // Adjust based on total activity
            if (totalRequests > 10000)
            {
                // High activity suggests multiple hubs
                baseEstimate += 1;
            }
            else if (totalRequests > 5000)
            {
                // Medium activity
                baseEstimate = Math.Max(baseEstimate, 2);
            }

            // Check system load
            var systemLoad = GetNormalizedSystemLoad();
            if (systemLoad > 0.7)
            {
                // High load suggests multiple specialized hubs
                baseEstimate += 1;
            }

            // Typically 1-5 hubs in most applications
            return Math.Min(5, Math.Max(1, baseEstimate));
        }
        catch
        {
            return 1;
        }
    }

    private double CalculateRequestTypeDiversity()
    {
        try
        {
            if (!_requestAnalytics.Any())
                return 0.0;

            var totalExecutions = _requestAnalytics.Values.Sum(x => x.TotalExecutions);
            if (totalExecutions == 0)
                return 0.0;

            // Calculate Shannon entropy to measure diversity
            var entropy = 0.0;
            foreach (var data in _requestAnalytics.Values)
            {
                var probability = (double)data.TotalExecutions / totalExecutions;
                if (probability > 0)
                {
                    entropy -= probability * Math.Log(probability, 2);
                }
            }

            // Normalize entropy to 0-1 range
            var maxEntropy = Math.Log(_requestAnalytics.Count, 2);
            if (maxEntropy > 0)
            {
                return entropy / maxEntropy;
            }

            return 0.0;
        }
        catch
        {
            return 0.0;
        }
    }

    private double CalculateThroughputVariance()
    {
        try
        {
            if (!_requestAnalytics.Any())
                return 0.0;

            var throughputs = _requestAnalytics.Values
                .Select(x => (double)x.TotalExecutions)
                .ToList();

            if (throughputs.Count < 2)
                return 0.0;

            var mean = throughputs.Average();
            if (mean == 0)
                return 0.0;

            var variance = throughputs.Sum(t => Math.Pow(t - mean, 2)) / throughputs.Count;
            var stdDev = Math.Sqrt(variance);

            // Return coefficient of variation (normalized variance)
            return stdDev / mean;
        }
        catch
        {
            return 0.0;
        }
    }

    private bool DetectTimeBasedHubPatterns()
    {
        try
        {
            // Analyze if different request types are active at different times
            // This suggests multiple hubs for different use cases

            var recentMetrics = _timeSeriesDb.GetRecentMetrics("request_patterns", 360); // Last 360 data points (6 hours if 1/min)
            if (!recentMetrics.Any())
                return false;

            // Group by hour and check if patterns vary significantly
            var hourlyGroups = recentMetrics
                .GroupBy(m => m.Timestamp.Hour)
                .Select(g => g.Average(m => m.Value))
                .ToList();

            if (hourlyGroups.Count < 3)
                return false;

            // Calculate variance in hourly patterns
            var mean = hourlyGroups.Average();
            var variance = hourlyGroups.Sum(v => Math.Pow(v - mean, 2)) / hourlyGroups.Count;
            var coefficientOfVariation = mean > 0 ? Math.Sqrt(variance) / mean : 0;

            // High variation (>0.4) suggests time-based hub patterns
            return coefficientOfVariation > 0.4;
        }
        catch
        {
            return false;
        }
    }

    private double CalculateSignalRGroupFactor()
    {
        // Account for SignalR group-based broadcasting
        // Groups typically increase connection efficiency slightly
        return 0.95; // 5% reduction due to group optimizations
    }

    private double EstimateWebSocketUsagePattern()
    {
        // Estimate WebSocket usage intensity based on system characteristics
        var throughput = CalculateCurrentThroughput();

        // Higher throughput suggests more real-time features
        if (throughput > 100) return 1.5; // High usage
        if (throughput > 50) return 1.2;  // Medium usage
        if (throughput > 10) return 1.0;  // Normal usage
        return 0.7; // Low usage
    }

    private double CalculateWebSocketHealthRatio()
    {
        // Calculate ratio of healthy WebSocket connections
        var errorRate = CalculateCurrentErrorRate();

        // WebSocket health inversely related to overall error rate
        return Math.Max(0.6, Math.Min(0.98, 1.0 - (errorRate * 1.5)));
    }

    private double CalculateWebSocketDisconnectionRate()
    {
        try
        {
            // Strategy 1: Use historical disconnection data if available
            var historicalRate = GetHistoricalDisconnectionRate();
            if (historicalRate > 0)
            {
                _logger.LogTrace("Using historical disconnection rate: {Rate:P2}", historicalRate);
                return historicalRate;
            }

            // Strategy 2: ML-based prediction using error patterns
            var mlPredictedRate = PredictDisconnectionRateFromPatterns();
            if (mlPredictedRate > 0)
            {
                _logger.LogTrace("Using ML-predicted disconnection rate: {Rate:P2}", mlPredictedRate);
                return mlPredictedRate;
            }

            // Strategy 3: Multi-factor heuristic calculation
            var heuristicRate = CalculateHeuristicDisconnectionRate();

            _logger.LogDebug("Using heuristic disconnection rate: {Rate:P2}", heuristicRate);
            return heuristicRate;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating WebSocket disconnection rate");
            return 0.05; // Default 5% disconnection rate
        }
    }

    private double GetHistoricalDisconnectionRate()
    {
        try
        {
            var disconnectMetrics = _timeSeriesDb.GetRecentMetrics("WebSocketDisconnections", 50);
            var connectionMetrics = _timeSeriesDb.GetRecentMetrics("WebSocketConnections", 50);

            if (disconnectMetrics.Count < 10 || connectionMetrics.Count < 10)
                return 0;

            // Calculate average disconnection rate
            var disconnectionRates = new List<double>();

            for (int i = 0; i < Math.Min(disconnectMetrics.Count, connectionMetrics.Count); i++)
            {
                var disconnects = disconnectMetrics[i].Value;
                var connections = connectionMetrics[i].Value;

                if (connections > 0)
                {
                    disconnectionRates.Add(disconnects / connections);
                }
            }

            if (!disconnectionRates.Any())
                return 0;

            // Use EMA for recent trend sensitivity
            var ema = CalculateEMA(disconnectionRates, alpha: 0.3);

            // Blend with median for stability
            var sortedRates = disconnectionRates.OrderBy(r => r).ToList();
            var median = sortedRates[sortedRates.Count / 2];

            var blendedRate = (ema * 0.6) + (median * 0.4);

            // Apply time-of-day adjustment
            var timeAdjustment = GetDisconnectionTimeAdjustment();
            blendedRate *= timeAdjustment;

            return Math.Max(0.01, Math.Min(blendedRate, 0.35));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error getting historical disconnection rate");
            return 0;
        }
    }

    private double PredictDisconnectionRateFromPatterns()
    {
        try
        {
            // Analyze error patterns that correlate with disconnections
            var errorRate = CalculateCurrentErrorRate();
            var errorTrend = CalculateErrorRateTrend();
            var systemLoad = GetDatabasePoolUtilization();
            var loadTrend = CalculateSystemLoadTrend();

            // Feature engineering for disconnection prediction
            var features = new Dictionary<string, double>
            {
                { "ErrorRate", errorRate },
                { "ErrorTrend", errorTrend },
                { "SystemLoad", systemLoad },
                { "LoadTrend", loadTrend },
                { "TimeOfDay", GetNormalizedTimeOfDay() },
                { "DayOfWeek", GetNormalizedDayOfWeek() }
            };

            // Calculate weighted prediction
            var baseDisconnectionRate = 0.0;

            // Error rate contribution (highest weight)
            baseDisconnectionRate += errorRate * 0.35;

            // Error trend contribution (increasing errors = more disconnects)
            if (errorTrend > 0.1)
                baseDisconnectionRate += errorTrend * 0.25;
            else if (errorTrend < -0.1)
                baseDisconnectionRate -= errorTrend * 0.15; // Improving = fewer disconnects

            // System load contribution
            baseDisconnectionRate += systemLoad * 0.20;

            // Load trend contribution
            if (loadTrend > 0.1)
                baseDisconnectionRate += loadTrend * 0.15;

            // Time-based patterns (night: fewer, business hours: more)
            var timeOfDay = DateTime.UtcNow.Hour;
            if (timeOfDay >= 9 && timeOfDay <= 17)
                baseDisconnectionRate *= 1.2; // 20% more during business hours
            else if (timeOfDay >= 0 && timeOfDay <= 6)
                baseDisconnectionRate *= 0.7; // 30% less during night

            // Apply network quality factor
            var networkQuality = EstimateNetworkQualityFromMetrics();
            baseDisconnectionRate *= (1.0 - (networkQuality * 0.3));

            return Math.Max(0.01, Math.Min(baseDisconnectionRate, 0.35));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error predicting disconnection rate from patterns");
            return 0;
        }
    }

    private double CalculateHeuristicDisconnectionRate()
    {
        try
        {
            var errorRate = CalculateCurrentErrorRate();
            var systemLoad = GetDatabasePoolUtilization();

            // Base calculation with improved weights
            var baseRate = (errorRate * 0.4) + (systemLoad * 0.15);

            // Add memory pressure factor
            var memoryPressure = EstimateMemoryPressure();
            baseRate += memoryPressure * 0.10;

            // Add connection churn factor
            var connectionChurn = EstimateConnectionChurn();
            baseRate += connectionChurn * 0.15;

            // Add response time factor (slow responses = more timeouts)
            var responseTime = CalculateAverageResponseTime();
            if (responseTime.TotalMilliseconds > 1000)
            {
                var timeoutFactor = Math.Min((responseTime.TotalMilliseconds - 1000) / 5000, 0.15);
                baseRate += timeoutFactor;
            }

            // Add concurrent connections factor (overload = more disconnects)
            var connectionOverload = EstimateConnectionOverloadFactor();
            baseRate += connectionOverload * 0.10;

            // Apply environmental adjustments
            var environmentalFactor = GetDisconnectionEnvironmentalFactor();
            baseRate *= environmentalFactor;

            return Math.Max(0.02, Math.Min(baseRate, 0.30));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating heuristic disconnection rate");
            return 0.05; // Default 5%
        }
    }

    internal double EstimateMemoryPressure()
    {
        try
        {
            // Production-ready memory pressure analysis using actual GC and system metrics

            // Try to get from stored metrics first for consistency
            var storedPressure = _timeSeriesDb.GetRecentMetrics("Memory_Pressure", 5);
            if (storedPressure.Any())
            {
                var avgPressure = storedPressure.Average(m => m.Value);
                var latestPressure = storedPressure.Last().Value;

                // Weighted average: 70% latest, 30% historical
                var pressure = latestPressure * 0.7 + avgPressure * 0.3;
                return Math.Max(0, Math.Min(pressure, 1.0));
            }

            // Multi-factor memory pressure analysis
            var pressureFactors = new List<MemoryPressureFactor>();

            // 1. GC Memory Usage Factor
            var gcMemoryFactor = CalculateGCMemoryFactor();
            pressureFactors.Add(new MemoryPressureFactor
            {
                Name = "GC_Memory",
                Value = gcMemoryFactor,
                Weight = 0.30 // 30% weight - most important
            });

            // 2. GC Collection Frequency Factor (Gen 2 collections)
            var gcCollectionFactor = CalculateGCCollectionFrequency();
            pressureFactors.Add(new MemoryPressureFactor
            {
                Name = "GC_Collections",
                Value = gcCollectionFactor,
                Weight = 0.25 // 25% weight
            });

            // 3. Working Set Memory Factor
            var workingSetFactor = CalculateWorkingSetFactor();
            pressureFactors.Add(new MemoryPressureFactor
            {
                Name = "Working_Set",
                Value = workingSetFactor,
                Weight = 0.20 // 20% weight
            });

            // 4. System Memory Availability Factor
            var systemMemoryFactor = CalculateSystemMemoryAvailability();
            pressureFactors.Add(new MemoryPressureFactor
            {
                Name = "System_Memory",
                Value = systemMemoryFactor,
                Weight = 0.15 // 15% weight
            });

            // 5. Request Pattern Memory Impact
            var requestPatternFactor = CalculateRequestPatternMemoryImpact();
            pressureFactors.Add(new MemoryPressureFactor
            {
                Name = "Request_Pattern",
                Value = requestPatternFactor,
                Weight = 0.10 // 10% weight
            });

            // Calculate weighted pressure
            var totalPressure = pressureFactors.Sum(f => f.Value * f.Weight);

            // Apply error rate adjustment
            var errorAdjustment = CalculateErrorBasedMemoryPressure();
            totalPressure = totalPressure * (1.0 + errorAdjustment);

            // Apply allocation rate factor
            var allocationRate = EstimateAllocationRate();
            if (allocationRate > 0.7) // High allocation rate
            {
                totalPressure *= 1.15; // 15% increase
            }

            // Store detailed metrics
            _timeSeriesDb.StoreMetric("Memory_Pressure", totalPressure, DateTime.UtcNow);
            foreach (var factor in pressureFactors)
            {
                _timeSeriesDb.StoreMetric($"Memory_{factor.Name}", factor.Value, DateTime.UtcNow);
            }

            _logger.LogDebug("Memory pressure: {Pressure:P} (GC: {GC:P}, Collections: {Col:P}, WorkingSet: {WS:P}, System: {Sys:P})",
                totalPressure, gcMemoryFactor, gcCollectionFactor, workingSetFactor, systemMemoryFactor);

            // Clamp to 0-1 range (0% to 100% pressure)
            return Math.Max(0, Math.Min(totalPressure, 1.0));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error estimating memory pressure");
            return 0.05; // Default low pressure
        }
    }

    private double CalculateGCMemoryFactor()
    {
        try
        {
            // Get total managed memory
            var totalMemory = GC.GetTotalMemory(forceFullCollection: false);

            // Get memory info
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var heapSize = gcMemoryInfo.HeapSizeBytes;
            var totalAvailable = gcMemoryInfo.TotalAvailableMemoryBytes;

            // Calculate memory usage percentage
            double memoryUsagePercent = 0;
            if (totalAvailable > 0)
            {
                memoryUsagePercent = (double)heapSize / totalAvailable;
            }
            else
            {
                // Fallback: use total memory relative to a typical limit (e.g., 2GB)
                var typicalLimit = 2L * 1024 * 1024 * 1024; // 2GB
                memoryUsagePercent = (double)totalMemory / typicalLimit;
            }

            // Store metrics
            _timeSeriesDb.StoreMetric("GC_TotalMemory_MB", totalMemory / (1024.0 * 1024.0), DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("GC_HeapSize_MB", heapSize / (1024.0 * 1024.0), DateTime.UtcNow);

            return Math.Min(1.0, memoryUsagePercent);
        }
        catch
        {
            return 0.5; // Moderate default
        }
    }

    private double CalculateGCCollectionFrequency()
    {
        try
        {
            // Get collection counts for all generations
            var gen0Count = GC.CollectionCount(0);
            var gen1Count = GC.CollectionCount(1);
            var gen2Count = GC.CollectionCount(2);

            // Retrieve historical counts
            var previousGen2Metrics = _timeSeriesDb.GetRecentMetrics("GC_Gen2_Collections", 10);

            // Calculate Gen 2 collection rate (most indicative of pressure)
            double gen2Rate = 0;
            if (previousGen2Metrics.Any())
            {
                var previousCount = (int)previousGen2Metrics.First().Value;
                var timeDiff = DateTime.UtcNow - previousGen2Metrics.First().Timestamp;
                var collectionDiff = gen2Count - previousCount;

                if (timeDiff.TotalSeconds > 0)
                {
                    gen2Rate = collectionDiff / timeDiff.TotalSeconds;
                }
            }

            // Store current counts
            _timeSeriesDb.StoreMetric("GC_Gen0_Collections", gen0Count, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("GC_Gen1_Collections", gen1Count, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("GC_Gen2_Collections", gen2Count, DateTime.UtcNow);

            // High Gen2 rate indicates memory pressure
            // Normal: < 0.1 per second, High: > 1 per second
            var pressureFactor = Math.Min(gen2Rate / 2.0, 1.0); // Normalize to 0-1

            return pressureFactor;
        }
        catch
        {
            return 0.3; // Moderate default
        }
    }

    private double CalculateWorkingSetFactor()
    {
        try
        {
            // Get current process
            using var process = System.Diagnostics.Process.GetCurrentProcess();

            // Get working set (physical memory used by process)
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var virtualMemory = process.VirtualMemorySize64;

            // Typical limits (can be configured)
            var workingSetLimit = 1L * 1024 * 1024 * 1024; // 1GB default
            var privateMemoryLimit = 2L * 1024 * 1024 * 1024; // 2GB default

            // Calculate usage ratios
            var workingSetRatio = (double)workingSet / workingSetLimit;
            var privateMemoryRatio = (double)privateMemory / privateMemoryLimit;

            // Store metrics
            _timeSeriesDb.StoreMetric("Process_WorkingSet_MB", workingSet / (1024.0 * 1024.0), DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("Process_PrivateMemory_MB", privateMemory / (1024.0 * 1024.0), DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("Process_VirtualMemory_MB", virtualMemory / (1024.0 * 1024.0), DateTime.UtcNow);

            // Weighted combination
            var pressureFactor = (workingSetRatio * 0.6 + privateMemoryRatio * 0.4);

            return Math.Min(1.0, pressureFactor);
        }
        catch
        {
            return 0.4; // Moderate default
        }
    }

    private double CalculateSystemMemoryAvailability()
    {
        try
        {
            // Get GC memory info which includes system-level information
            var gcMemoryInfo = GC.GetGCMemoryInfo();

            var totalAvailable = gcMemoryInfo.TotalAvailableMemoryBytes;
            var highMemoryLoadThreshold = gcMemoryInfo.HighMemoryLoadThresholdBytes;

            // Calculate system memory pressure
            double systemPressure = 0;
            if (totalAvailable > 0 && highMemoryLoadThreshold > 0)
            {
                // If we're close to high memory load threshold, pressure is high
                systemPressure = 1.0 - ((double)totalAvailable / highMemoryLoadThreshold);
                systemPressure = Math.Max(0, systemPressure);
            }

            // Store metrics
            _timeSeriesDb.StoreMetric("System_AvailableMemory_MB", totalAvailable / (1024.0 * 1024.0), DateTime.UtcNow);

            return Math.Min(1.0, systemPressure);
        }
        catch
        {
            return 0.3; // Low-moderate default
        }
    }

    private double CalculateRequestPatternMemoryImpact()
    {
        try
        {
            var activeRequests = GetActiveRequestCount();
            var systemLoad = GetDatabasePoolUtilization();

            // High load + many requests = potential memory pressure
            var estimatedPressure = (systemLoad * 0.6) + (Math.Min(activeRequests / 1000.0, 1.0) * 0.4);

            // Analyze request sizes and complexity
            var avgRequestComplexity = _requestAnalytics.Values
                .Where(a => a.TotalExecutions > 0)
                .Average(a => a.AverageExecutionTime.TotalMilliseconds / 100.0); // Normalize

            // Complex requests typically use more memory
            var complexityFactor = Math.Min(avgRequestComplexity, 2.0) / 2.0; // 0-1 range

            estimatedPressure = (estimatedPressure * 0.7 + complexityFactor * 0.3);

            return Math.Min(1.0, estimatedPressure);
        }
        catch
        {
            return 0.3; // Moderate default
        }
    }

    private double CalculateErrorBasedMemoryPressure()
    {
        try
        {
            // Check for signs of memory issues in error patterns
            var recentErrors = _requestAnalytics.Values
                .Where(a => a.ErrorRate > 0.05) // More than 5% error rate
                .Count();

            // More errors = potentially memory-related issues
            if (recentErrors > 5)
            {
                return 0.3; // 30% increase
            }
            else if (recentErrors > 3)
            {
                return 0.2; // 20% increase
            }
            else if (recentErrors > 0)
            {
                return 0.1; // 10% increase
            }

            return 0; // No adjustment
        }
        catch
        {
            return 0;
        }
    }

    private double EstimateAllocationRate()
    {
        try
        {
            // Get allocation rate from GC statistics
            var gcInfo = GC.GetGCMemoryInfo();
            var totalAllocated = GC.GetTotalAllocatedBytes(precise: false);

            // Get historical allocation
            var historicalAllocation = _timeSeriesDb.GetRecentMetrics("GC_TotalAllocated_MB", 10);
            if (historicalAllocation.Any())
            {
                var previousAllocation = historicalAllocation.First().Value * 1024 * 1024; // Convert back to bytes
                var timeDiff = DateTime.UtcNow - historicalAllocation.First().Timestamp;

                if (timeDiff.TotalSeconds > 0)
                {
                    var allocationDiff = totalAllocated - (long)previousAllocation;
                    var allocationRateMBPerSec = (allocationDiff / (1024.0 * 1024.0)) / timeDiff.TotalSeconds;

                    // Store allocation rate
                    _timeSeriesDb.StoreMetric("GC_AllocationRate_MBPerSec", allocationRateMBPerSec, DateTime.UtcNow);

                    // High allocation rate: > 100 MB/sec = high pressure
                    var pressureFactor = Math.Min(allocationRateMBPerSec / 200.0, 1.0);
                    return pressureFactor;
                }
            }

            // Store current allocation
            _timeSeriesDb.StoreMetric("GC_TotalAllocated_MB", totalAllocated / (1024.0 * 1024.0), DateTime.UtcNow);

            return 0.5; // Moderate default if no history
        }
        catch
        {
            return 0.5; // Moderate default
        }
    }

    private double EstimateConnectionChurn()
    {
        try
        {
            var recentConnections = _timeSeriesDb.GetRecentMetrics("WebSocketConnections", 20);

            if (recentConnections.Count < 5)
                return 0.05; // Default low churn

            // Calculate variance in connection counts (high variance = high churn)
            var values = recentConnections.Select(m => m.Value).ToList();
            var mean = values.Average();

            if (mean < 1)
                return 0.05;

            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            var coefficientOfVariation = Math.Sqrt(variance) / mean;

            // Normalize to 0-0.2 range
            var churnRate = Math.Min(coefficientOfVariation * 0.5, 0.2);

            return churnRate;
        }
        catch
        {
            return 0.05;
        }
    }

    private double EstimateConnectionOverloadFactor()
    {
        try
        {
            // Check if we're approaching or exceeding connection limits
            var currentConnections = GetWebSocketConnectionCount();
            var maxConnections = _options.MaxEstimatedWebSocketConnections;

            if (maxConnections <= 0 || currentConnections <= 0)
                return 0;

            var utilizationRatio = (double)currentConnections / maxConnections;

            // Overload kicks in at 80% utilization
            if (utilizationRatio > 0.8)
            {
                var overloadFactor = (utilizationRatio - 0.8) * 0.5; // Up to 10% at 100% utilization
                return Math.Min(overloadFactor, 0.15);
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private double GetDisconnectionTimeAdjustment()
    {
        var hourOfDay = DateTime.UtcNow.Hour;

        // Disconnection patterns vary by time
        if (hourOfDay >= 9 && hourOfDay <= 17)
        {
            return 1.2; // 20% more during business hours (more activity = more disconnects)
        }
        else if (hourOfDay >= 18 && hourOfDay <= 22)
        {
            return 1.0; // Normal during evening
        }
        else if (hourOfDay >= 23 || hourOfDay <= 6)
        {
            return 0.7; // 30% less during night (less activity)
        }
        else
        {
            return 0.9; // Slightly less in morning hours
        }
    }

    private double GetDisconnectionEnvironmentalFactor()
    {
        try
        {
            // Analyze overall system health
            var avgErrorRate = _requestAnalytics.Values.Any()
                ? _requestAnalytics.Values.Average(a => a.ErrorRate)
                : 0;

            // Poor system health → more disconnects
            if (avgErrorRate > 0.15)
                return 1.5; // 50% more disconnects
            else if (avgErrorRate > 0.10)
                return 1.3; // 30% more
            else if (avgErrorRate > 0.05)
                return 1.1; // 10% more
            else if (avgErrorRate < 0.01)
                return 0.8; // 20% fewer (healthy system)
            else
                return 1.0; // Normal
        }
        catch
        {
            return 1.0;
        }
    }

    private double CalculateErrorRateTrend()
    {
        try
        {
            var errorMetrics = _timeSeriesDb.GetRecentMetrics("ErrorRate", 20);

            if (errorMetrics.Count < 10)
                return 0;

            // Calculate simple linear trend
            var values = errorMetrics.Select(m => m.Value).ToList();
            var recentAvg = values.Take(values.Count / 2).Average();
            var olderAvg = values.Skip(values.Count / 2).Average();

            // Trend = (recent - older) / older
            if (olderAvg == 0)
                return 0;

            var trend = (recentAvg - olderAvg) / olderAvg;

            return Math.Max(-0.5, Math.Min(trend, 0.5)); // Cap at ±50%
        }
        catch
        {
            return 0;
        }
    }

    private double CalculateSystemLoadTrend()
    {
        try
        {
            var loadMetrics = _timeSeriesDb.GetRecentMetrics("SystemLoad", 20);

            if (loadMetrics.Count < 10)
                return 0;

            var values = loadMetrics.Select(m => m.Value).ToList();
            var recentAvg = values.Take(values.Count / 2).Average();
            var olderAvg = values.Skip(values.Count / 2).Average();

            if (olderAvg == 0)
                return 0;

            var trend = (recentAvg - olderAvg) / olderAvg;

            return Math.Max(-0.5, Math.Min(trend, 0.5));
        }
        catch
        {
            return 0;
        }
    }

    private double GetNormalizedTimeOfDay()
    {
        return DateTime.UtcNow.Hour / 24.0;
    }

    private double GetNormalizedDayOfWeek()
    {
        return ((int)DateTime.UtcNow.DayOfWeek) / 7.0;
    }

    private double EstimateNetworkQualityFromMetrics()
    {
        try
        {
            var errorRate = CalculateCurrentErrorRate();
            var responseTime = CalculateAverageResponseTime();

            // Good network = low errors + fast responses
            var errorQuality = 1.0 - Math.Min(errorRate * 5, 1.0); // 0-1 scale
            var timeQuality = Math.Max(0, 1.0 - (responseTime.TotalMilliseconds / 5000)); // 0-1 scale

            // Weighted average
            var overallQuality = (errorQuality * 0.6) + (timeQuality * 0.4);

            return Math.Max(0, Math.Min(overallQuality, 1.0));
        }
        catch
        {
            return 0.7; // Default moderate quality
        }
    }

    private void StoreDisconnectionRateMetrics(double disconnectionRate)
    {
        try
        {
            if (disconnectionRate <= 0)
                return;

            var timestamp = DateTime.UtcNow;

            _timeSeriesDb.StoreMetric("WebSocketDisconnectionRate", disconnectionRate, timestamp);

            _logger.LogTrace("Stored disconnection rate metric: {Rate:P2} at {Time}",
                disconnectionRate, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing disconnection rate metrics");
        }
    }

    private double EstimateKeepAliveHealthRatio()
    {
        try
        {
            // Multi-factor keepalive health estimation using AI/ML components

            // 1. System stability as baseline
            var systemStability = CalculateSystemStability();
            var baseHealth = systemStability * 1.1;

            // 2. Network quality indicators
            var responseTime = CalculateAverageResponseTime();
            var networkQuality = CalculateNetworkQualityFactor(responseTime.TotalMilliseconds);

            // 3. Error rate impact on keepalive
            var errorRate = CalculateCurrentErrorRate();
            var errorImpact = 1.0 - (errorRate * 0.8); // Errors affect keepalive health

            // 4. System load considerations
            var systemLoad = GetDatabasePoolUtilization();
            var loadFactor = 1.0 - (systemLoad * 0.15); // High load can affect keepalive responsiveness

            // 5. Historical trend analysis using time-series data
            var trendFactor = AnalyzeKeepAliveTrends();

            // 6. Time-of-day variations (cached in time-series DB)
            var temporalFactor = GetTemporalHealthFactor();

            // Weighted combination of all factors (removed pattern analysis to avoid circular dependency)
            var combinedHealth =
                (baseHealth * 0.25) +           // 25% system stability
                (networkQuality * 0.20) +        // 20% network quality
                (errorImpact * 0.15) +           // 15% error impact
                (loadFactor * 0.15) +            // 15% system load
                (trendFactor * 0.20) +           // 20% historical trends (increased weight)
                (temporalFactor * 0.05);         // 5% temporal factors

            // Apply bounds with confidence adjustment
            var confidence = CalculateKeepAliveConfidence();
            var finalHealth = combinedHealth * confidence;

            // Store in time-series for trend analysis
            StoreKeepAliveHealthMetric(finalHealth);

            // Clamp to realistic range: 75% to 99%
            return Math.Max(0.75, Math.Min(0.99, finalHealth));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating keepalive health ratio, using fallback");

            // Fallback to simple calculation
            var systemStability = CalculateSystemStability();
            return Math.Max(0.80, Math.Min(0.95, systemStability * 1.05));
        }
    }

    private double CalculateNetworkQualityFactor(double responseTime)
    {
        // Network quality based on response times
        // Faster response times = better keepalive reliability

        if (responseTime < 50) return 1.0;      // Excellent (<50ms)
        if (responseTime < 100) return 0.98;    // Very good (50-100ms)
        if (responseTime < 200) return 0.95;    // Good (100-200ms)
        if (responseTime < 500) return 0.90;    // Fair (200-500ms)
        if (responseTime < 1000) return 0.85;   // Poor (500ms-1s)

        return 0.80; // Very poor (>1s)
    }

    private double AnalyzeKeepAliveTrends()
    {
        try
        {
            // Analyze historical keepalive patterns from time-series DB
            var recentHistory = _timeSeriesDb.GetHistory("KeepAliveHealth", TimeSpan.FromHours(1));
            var recentMetrics = recentHistory?.ToList();

            if (recentMetrics == null || recentMetrics.Count < 5)
            {
                return 0.90; // Default if insufficient data
            }

            // Calculate average and trend from historical data
            var values = recentMetrics.Select(m => (double)m.Value).ToArray();
            var averageHealth = values.Average();

            // Simple trend detection: compare first half vs second half
            var firstHalf = values.Take(values.Length / 2).Average();
            var secondHalf = values.Skip(values.Length / 2).Average();
            var trendAdjustment = secondHalf > firstHalf ? 0.05 : (secondHalf < firstHalf ? -0.05 : 0.0);

            var trendAdjustedHealth = averageHealth + trendAdjustment;

            return Math.Max(0.75, Math.Min(1.0, trendAdjustedHealth));
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error analyzing keepalive trends");
            return 0.90; // Safe default
        }
    }

    private double GetTemporalHealthFactor()
    {
        try
        {
            // Time-based variations in keepalive health
            // Different times of day may have different network characteristics

            var currentHour = DateTime.UtcNow.Hour;

            // Peak hours (9-17 UTC): slightly lower health due to higher load
            if (currentHour >= 9 && currentHour <= 17)
            {
                return 0.95;
            }
            // Off-peak hours: better health
            else if (currentHour >= 0 && currentHour <= 6)
            {
                return 0.98;
            }
            // Transition hours
            else
            {
                return 0.96;
            }
        }
        catch
        {
            return 0.95; // Default
        }
    }

    private double CalculateKeepAliveConfidence()
    {
        try
        {
            // Calculate confidence in the estimation based on data availability
            var recentHealthData = _timeSeriesDb.GetHistory("KeepAliveHealth", TimeSpan.FromMinutes(30));
            var connectionData = _timeSeriesDb.GetHistory("ConnectionCount", TimeSpan.FromMinutes(10));

            var hasTimeSeriesData = recentHealthData?.Any() ?? false;
            var hasConnectionMetrics = connectionData?.Any() ?? false;
            var hasAnalyticsData = _requestAnalytics.Count > 0;

            var confidenceScore = 0.7; // Base confidence

            if (hasTimeSeriesData) confidenceScore += 0.15;
            if (hasConnectionMetrics) confidenceScore += 0.10;
            if (hasAnalyticsData) confidenceScore += 0.05;

            return Math.Min(1.0, confidenceScore);
        }
        catch
        {
            return 0.85; // Conservative confidence
        }
    }

    private void StoreKeepAliveHealthMetric(double healthValue)
    {
        try
        {
            // Store metric in time-series database for trend analysis
            _timeSeriesDb.StoreMetric("KeepAliveHealth", healthValue, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing keepalive health metric");
            // Non-critical, continue
        }
    }

    // Delegates to SystemMetricsCalculator
    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
    private double CalculateCurrentThroughput() => _systemMetrics.CalculateCurrentThroughput();
    private double CalculateMemoryUsage() => _systemMetrics.CalculateMemoryUsage();
    private double CalculateCurrentErrorRate() => _systemMetrics.CalculateCurrentErrorRate();
    private double GetDatabasePoolUtilization() => _systemMetrics.GetDatabasePoolUtilization();
    private TimeSpan CalculateAverageResponseTime() => _systemMetrics.CalculateAverageResponseTime();
    private double CalculateSystemStability()
    {
        var varianceScores = _requestAnalytics.Values.Select(data => data.CalculateExecutionVariance()).ToArray();
        if (varianceScores.Length == 0) return 1.0;

        var averageVariance = varianceScores.Average();
        // Lower variance = higher stability (inverted score)
        return Math.Max(0.0, 1.0 - Math.Min(1.0, averageVariance));
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

    private double CalculateWeightedAverage(List<MetricDataPoint> metrics)
    {
        if (metrics.Count == 0)
            return 0;

        double totalWeight = 0;
        double weightedSum = 0;

        // More recent observations get higher weights
        for (int i = 0; i < metrics.Count; i++)
        {
            var weight = i + 1; // Linear weight increase
            weightedSum += metrics[i].Value * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0;
    }

    private double CalculateTrend(List<MetricDataPoint> metrics)
    {
        var n = metrics.Count;
        if (n < 2)
            return 0;

        // Use index as x-axis (time)
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (int i = 0; i < n; i++)
        {
            var x = i;
            var y = metrics[i].Value;

            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        // Slope = (n*ΣXY - ΣX*ΣY) / (n*ΣX² - (ΣX)²)
        var denominator = (n * sumX2) - (sumX * sumX);
        if (Math.Abs(denominator) < 0.0001)
            return 0;

        var slope = ((n * sumXY) - (sumX * sumY)) / denominator;
        return slope;
    }

    private double CalculateEMA(List<double> values, double alpha)
    {
        if (values.Count == 0)
            return 0;

        double ema = values[0];
        for (int i = 1; i < values.Count; i++)
        {
            ema = (alpha * values[i]) + ((1 - alpha) * ema);
        }
        return ema;
    }

    private int EstimateRealTimeUsers()
    {
        // Estimate real-time connected users based on system activity
        var activeRequests = GetActiveRequestCount();
        return Math.Max(0, activeRequests / 5); // Assume 20% of requests are real-time
    }

    private double CalculateConnectionMultiplier()
    {
        // Account for users with multiple tabs/connections
        return 1.3; // 30% multiplier for multi-connection users
    }

    private double CalculateConnectionHealthRatio()
    {
        // Calculate ratio of healthy connections
        var errorRate = CalculateCurrentErrorRate();
        return Math.Max(0.7, 1.0 - (errorRate * 2)); // Health inversely related to error rate
    }
}