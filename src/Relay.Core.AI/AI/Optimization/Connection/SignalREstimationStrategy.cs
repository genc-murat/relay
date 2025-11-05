using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class SignalREstimationStrategy(
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

            // Strategy 1: Try to get from stored SignalR metrics (historical data)
            connectionCount = TryGetStoredSignalRMetrics();
            if (connectionCount > 0)
            {
                var maxLimit = _options.MaxEstimatedWebSocketConnections / 2;
                connectionCount = Math.Min(connectionCount, maxLimit);
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
            if (recentMetrics.Count != 0)
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
            if (recentMetrics.Count == 0)
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

    private int EstimateRealTimeUsers()
    {
        // Estimate real-time connected users based on system activity
        var activeRequests = GetActiveRequestCount();
        return Math.Max(0, activeRequests / 5); // Assume 20% of requests are real-time
    }

    private int EstimateActiveHubCount()
    {
        // Estimate number of active SignalR hubs
        // This is a simplified estimation - in real implementation would track actual hubs
        var activeRequests = GetActiveRequestCount();
        return Math.Max(1, Math.Min(5, activeRequests / 20)); // 1-5 hubs based on activity
    }

    private static double CalculateConnectionMultiplier()
    {
        // Account for users with multiple tabs/connections
        return 1.3; // 30% multiplier for multi-connection users
    }

    private double CalculateSignalRGroupFactor()
    {
        // Estimate impact of SignalR groups on connection count
        // Groups can cause multiple connections per user
        return 1.2; // Conservative estimate
    }

    private double CalculateConnectionHealthRatio()
    {
        // Calculate ratio of healthy connections
        var errorRate = CalculateCurrentErrorRate();
        return Math.Max(0.7, 1.0 - (errorRate * 2)); // Health inversely related to error rate
    }

    // Delegates to SystemMetricsCalculator
    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
    private double CalculateCurrentThroughput() => _systemMetrics.CalculateCurrentThroughput();
    private double CalculateMemoryUsage() => _systemMetrics.CalculateMemoryUsage();
    private double CalculateCurrentErrorRate() => _systemMetrics.CalculateCurrentErrorRate();
}
