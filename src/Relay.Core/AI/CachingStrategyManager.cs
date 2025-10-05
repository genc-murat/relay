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
        private readonly ConnectionMetricsCache _metricsCache;

        public CachingStrategyManager(
            ILogger<CachingStrategyManager> logger,
            ConnectionMetricsCache metricsCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsCache = metricsCache ?? throw new ArgumentNullException(nameof(metricsCache));
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
                
                // Use ConnectionMetricsCache for time-series based rolling window
                _metricsCache.CacheConnectionMetricWithRollingWindow(timeBasedKey, connectionCount);

                // Build and store connection breakdown
                var breakdown = new ConnectionBreakdown
                {
                    Timestamp = timestamp,
                    TotalConnections = connectionCount,
                    HttpConnections = getHttpConnectionCount(),
                    DatabaseConnections = getDatabaseConnectionCount(),
                    ExternalServiceConnections = getExternalServiceConnectionCount(),
                    WebSocketConnections = getWebSocketConnectionCount(),
                    ActiveRequestConnections = getActiveRequestCount(),
                    ThreadPoolUtilization = getThreadPoolUtilization(),
                    DatabasePoolUtilization = getDatabasePoolUtilization()
                };

                _logger.LogTrace("Connection breakdown - HTTP: {Http}, DB: {Db}, External: {Ext}, WS: {Ws}",
                    breakdown.HttpConnections, breakdown.DatabaseConnections,
                    breakdown.ExternalServiceConnections, breakdown.WebSocketConnections);

                // Store breakdown history and trend data using ConnectionMetricsCache
                _metricsCache.StoreConnectionBreakdownHistory(breakdown);
                _metricsCache.StoreConnectionTrendData(connectionCount, timestamp);

                _logger.LogTrace("Cached connection count: {Count} at {Timestamp} with time-series database",
                    connectionCount, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error caching connection count - non-critical, continuing");
            }
        }
    }
}
