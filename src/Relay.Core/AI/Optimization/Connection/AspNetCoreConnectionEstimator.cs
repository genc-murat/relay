using Microsoft.Extensions.Logging;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class AspNetCoreConnectionEstimator(
    ILogger logger,
    AIOptimizationOptions options,
    ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
    Analysis.TimeSeries.TimeSeriesDatabase timeSeriesDb,
    SystemMetricsCalculator systemMetrics,
    ProtocolMetricsCalculator protocolCalculator,
    ConnectionMetricsUtilities utilities)
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AIOptimizationOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
    private readonly Analysis.TimeSeries.TimeSeriesDatabase _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
    private readonly SystemMetricsCalculator _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
    private readonly ProtocolMetricsCalculator _protocolCalculator = protocolCalculator ?? throw new ArgumentNullException(nameof(protocolCalculator));
    private readonly ConnectionMetricsUtilities _utilities = utilities ?? throw new ArgumentNullException(nameof(utilities));

    public int GetAspNetCoreConnectionCount()
    {
        try
        {
            var connectionCount = 0;

            // 1. Try to get actual Kestrel metrics if available
            var kestrelConnections = GetKestrelServerConnections();
            if (kestrelConnections > 0)
            {
                _logger.LogTrace("Kestrel actual connections: {Count}", kestrelConnections);
                var boundedKestrelConnections = Math.Max(1, Math.Min(kestrelConnections, _options.MaxEstimatedHttpConnections / 2));
                return boundedKestrelConnections;
            }

            // 2. Fallback: Estimate from request analytics
            var activeRequests = GetActiveRequestCount();
            var estimatedInboundConnections = Math.Max(1, activeRequests);

            // 3. Apply HTTP protocol multiplexing factors
            var protocolFactor = _protocolCalculator.CalculateProtocolMultiplexingFactor();
            estimatedInboundConnections = (int)(estimatedInboundConnections * protocolFactor);

            // 4. Factor in persistent connections (keep-alive)
            var keepAliveFactor = _protocolCalculator.CalculateKeepAliveConnectionFactor();
            estimatedInboundConnections = (int)(estimatedInboundConnections * keepAliveFactor);

            // 5. Apply load-based adjustment
            var loadLevel = ClassifyCurrentLoadLevel();
            var loadAdjustment = GetLoadBasedConnectionAdjustment(loadLevel);
            estimatedInboundConnections = (int)(estimatedInboundConnections * loadAdjustment);

            // 6. Historical average smoothing
            var historicalAvg = GetHistoricalConnectionAverage("AspNetCore");
            if (historicalAvg > 0)
            {
                // Weighted average: 70% current, 30% historical
                connectionCount = (int)((estimatedInboundConnections * 0.7) + (historicalAvg * 0.3));
            }
            else
            {
                connectionCount = estimatedInboundConnections;
            }

            // 7. Apply reasonable bounds
            var finalCount = Math.Max(1, Math.Min(connectionCount, _options.MaxEstimatedHttpConnections / 2));

            _logger.LogDebug("ASP.NET Core connection estimate: Active={Active}, Protocol={Protocol:F2}, KeepAlive={KeepAlive:F2}, Load={Load:F2}, Final={Final}",
                activeRequests, protocolFactor, keepAliveFactor, loadAdjustment, finalCount);

            return finalCount;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating ASP.NET Core connections");
            return Environment.ProcessorCount * 2; // Safe fallback
        }
    }

    public int GetKestrelServerConnections()
    {
        try
        {
            var connectionCount = 0;

            // Strategy 1: Try stored metrics from time-series DB (EventCounters would populate this)
            connectionCount = TryGetStoredKestrelMetrics();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Kestrel connections from stored metrics: {Count}", connectionCount);
                StoreKestrelConnectionMetrics(connectionCount);
                return connectionCount;
            }

            // Strategy 2: Try to infer from request analytics patterns
            connectionCount = InferConnectionsFromRequestPatterns();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Kestrel connections inferred from patterns: {Count}", connectionCount);
                StoreKestrelConnectionMetrics(connectionCount);
                return connectionCount;
            }

            // Strategy 3: Try to estimate from connection metrics collector
            connectionCount = EstimateFromConnectionMetrics();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Kestrel connections from metrics collector: {Count}", connectionCount);
                StoreKestrelConnectionMetrics(connectionCount);
                return connectionCount;
            }

            // Strategy 4: Predict based on historical patterns and current load
            connectionCount = PredictConnectionCount();
            if (connectionCount > 0)
            {
                _logger.LogTrace("Kestrel connections predicted: {Count}", connectionCount);
                StoreKestrelConnectionMetrics(connectionCount);
                return connectionCount;
            }

            _logger.LogDebug("No Kestrel connection data available from any strategy");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving Kestrel server connections");
            return 0;
        }
    }

    private int TryGetStoredKestrelMetrics()
    {
        try
        {
            // Check multiple metric names that might contain connection data
            var metricNames = new[]
            {
                "KestrelConnections",
                "kestrel-current-connections",
                "current-connections",
                "ConnectionCount_AspNetCore"
            };

            foreach (var metricName in metricNames)
            {
                var recentMetrics = _timeSeriesDb.GetRecentMetrics(metricName, 10);
                if (recentMetrics.Count != 0)
                {
                    // Use weighted average of recent values for stability
                    var weights = Enumerable.Range(1, recentMetrics.Count).Select(i => (double)i).ToArray();
                    var weightedSum = recentMetrics.Select((m, i) => m.Value * weights[i]).Sum();
                    var totalWeight = weights.Sum();

                    var weightedAvg = (int)(weightedSum / totalWeight);
                    return Math.Max(0, weightedAvg);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error reading stored Kestrel metrics");
            return 0;
        }
    }

    private int InferConnectionsFromRequestPatterns()
    {
        try
        {
            if (_requestAnalytics.IsEmpty)
                return 0;

            // Analyze concurrent execution patterns
            var concurrentPeaks = _requestAnalytics.Values
                .Select(a => a.ConcurrentExecutionPeaks)
                .ToList();

            if (concurrentPeaks.Count == 0 || concurrentPeaks.All(p => p == 0))
                return 0;

            // Use 90th percentile of concurrent execution as estimate
            var sortedPeaks = concurrentPeaks.OrderBy(p => p).ToList();
            var p90Index = (int)(sortedPeaks.Count * 0.9);
            var p90Value = sortedPeaks[Math.Min(p90Index, sortedPeaks.Count - 1)];

            // Connection count typically 1.2-1.5x concurrent execution due to keep-alive
            var estimatedConnections = (int)(p90Value * 1.3);

            _logger.LogDebug("Inferred connections from request patterns: P90={P90}, Estimated={Est}",
                p90Value, estimatedConnections);

            return estimatedConnections;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error inferring connections from request patterns");
            return 0;
        }
    }

    private int EstimateFromConnectionMetrics()
    {
        try
        {
            // Try to estimate from request analytics aggregates
            var totalActiveRequests = _requestAnalytics.Values.Sum(a => a.ConcurrentExecutionPeaks);

            if (totalActiveRequests > 0)
            {
                // Estimate connections as ~1.2x active requests
                return (int)(totalActiveRequests * 1.2);
            }

            // Check if we have any connection-related metrics in time-series
            var connectionMetrics = _timeSeriesDb.GetRecentMetrics("ConnectionMetrics", 5);
            if (connectionMetrics.Count != 0)
            {
                return (int)connectionMetrics.Last().Value;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error estimating from connection metrics");
            return 0;
        }
    }

    private int PredictConnectionCount()
    {
        try
        {
            // Get current system state
            var currentTime = DateTime.UtcNow;
            var hourOfDay = currentTime.Hour;
            var dayOfWeek = (int)currentTime.DayOfWeek;

            // Get historical connection data
            var historicalData = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 100);

            if (historicalData.Count < 20)
                return 0; // Not enough data for prediction

            // Find similar time periods (same hour of day ±1 hour)
            var similarTimeData = historicalData
                .Where(m => Math.Abs(m.Timestamp.Hour - hourOfDay) <= 1)
                .ToList();

            if (similarTimeData.Count != 0)
            {
                // Use median of similar time periods
                var sortedValues = similarTimeData.Select(m => m.Value).OrderBy(v => v).ToList();
                var median = sortedValues[sortedValues.Count / 2];

                // Apply load adjustment
                var loadLevel = ClassifyCurrentLoadLevel();
                var loadFactor = GetLoadBasedConnectionAdjustment(loadLevel);

                var predicted = (int)(median * loadFactor);

                _logger.LogDebug("Predicted connections: Historical median={Median}, Load factor={Factor}, Predicted={Pred}",
                    median, loadFactor, predicted);

                return Math.Max(1, predicted);
            }

            // Fallback: Use exponential moving average of all historical data
            var ema = _utilities.CalculateEMA([.. historicalData.Select(m => (double)m.Value)], alpha: 0.3);
            return Math.Max(1, (int)ema);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error predicting connection count");
            return 0;
        }
    }

    private void StoreKestrelConnectionMetrics(int connectionCount)
    {
        try
        {
            if (connectionCount <= 0)
                return;

            var timestamp = DateTime.UtcNow;

            // Store in time-series database
            _timeSeriesDb.StoreMetric("KestrelConnections", connectionCount, timestamp);

            // Also store as component-specific metric
            _timeSeriesDb.StoreMetric("ConnectionCount_AspNetCore", connectionCount, timestamp);

            _logger.LogTrace("Stored Kestrel connection metric: {Count} at {Time}",
                connectionCount, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error storing Kestrel connection metrics");
        }
    }



    private LoadLevel ClassifyCurrentLoadLevel()
    {
        // This will be moved to utilities or kept here
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

    private double GetHistoricalConnectionAverage(string component)
    {
        try
        {
            var metricName = $"ConnectionCount_{component}";
            var metrics = _timeSeriesDb.GetRecentMetrics(metricName, 50);

            if (metrics.Count >= 5)
            {
                // Use exponential moving average for recent trend
                var ema = _utilities.CalculateEMA([.. metrics.Select(m => (double)m.Value)], alpha: 0.3);
                return ema;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
}