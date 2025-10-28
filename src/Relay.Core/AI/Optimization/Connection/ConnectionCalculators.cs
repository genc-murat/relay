using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class ConnectionCalculators(
    ILogger logger,
    Relay.Core.AI.AIOptimizationOptions options,
    ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
    TimeSeriesDatabase timeSeriesDb,
    Relay.Core.AI.SystemMetricsCalculator systemMetrics,
    ConnectionMetricsUtilities utilities)
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Relay.Core.AI.AIOptimizationOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
    private readonly TimeSeriesDatabase _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
    private readonly Relay.Core.AI.SystemMetricsCalculator _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
    private readonly ConnectionMetricsUtilities _utilities = utilities ?? throw new ArgumentNullException(nameof(utilities));

    public double CalculateMetricVolatility(List<MetricDataPoint> metrics)
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

    public double CalculateWeightedAverage(List<MetricDataPoint> metrics)
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

    public double CalculateTrend(List<MetricDataPoint> metrics)
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

    public int EstimateRealTimeUsers()
    {
        // Estimate real-time connected users based on system activity
        var activeRequests = GetActiveRequestCount();
        return Math.Max(0, activeRequests / 5); // Assume 20% of requests are real-time
    }

    public int EstimateActiveHubCount()
    {
        // Estimate number of active SignalR hubs
        // This is a simplified estimation - in real implementation would track actual hubs
        var activeRequests = GetActiveRequestCount();
        return Math.Max(1, Math.Min(5, activeRequests / 20)); // 1-5 hubs based on activity
    }

    public static double CalculateConnectionMultiplier()
    {
        // Account for users with multiple tabs/connections
        return 1.3; // 30% multiplier for multi-connection users
    }

    public double CalculateSignalRGroupFactor()
    {
        // Estimate impact of SignalR groups on connection count
        // Groups can cause multiple connections per user
        return 1.2; // Conservative estimate
    }

    public double CalculateConnectionHealthRatio()
    {
        // Calculate ratio of healthy connections
        var errorRate = CalculateCurrentErrorRate();
        return Math.Max(0.7, 1.0 - (errorRate * 2)); // Health inversely related to error rate
    }

    public LoadLevel ClassifyCurrentLoadLevel()
    {
        try
        {
            // If there are no analytics, return safe default
            if (_requestAnalytics.IsEmpty)
                return LoadLevel.Medium;

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

    public double GetLoadBasedConnectionAdjustment(LoadLevel level)
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

    public double CalculateTimeOfDayWebSocketFactor(int hourOfDay)
    {
        // WebSocket usage patterns by time of day
        if (hourOfDay >= 9 && hourOfDay <= 17)
            return 1.4; // Business hours - higher usage
        else if (hourOfDay >= 18 && hourOfDay <= 22)
            return 1.1; // Evening - moderate usage
        else if (hourOfDay >= 23 || hourOfDay <= 5)
            return 0.6; // Night - lower usage
        else
            return 0.9; // Early morning - low usage
    }

    public double CalculateSystemStability()
    {
        var varianceScores = _requestAnalytics.Values.Select(data => data.CalculateExecutionVariance()).ToArray();
        if (varianceScores.Length == 0) return 1.0;

        var averageVariance = varianceScores.Average();
        // Lower variance = higher stability (inverted score)
        return Math.Max(0.0, 1.0 - Math.Min(1.0, averageVariance));
    }

    // Delegates to SystemMetricsCalculator
    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
    private double CalculateCurrentThroughput() => _systemMetrics.CalculateCurrentThroughput();
    private double CalculateMemoryUsage() => _systemMetrics.CalculateMemoryUsage();
    private double CalculateCurrentErrorRate() => _systemMetrics.CalculateCurrentErrorRate();
    private double GetDatabasePoolUtilization() => _systemMetrics.GetDatabasePoolUtilization();
    private TimeSpan CalculateAverageResponseTime() => _systemMetrics.CalculateAverageResponseTime();
}


