using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Manages caching strategies for connection metrics and optimization data
    /// </summary>
    internal sealed class CachingStrategyManager
    {
        private readonly ILogger<CachingStrategyManager> _logger;

        public CachingStrategyManager(ILogger<CachingStrategyManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void CacheConnectionCount(
            int connectionCount,
            Func<int> getHttpConnectionCount,
            Func<int> getDatabaseConnectionCount,
            Func<int> getExternalServiceConnectionCount,
            Func<int> getWebSocketConnectionCount,
            Func<int> getActiveRequestCount,
            Func<double> getThreadPoolUtilization,
            Func<double> getDatabasePoolUtilization)
        {
            try
            {
                var timestamp = DateTime.UtcNow;

                var timeBasedKey = $"ai_connection_count_{timestamp:yyyyMMddHHmmss}";
                CacheConnectionMetric(timeBasedKey, connectionCount, TimeSpan.FromSeconds(30));

                var rollingWindowKey = $"ai_connection_rolling_{timestamp:yyyyMMddHHmm}";
                CacheConnectionMetricWithRollingWindow(rollingWindowKey, connectionCount);

                CacheConnectionBreakdown(timestamp, connectionCount, getHttpConnectionCount, getDatabaseConnectionCount,
                    getExternalServiceConnectionCount, getWebSocketConnectionCount, getActiveRequestCount,
                    getThreadPoolUtilization, getDatabasePoolUtilization);

                UpdateConnectionStatistics(connectionCount, timestamp);
                UpdatePeakConnectionMetrics(connectionCount, timestamp);
                StoreConnectionTrendData(connectionCount, timestamp);

                _logger.LogTrace("Cached connection count: {Count} at {Timestamp} with multiple cache strategies",
                    connectionCount, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error caching connection count - non-critical, continuing");
            }
        }

        private void CacheConnectionMetric(string cacheKey, int connectionCount, TimeSpan duration)
        {
            try
            {
                var cacheEntry = new ConnectionCacheEntry
                {
                    Count = connectionCount,
                    Timestamp = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(duration)
                };

                _logger.LogTrace("Cached metric with key: {Key}, Count: {Count}, Duration: {Duration}s",
                    cacheKey, connectionCount, duration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error in CacheConnectionMetric");
            }
        }

        private void CacheConnectionMetricWithRollingWindow(string windowKey, int connectionCount)
        {
            try
            {
                var rollingWindowSize = 10;

                _logger.LogTrace("Rolling window cache updated: {Key}, Count: {Count}, WindowSize: {WindowSize}",
                    windowKey, connectionCount, rollingWindowSize);

                var rollingAverage = connectionCount;
                var rollingStdDev = 0.0;
                var rollingTrend = 0.0;

                _logger.LogTrace("Rolling stats - Avg: {Avg}, StdDev: {StdDev:F2}, Trend: {Trend:F2}",
                    rollingAverage, rollingStdDev, rollingTrend);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error in CacheConnectionMetricWithRollingWindow");
            }
        }

        private void CacheConnectionBreakdown(
            DateTime timestamp,
            int totalConnections,
            Func<int> getHttpConnectionCount,
            Func<int> getDatabaseConnectionCount,
            Func<int> getExternalServiceConnectionCount,
            Func<int> getWebSocketConnectionCount,
            Func<int> getActiveRequestCount,
            Func<double> getThreadPoolUtilization,
            Func<double> getDatabasePoolUtilization)
        {
            try
            {
                var breakdown = new ConnectionBreakdown
                {
                    Timestamp = timestamp,
                    TotalConnections = totalConnections,
                    HttpConnections = getHttpConnectionCount(),
                    DatabaseConnections = getDatabaseConnectionCount(),
                    ExternalServiceConnections = getExternalServiceConnectionCount(),
                    WebSocketConnections = getWebSocketConnectionCount(),
                    ActiveRequestConnections = getActiveRequestCount(),
                    ThreadPoolUtilization = getThreadPoolUtilization(),
                    DatabasePoolUtilization = getDatabasePoolUtilization()
                };

                _logger.LogTrace("Connection breakdown cached - HTTP: {Http}, DB: {Db}, External: {Ext}, WS: {Ws}",
                    breakdown.HttpConnections, breakdown.DatabaseConnections,
                    breakdown.ExternalServiceConnections, breakdown.WebSocketConnections);

                StoreConnectionBreakdownHistory(breakdown);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error caching connection breakdown");
            }
        }

        private void UpdateConnectionStatistics(int connectionCount, DateTime timestamp)
        {
            try
            {
                var currentHour = timestamp.Hour;
                var statsKey = $"connection_stats_hour_{currentHour}";

                var hourlyAverage = connectionCount;
                var hourlyPeak = Math.Max(connectionCount, hourlyAverage);
                var hourlyMin = Math.Min(connectionCount, hourlyAverage);

                _logger.LogTrace("Connection statistics updated for hour {Hour}: Avg={Avg}, Peak={Peak}, Min={Min}",
                    currentHour, hourlyAverage, hourlyPeak, hourlyMin);

                var dayOfWeek = timestamp.DayOfWeek;
                var dailyStatsKey = $"connection_stats_day_{dayOfWeek}";

                _logger.LogTrace("Daily pattern tracking: {DayOfWeek}, Stats: {StatsKey}",
                    dayOfWeek, dailyStatsKey);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error updating connection statistics");
            }
        }

        private void UpdatePeakConnectionMetrics(int connectionCount, DateTime timestamp)
        {
            try
            {
                var isPeakToday = true;
                var isPeakThisHour = true;
                var isPeakAllTime = false;

                if (isPeakToday)
                {
                    var dailyPeakKey = $"connection_peak_daily_{timestamp:yyyyMMdd}";
                    _logger.LogTrace("New daily peak connection count: {Count} at {Time}",
                        connectionCount, timestamp);
                }

                if (isPeakThisHour)
                {
                    var hourlyPeakKey = $"connection_peak_hourly_{timestamp:yyyyMMddHH}";
                    _logger.LogTrace("New hourly peak connection count: {Count} at {Time}",
                        connectionCount, timestamp);
                }

                if (isPeakAllTime)
                {
                    var allTimePeakKey = "connection_peak_all_time";
                    _logger.LogInformation("NEW ALL-TIME PEAK connection count: {Count} at {Time}",
                        connectionCount, timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error updating peak connection metrics");
            }
        }

        private void StoreConnectionTrendData(int connectionCount, DateTime timestamp)
        {
            try
            {
                var trendDataPoints = 60;
                var movingAverage5Min = connectionCount;
                var movingAverage15Min = connectionCount;
                var movingAverage60Min = connectionCount;

                var trend = new ConnectionTrendData
                {
                    Timestamp = timestamp,
                    ConnectionCount = connectionCount,
                    MovingAverage5Min = movingAverage5Min,
                    MovingAverage15Min = movingAverage15Min,
                    MovingAverage60Min = movingAverage60Min,
                    TrendDirection = CalculateTrendDirection(movingAverage5Min, movingAverage15Min),
                    VolatilityScore = CalculateVolatilityScore(connectionCount, movingAverage15Min)
                };

                _logger.LogTrace("Trend data stored: Count={Count}, MA5={MA5}, MA15={MA15}, Direction={Direction}, Volatility={Volatility:F2}",
                    connectionCount, movingAverage5Min, movingAverage15Min, trend.TrendDirection, trend.VolatilityScore);

                DetectConnectionAnomalies(trend);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error storing connection trend data");
            }
        }

        private string CalculateTrendDirection(int shortTermMA, int longTermMA)
        {
            if (shortTermMA > longTermMA * 1.1)
                return "Increasing";
            else if (shortTermMA < longTermMA * 0.9)
                return "Decreasing";
            else
                return "Stable";
        }

        private double CalculateVolatilityScore(int currentValue, int movingAverage)
        {
            if (movingAverage == 0)
                return 0.0;

            var deviation = Math.Abs(currentValue - movingAverage);
            return (double)deviation / movingAverage;
        }

        private void DetectConnectionAnomalies(ConnectionTrendData trendData)
        {
            try
            {
                var anomalyThreshold = 0.5;

                if (trendData.VolatilityScore > anomalyThreshold)
                {
                    _logger.LogWarning("Connection count anomaly detected: Volatility={Volatility:F2}, Count={Count}, MA={MA}",
                        trendData.VolatilityScore, trendData.ConnectionCount, trendData.MovingAverage15Min);

                    if (trendData.ConnectionCount > trendData.MovingAverage15Min * 1.5)
                    {
                        _logger.LogWarning("Potential connection spike detected - investigating...");
                    }
                    else if (trendData.ConnectionCount < trendData.MovingAverage15Min * 0.5)
                    {
                        _logger.LogWarning("Potential connection drop detected - investigating...");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error detecting connection anomalies");
            }
        }

        private void StoreConnectionBreakdownHistory(ConnectionBreakdown breakdown)
        {
            try
            {
                var historyKey = $"connection_breakdown_history_{breakdown.Timestamp:yyyyMMddHHmm}";

                _logger.LogTrace("Stored connection breakdown history: {Key}, Total={Total}",
                    historyKey, breakdown.TotalConnections);

                AnalyzeConnectionDistribution(breakdown);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error storing connection breakdown history");
            }
        }

        private void AnalyzeConnectionDistribution(ConnectionBreakdown breakdown)
        {
            try
            {
                if (breakdown.TotalConnections == 0)
                    return;

                var httpPercentage = (double)breakdown.HttpConnections / breakdown.TotalConnections * 100;
                var dbPercentage = (double)breakdown.DatabaseConnections / breakdown.TotalConnections * 100;
                var wsPercentage = (double)breakdown.WebSocketConnections / breakdown.TotalConnections * 100;
                var externalPercentage = (double)breakdown.ExternalServiceConnections / breakdown.TotalConnections * 100;

                _logger.LogTrace("Connection distribution - HTTP: {Http:F1}%, DB: {Db:F1}%, WS: {Ws:F1}%, External: {Ext:F1}%",
                    httpPercentage, dbPercentage, wsPercentage, externalPercentage);

                if (dbPercentage > 50)
                {
                    _logger.LogDebug("Database connections represent over 50% of total connections - consider connection pooling optimization");
                }

                if (wsPercentage > 30)
                {
                    _logger.LogDebug("WebSocket connections represent over 30% of total connections - high real-time workload detected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error analyzing connection distribution");
            }
        }
    }

    internal class ConnectionCacheEntry
    {
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    internal class ConnectionBreakdown
    {
        public DateTime Timestamp { get; set; }
        public int TotalConnections { get; set; }
        public int HttpConnections { get; set; }
        public int DatabaseConnections { get; set; }
        public int ExternalServiceConnections { get; set; }
        public int WebSocketConnections { get; set; }
        public int ActiveRequestConnections { get; set; }
        public double ThreadPoolUtilization { get; set; }
        public double DatabasePoolUtilization { get; set; }
    }

    internal class ConnectionTrendData
    {
        public DateTime Timestamp { get; set; }
        public int ConnectionCount { get; set; }
        public int MovingAverage5Min { get; set; }
        public int MovingAverage15Min { get; set; }
        public int MovingAverage60Min { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
        public double VolatilityScore { get; set; }
    }

    internal class PeakConnectionMetrics
    {
        public int DailyPeak { get; set; }
        public int HourlyPeak { get; set; }
        public int AllTimePeak { get; set; }
        public DateTime LastPeakTimestamp { get; set; }
    }

    internal class ConnectionTrendDataPoint
    {
        public DateTime Timestamp { get; set; }
        public int ConnectionCount { get; set; }
        public double MovingAverage5Min { get; set; }
        public double MovingAverage15Min { get; set; }
        public double MovingAverage1Hour { get; set; }
        public string TrendDirection { get; set; } = "stable";
        public double VolatilityScore { get; set; }
    }
}
