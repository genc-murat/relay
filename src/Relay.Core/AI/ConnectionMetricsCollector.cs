using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Collects and analyzes connection metrics across different connection types
    /// </summary>
    internal sealed class ConnectionMetricsCollector
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollector(
            ILogger<ConnectionMetricsCollector> logger,
            AIOptimizationOptions options,
            ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
        }

        public int GetActiveConnectionCount(
            Func<int> getActiveRequestCount,
            Func<double> calculateConnectionThroughputFactor,
            Func<int> estimateKeepAliveConnections,
            Func<int, int> filterHealthyConnections,
            Action<int> cacheConnectionCount,
            Func<int> getFallbackConnectionCount)
        {
            try
            {
                var connectionCount = 0;

                connectionCount += GetHttpConnectionCount(getActiveRequestCount, calculateConnectionThroughputFactor, estimateKeepAliveConnections);
                connectionCount += GetDatabaseConnectionCount();
                connectionCount += GetExternalServiceConnectionCount();
                connectionCount += GetWebSocketConnectionCount();

                connectionCount = filterHealthyConnections(connectionCount);
                cacheConnectionCount(connectionCount);

                _logger.LogTrace("Active connection count calculated: {ConnectionCount}", connectionCount);

                return Math.Max(0, connectionCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating active connection count, using fallback estimation");
                return getFallbackConnectionCount();
            }
        }

        public int GetHttpConnectionCount(
            Func<int> getActiveRequestCount,
            Func<double> calculateConnectionThroughputFactor,
            Func<int> estimateKeepAliveConnections)
        {
            try
            {
                var httpConnections = 0;

                httpConnections += GetAspNetCoreConnectionCount(getActiveRequestCount);
                httpConnections += GetHttpClientPoolConnectionCount();
                httpConnections += GetOutboundHttpConnectionCount();
                httpConnections += GetUpgradedConnectionCount();
                httpConnections += GetLoadBalancerConnectionCount();

                if (httpConnections == 0)
                {
                    var throughput = calculateConnectionThroughputFactor();
                    httpConnections = (int)(throughput * 0.7);

                    var activeRequests = getActiveRequestCount();
                    httpConnections += Math.Min(activeRequests, Environment.ProcessorCount * 2);

                    var keepAliveConnections = estimateKeepAliveConnections();
                    httpConnections += keepAliveConnections;
                }

                var finalCount = Math.Min(httpConnections, _options.MaxEstimatedHttpConnections);

                _logger.LogTrace("HTTP connection count calculated: {Count}", finalCount);

                return finalCount;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating HTTP connections, using fallback estimation");
                return GetFallbackHttpConnectionCount();
            }
        }

        private int GetAspNetCoreConnectionCount(Func<int> getActiveRequestCount)
        {
            try
            {
                var activeRequests = getActiveRequestCount();
                var estimatedInboundConnections = Math.Max(1, activeRequests);

                // HTTP/2 multiplexing factor
                var http2Multiplexing = 0.3;
                estimatedInboundConnections = (int)(estimatedInboundConnections * (1 - http2Multiplexing));

                // Keep-alive multiplier
                var keepAliveMultiplier = 1.5;
                estimatedInboundConnections = (int)(estimatedInboundConnections * keepAliveMultiplier);

                return Math.Min(estimatedInboundConnections, _options.MaxEstimatedHttpConnections / 2);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating ASP.NET Core connections");
                return 0;
            }
        }

        private int GetHttpClientPoolConnectionCount()
        {
            try
            {
                var requestAnalytics = _requestAnalytics.Values.ToArray();
                var totalExternalCalls = requestAnalytics.Sum(x => x.ExecutionTimesCount);

                var estimatedPoolSize = Math.Min(10, Math.Max(2, totalExternalCalls / 20));

                var concurrentExternalRequests = requestAnalytics
                    .Where(x => x.ConcurrentExecutionPeaks > 0)
                    .Sum(x => Math.Min(x.ConcurrentExecutionPeaks, 5));

                var activePoolConnections = (int)(estimatedPoolSize * 0.6 + concurrentExternalRequests * 0.2);

                return Math.Max(0, Math.Min(activePoolConnections, 50));
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating HttpClient pool connections");
                return 0;
            }
        }

        private int GetOutboundHttpConnectionCount()
        {
            try
            {
                var requestAnalytics = _requestAnalytics.Values.ToArray();
                var externalApiCallRate = requestAnalytics.Average(x => x.ExternalApiCalls);

                var estimatedOutboundConnections = (int)(externalApiCallRate * 2);

                var poolingEfficiency = 0.4;
                estimatedOutboundConnections = (int)(estimatedOutboundConnections * (1 - poolingEfficiency));

                return Math.Max(0, Math.Min(estimatedOutboundConnections, 30));
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating outbound HTTP connections");
                return 0;
            }
        }

        private int GetUpgradedConnectionCount()
        {
            try
            {
                var webSocketConnections = GetWebSocketConnectionCount();
                var upgradeRate = 0.1;
                var upgradedConnections = (int)(webSocketConnections * upgradeRate);

                return Math.Max(0, upgradedConnections);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating upgraded connections");
                return 0;
            }
        }

        private int GetLoadBalancerConnectionCount()
        {
            try
            {
                var healthCheckConnections = 2;
                var persistentConnections = 1;

                return healthCheckConnections + persistentConnections;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating load balancer connections");
                return 0;
            }
        }

        private int GetFallbackHttpConnectionCount()
        {
            var processorBasedEstimate = Environment.ProcessorCount * 2;
            return Math.Min(processorBasedEstimate, 20);
        }

        public int GetDatabaseConnectionCount()
        {
            try
            {
                var dbConnections = 0;

                dbConnections += GetSqlServerConnectionCount();
                dbConnections += GetEntityFrameworkConnectionCount();
                dbConnections += GetNoSqlConnectionCount();

                return Math.Min(dbConnections, 100);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating database connections");
                return 5;
            }
        }

        private int GetSqlServerConnectionCount()
        {
            var requestAnalytics = _requestAnalytics.Values.ToArray();
            var avgDbCalls = requestAnalytics.Any() ? requestAnalytics.Average(x => x.DatabaseCalls) : 0;
            return (int)Math.Ceiling(avgDbCalls * 0.3);
        }

        private int GetEntityFrameworkConnectionCount()
        {
            var requestAnalytics = _requestAnalytics.Values.ToArray();
            var avgDbCalls = requestAnalytics.Any() ? requestAnalytics.Average(x => x.DatabaseCalls) : 0;
            return (int)Math.Ceiling(avgDbCalls * 0.2);
        }

        private int GetNoSqlConnectionCount()
        {
            return GetRedisConnectionCount();
        }

        private int GetRedisConnectionCount()
        {
            var requestAnalytics = _requestAnalytics.Values.ToArray();
            var cacheIntensiveRequests = requestAnalytics.Count(x => x.CacheHitRatio > 0.5);
            return Math.Min(cacheIntensiveRequests / 10, 10);
        }

        public int GetExternalServiceConnectionCount()
        {
            try
            {
                var externalConnections = 0;

                externalConnections += GetMessageQueueConnectionCount();
                externalConnections += GetExternalApiConnectionCount();
                externalConnections += GetMicroserviceConnectionCount();

                return Math.Min(externalConnections, 50);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating external service connections");
                return 2;
            }
        }

        private int GetMessageQueueConnectionCount()
        {
            return 2;
        }

        private int GetExternalApiConnectionCount()
        {
            var requestAnalytics = _requestAnalytics.Values.ToArray();
            var avgExternalCalls = requestAnalytics.Any() ? requestAnalytics.Average(x => x.ExternalApiCalls) : 0;
            return (int)Math.Ceiling(avgExternalCalls * 0.5);
        }

        private int GetMicroserviceConnectionCount()
        {
            var requestAnalytics = _requestAnalytics.Values.ToArray();
            var avgExternalCalls = requestAnalytics.Any() ? requestAnalytics.Average(x => x.ExternalApiCalls) : 0;
            return (int)Math.Ceiling(avgExternalCalls * 0.3);
        }

        public int GetWebSocketConnectionCount()
        {
            try
            {
                var wsConnections = 0;

                wsConnections += GetSignalRHubConnections();
                wsConnections += GetRawWebSocketConnections();
                wsConnections += GetServerSentEventConnections();
                wsConnections += GetLongPollingConnections();

                wsConnections = FilterWebSocketConnections(wsConnections);

                _logger.LogTrace("WebSocket connection count calculated: {Count}", wsConnections);

                return Math.Min(wsConnections, _options.MaxEstimatedWebSocketConnections);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error calculating WebSocket connections");
                return GetFallbackWebSocketConnectionCount();
            }
        }

        private int GetSignalRHubConnections()
        {
            try
            {
                var estimatedHubConnections = 10;
                var hubMultiplexing = 0.7;
                var effectiveConnections = (int)(estimatedHubConnections * hubMultiplexing);

                var groupBroadcasting = 1.2;
                effectiveConnections = (int)(effectiveConnections * groupBroadcasting);

                return Math.Max(0, effectiveConnections);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating SignalR hub connections");
                return 0;
            }
        }

        private int GetRawWebSocketConnections()
        {
            try
            {
                var requestAnalytics = _requestAnalytics.Values.ToArray();
                var realtimeRequests = requestAnalytics.Count(x => x.ExecutionTimesCount > 100);

                var estimatedWsConnections = realtimeRequests * 2;

                var connectionStability = 0.8;
                estimatedWsConnections = (int)(estimatedWsConnections * connectionStability);

                return Math.Max(0, Math.Min(estimatedWsConnections, 50));
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating raw WebSocket connections");
                return 0;
            }
        }

        private int GetServerSentEventConnections()
        {
            try
            {
                var requestAnalytics = _requestAnalytics.Values.ToArray();
                var longRunningRequests = requestAnalytics.Count(x => x.ExecutionTimesCount > 50);

                var estimatedSseConnections = longRunningRequests;

                var sseOverhead = 1.1;
                estimatedSseConnections = (int)(estimatedSseConnections * sseOverhead);

                return Math.Max(0, Math.Min(estimatedSseConnections, 30));
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating SSE connections");
                return 0;
            }
        }

        private int GetLongPollingConnections()
        {
            try
            {
                var requestAnalytics = _requestAnalytics.Values.ToArray();
                var pollingRequests = requestAnalytics.Count(x => x.RepeatRequestRate > 0.3);

                var estimatedPollingConnections = pollingRequests;

                var churnRate = 2.0;
                estimatedPollingConnections = (int)(estimatedPollingConnections * churnRate);

                var maxConcurrentPolling = 20;
                estimatedPollingConnections = Math.Min(estimatedPollingConnections, maxConcurrentPolling);

                return Math.Max(0, estimatedPollingConnections);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error estimating long polling connections");
                return 0;
            }
        }

        private int FilterWebSocketConnections(int totalConnections)
        {
            try
            {
                var pingPongHealthCheck = 0.9;
                var healthyConnections = (int)(totalConnections * pingPongHealthCheck);

                var idleConnectionTimeout = 0.85;
                healthyConnections = (int)(healthyConnections * idleConnectionTimeout);

                return Math.Max(0, healthyConnections);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error filtering WebSocket connections");
                return totalConnections;
            }
        }

        private int GetFallbackWebSocketConnectionCount()
        {
            return Math.Min(Environment.ProcessorCount, 10);
        }
    }
}
