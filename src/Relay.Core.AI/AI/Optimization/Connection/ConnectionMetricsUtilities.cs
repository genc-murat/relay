using Microsoft.Extensions.Logging;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

public class ConnectionMetricsUtilities
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly Analysis.TimeSeries.TimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ProtocolMetricsCalculator _protocolCalculator;

    public ConnectionMetricsUtilities(
        ILogger logger,
        AIOptimizationOptions options,
        ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
        Analysis.TimeSeries.TimeSeriesDatabase timeSeriesDb,
        SystemMetricsCalculator systemMetrics,
        ProtocolMetricsCalculator protocolCalculator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
        _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
        _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
        _protocolCalculator = protocolCalculator ?? throw new ArgumentNullException(nameof(protocolCalculator));
    }

    public virtual double CalculateEMA(List<double> values, double alpha)
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

    public double CalculateConnectionThroughputFactor()
    {
        var throughput = CalculateCurrentThroughput();
        return Math.Max(1.0, throughput / 10); // Scale factor
    }

    public int EstimateKeepAliveConnections()
    {
        // Estimate persistent HTTP connections based on system characteristics
        var processorCount = Environment.ProcessorCount;
        var baseKeepAlive = processorCount * 2; // Base keep-alive pool

        // Adjust based on current system load
        var systemLoad = GetDatabasePoolUtilization();
        var loadAdjustment = (int)(baseKeepAlive * systemLoad);

        return Math.Min(baseKeepAlive + loadAdjustment, processorCount * 8);
    }

    public int GetOutboundHttpConnectionCount()
    {
        try
        {
            // Track outbound HTTP connections to external services
            var externalApiCallsRate = _requestAnalytics.Values
                .Sum(x => x.ExecutionTimesCount) / Math.Max(1, _requestAnalytics.Count);

            // Estimate active outbound connections
            var outboundConnections = Math.Min(15, Math.Max(1, externalApiCallsRate / 10));

            // Factor in connection reuse and pooling
            var poolingEfficiency = 0.4; // 60% reduction due to connection pooling
            outboundConnections = (int)(outboundConnections * (1 - poolingEfficiency));

            return Math.Max(0, outboundConnections);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating outbound HTTP connections");
            return 0;
        }
    }

    public int GetUpgradedConnectionCount(WebSocketConnectionMetricsProvider? webSocketProvider)
    {
        try
        {
            // Track connections upgraded from HTTP to WebSocket or other protocols
            // In production, would integrate with WebSocket connection manager

            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("Upgraded_ConnectionCount", 30);
            if (storedMetrics.Any())
            {
                var avgCount = (int)storedMetrics.Average(m => m.Value);

                // Apply decay factor - upgraded connections typically transition quickly
                var latestMetric = storedMetrics.Last();
                var timeSinceLastUpdate = DateTime.UtcNow - latestMetric.Timestamp;
                var decayFactor = Math.Max(0.5, 1.0 - (timeSinceLastUpdate.TotalSeconds / 300.0)); // 5-minute decay

                return Math.Max(0, (int)(avgCount * decayFactor));
            }

            var webSocketConnections = GetWebSocketConnectionCount(webSocketProvider);

            // Analyze upgrade patterns from request analytics
            var totalRequests = _requestAnalytics.Values.Sum(x => x.TotalExecutions);
            var activeRequests = GetActiveRequestCount();

            // Estimate upgrade rate based on WebSocket presence
            double upgradeRate = 0.05; // Default: 5% of connections upgrade

            if (webSocketConnections > 0)
            {
                // If we have active WebSocket connections, calculate upgrade rate
                if (totalRequests > 0)
                {
                    upgradeRate = Math.Min(0.2, (double)webSocketConnections / Math.Max(1, activeRequests));
                }
            }

            // Calculate connections currently in upgrade transition
            // Upgrades are typically short-lived (1-5 seconds)
            var avgResponseTime = _requestAnalytics.Values
                .Where(x => x.TotalExecutions > 0)
                .Select(x => x.AverageExecutionTime.TotalMilliseconds)
                .DefaultIfEmpty(100)
                .Average();

            // Upgrade window: typically 2-5x the average response time
            var upgradeWindowMultiplier = 3.0;
            var upgradeWindowSeconds = (avgResponseTime * upgradeWindowMultiplier) / 1000.0;

            // Calculate connections in upgrade state
            var throughputPerSecond = CalculateCurrentThroughput();
            var connectionsInUpgrade = (int)(throughputPerSecond * upgradeRate * Math.Min(upgradeWindowSeconds, 10));

            // Add recently upgraded WebSocket connections (still counted as HTTP)
            // Only count connections upgraded in last 30 seconds
            var recentUpgrades = (int)(webSocketConnections * 0.1); // 10% are recent upgrades

            var totalUpgradedConnections = connectionsInUpgrade + recentUpgrades;

            // Consider protocol distribution
            var protocolFactor = _protocolCalculator.CalculateProtocolMultiplexingFactor();
            if (protocolFactor < 0.5) // Lots of HTTP/2+ = more upgrade potential
            {
                totalUpgradedConnections = (int)(totalUpgradedConnections * 1.5);
            }

            // Store metric for future reference
            _timeSeriesDb.StoreMetric("Upgraded_ConnectionCount", totalUpgradedConnections, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("Upgrade_Rate", upgradeRate, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("WebSocket_ConnectionCount", webSocketConnections, DateTime.UtcNow);

            // Cap at reasonable maximum
            return Math.Max(0, Math.Min(totalUpgradedConnections, 50));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error estimating upgraded connections");
            return 0;
        }
    }

    public int GetFallbackHttpConnectionCount()
    {
        try
        {
            // Conservative fallback based on system characteristics
            var processorCount = Environment.ProcessorCount;
            var activeRequests = GetActiveRequestCount();

            // Base estimate: 2 connections per processor + active requests
            var fallbackEstimate = (processorCount * 2) + Math.Min(activeRequests, processorCount * 4);

            // Apply conservative multiplier for keep-alive and pooling
            fallbackEstimate = (int)(fallbackEstimate * 1.3);

            return Math.Min(fallbackEstimate, 100); // Reasonable upper bound
        }
        catch
        {
            // Ultimate fallback
            return Environment.ProcessorCount * 3;
        }
    }

    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
    private double CalculateCurrentThroughput() => _systemMetrics.CalculateCurrentThroughput();
    private double GetDatabasePoolUtilization() => _systemMetrics.GetDatabasePoolUtilization();

    private int GetWebSocketConnectionCount(WebSocketConnectionMetricsProvider? webSocketProvider)
    {
        // Use the injected WebSocket provider if available
        return webSocketProvider?.GetWebSocketConnectionCount() ?? 0;
    }
}