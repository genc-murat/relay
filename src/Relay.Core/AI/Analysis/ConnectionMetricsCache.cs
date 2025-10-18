using System;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;

namespace Relay.Core.AI
{
    /// <summary>
    /// Manages connection metrics caching with time-series database integration
    /// </summary>
    internal sealed class ConnectionMetricsCache
    {
        private readonly ILogger<ConnectionMetricsCache> _logger;
        private readonly TimeSeriesDatabase _timeSeriesDb;

        public ConnectionMetricsCache(
            ILogger<ConnectionMetricsCache> logger,
            TimeSeriesDatabase timeSeriesDb)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
        }

        /// <summary>
        /// Cache connection metric with rolling window statistics using TimeSeriesDB
        /// </summary>
        public void CacheConnectionMetricWithRollingWindow(string windowKey, int connectionCount)
        {
            try
            {
                var timestamp = DateTime.UtcNow;

                // Store in time-series database
                _timeSeriesDb.StoreMetric("ConnectionCount", connectionCount, timestamp);
                _timeSeriesDb.StoreMetric($"ConnectionCount_{windowKey}", connectionCount, timestamp);

                // Calculate rolling statistics from TimeSeriesDB
                var rollingWindow = TimeSpan.FromMinutes(10);
                var stats = _timeSeriesDb.GetStatistics("ConnectionCount", rollingWindow);

                if (stats != null)
                {
                    _logger.LogTrace("Rolling window cache updated: {Key}, Count: {Count}, " +
                        "Avg: {Avg:F2}, StdDev: {StdDev:F2}",
                        windowKey, connectionCount, stats.Mean, stats.StdDev);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error in CacheConnectionMetricWithRollingWindow");
            }
        }

        /// <summary>
        /// Store connection trend data using TimeSeriesDB with technical indicators
        /// </summary>
        public void StoreConnectionTrendData(int connectionCount, DateTime timestamp)
        {
            try
            {
                // Store raw count in time-series database
                _timeSeriesDb.StoreMetric("ConnectionCount", connectionCount, timestamp);

                // Calculate and store moving averages using TimeSeriesDB statistics
                var stats5 = _timeSeriesDb.GetStatistics("ConnectionCount", TimeSpan.FromMinutes(5));
                var stats15 = _timeSeriesDb.GetStatistics("ConnectionCount", TimeSpan.FromMinutes(15));
                var stats60 = _timeSeriesDb.GetStatistics("ConnectionCount", TimeSpan.FromHours(1));

                var ma5 = stats5?.Mean ?? connectionCount;
                var ma15 = stats15?.Mean ?? connectionCount;
                var ma60 = stats60?.Mean ?? connectionCount;

                _timeSeriesDb.StoreMetric("ConnectionCount_MA5", ma5, timestamp);
                _timeSeriesDb.StoreMetric("ConnectionCount_MA15", ma15, timestamp);
                _timeSeriesDb.StoreMetric("ConnectionCount_MA60", ma60, timestamp);

                // Calculate and store trend direction
                var trend = CalculateConnectionTrend(connectionCount, ma5, ma15);
                _timeSeriesDb.StoreMetric("ConnectionCount_Trend", 
                    trend == "increasing" ? 1.0 : trend == "decreasing" ? -1.0 : 0.0, timestamp);

                // Calculate and store volatility
                var volatility = stats60 != null ? stats60.StdDev / Math.Max(1, stats60.Mean) : 0.0;
                _timeSeriesDb.StoreMetric("ConnectionCount_Volatility", volatility, timestamp);

                _logger.LogTrace("Trend data stored: Count={Count}, MA5={MA5:F1}, MA15={MA15:F1}, " +
                    "Trend={Trend}, Volatility={Volatility:F3}",
                    connectionCount, ma5, ma15, trend, volatility);

                // Detect anomalies using stored data
                DetectConnectionAnomalies(connectionCount, timestamp, stats60);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error storing connection trend data");
            }
        }

        /// <summary>
        /// Store connection breakdown history in TimeSeriesDB
        /// </summary>
        public void StoreConnectionBreakdownHistory(ConnectionBreakdown breakdown)
        {
            try
            {
                var timestamp = breakdown.Timestamp;

                // Store each connection type in time-series database
                _timeSeriesDb.StoreMetric("Connection_HTTP", breakdown.HttpConnections, timestamp);
                _timeSeriesDb.StoreMetric("Connection_Database", breakdown.DatabaseConnections, timestamp);
                _timeSeriesDb.StoreMetric("Connection_External", breakdown.ExternalServiceConnections, timestamp);
                _timeSeriesDb.StoreMetric("Connection_WebSocket", breakdown.WebSocketConnections, timestamp);
                _timeSeriesDb.StoreMetric("Connection_ActiveRequests", breakdown.ActiveRequestConnections, timestamp);
                _timeSeriesDb.StoreMetric("Connection_ThreadPool", breakdown.ThreadPoolUtilization, timestamp);
                _timeSeriesDb.StoreMetric("Connection_DatabasePool", breakdown.DatabasePoolUtilization, timestamp);

                // Calculate and store ratios
                if (breakdown.TotalConnections > 0)
                {
                    var httpRatio = (double)breakdown.HttpConnections / breakdown.TotalConnections;
                    var dbRatio = (double)breakdown.DatabaseConnections / breakdown.TotalConnections;
                    var wsRatio = (double)breakdown.WebSocketConnections / breakdown.TotalConnections;

                    _timeSeriesDb.StoreMetric("ConnectionRatio_HTTP", httpRatio, timestamp);
                    _timeSeriesDb.StoreMetric("ConnectionRatio_Database", dbRatio, timestamp);
                    _timeSeriesDb.StoreMetric("ConnectionRatio_WebSocket", wsRatio, timestamp);

                    _logger.LogTrace("Connection ratios stored - HTTP: {HttpRatio:P}, DB: {DbRatio:P}, WS: {WsRatio:P}",
                        httpRatio, dbRatio, wsRatio);

                    // Detect unusual ratios
                    if (httpRatio > 0.8)
                    {
                        _logger.LogDebug("High HTTP connection ratio detected: {Ratio:P}", httpRatio);
                    }

                    if (dbRatio > 0.5)
                    {
                        _logger.LogWarning("High database connection ratio detected: {Ratio:P} - possible connection leak",
                            dbRatio);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error storing connection breakdown history");
            }
        }

        /// <summary>
        /// Detect connection anomalies using multiple algorithms with TimeSeriesDB data
        /// </summary>
        public void DetectConnectionAnomalies(int currentCount, DateTime timestamp, MetricStatistics? stats)
        {
            try
            {
                var anomalies = new System.Collections.Generic.List<string>();

                // If no stats provided, get from TimeSeriesDB
                if (stats == null)
                {
                    stats = _timeSeriesDb.GetStatistics("ConnectionCount", TimeSpan.FromHours(1));
                }

                if (stats == null || stats.Count == 0)
                {
                    _logger.LogTrace("Insufficient data for anomaly detection");
                    return;
                }

                // 1. Z-Score based detection
                if (stats.Mean > 0)
                {
                    var zScore = (currentCount - stats.Mean) / Math.Max(1, stats.StdDev);
                    if (Math.Abs(zScore) > 3.0) // 3-sigma rule
                    {
                        anomalies.Add($"Z-Score anomaly detected: {zScore:F2} sigma deviation");
                        _timeSeriesDb.StoreMetric("Connection_Anomaly_ZScore", Math.Abs(zScore), timestamp);
                    }
                }

                // 2. Sudden spike detection
                var recentAvg = _timeSeriesDb.GetStatistics("ConnectionCount", TimeSpan.FromMinutes(5))?.Mean ?? 0;
                if (recentAvg > 0 && currentCount > recentAvg * 2.0)
                {
                    anomalies.Add($"Sudden spike detected: {currentCount} vs avg {recentAvg:F0}");
                    _timeSeriesDb.StoreMetric("Connection_Anomaly_Spike", currentCount / recentAvg, timestamp);
                }

                // 3. Sudden drop detection
                if (recentAvg > 0 && currentCount < recentAvg * 0.5)
                {
                    anomalies.Add($"Sudden drop detected: {currentCount} vs avg {recentAvg:F0}");
                    _timeSeriesDb.StoreMetric("Connection_Anomaly_Drop", recentAvg / Math.Max(1, currentCount), timestamp);
                }

                // 4. High volatility detection
                var volatility = stats.StdDev / Math.Max(1, stats.Mean);
                if (volatility > 0.5)
                {
                    anomalies.Add($"High volatility detected: {volatility:F2}");
                    _timeSeriesDb.StoreMetric("Connection_Anomaly_Volatility", volatility, timestamp);
                }

                // Log anomalies
                if (anomalies.Count > 0)
                {
                    _logger.LogWarning("Connection anomalies detected at {Timestamp}: {Anomalies}",
                        timestamp, string.Join("; ", anomalies));

                    // Store anomaly count
                    _timeSeriesDb.StoreMetric("Connection_AnomalyCount", anomalies.Count, timestamp);
                }
                else
                {
                    _timeSeriesDb.StoreMetric("Connection_AnomalyCount", 0, timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error detecting connection anomalies");
            }
        }

        /// <summary>
        /// Calculate connection trend from time-series data
        /// </summary>
        private string CalculateConnectionTrend(double current, double ma5, double ma15)
        {
            if (ma15 == 0) return "stable";

            var percentDiff5 = (current - ma5) / ma5;
            var percentDiff15 = (current - ma15) / ma15;

            // Strong trend if both short and medium term agree
            if (percentDiff5 > 0.1 && percentDiff15 > 0.1) return "strongly_increasing";
            if (percentDiff5 < -0.1 && percentDiff15 < -0.1) return "strongly_decreasing";
            
            // Moderate trend
            if (percentDiff5 > 0.05) return "increasing";
            if (percentDiff5 < -0.05) return "decreasing";
            
            return "stable";
        }
    }
}
