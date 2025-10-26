using Microsoft.Extensions.Logging;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class HttpConnectionMetricsProvider
{
    private readonly ILogger _logger;
    private readonly Relay.Core.AI.AIOptimizationOptions _options;
    private readonly Relay.Core.AI.SystemMetricsCalculator _systemMetrics;
    private readonly AspNetCoreConnectionEstimator _aspNetCoreEstimator;
    private readonly HttpClientPoolEstimator _httpClientPoolEstimator;
    private readonly LoadBalancerConnectionEstimator _loadBalancerEstimator;
    private readonly ProtocolMetricsCalculator _protocolCalculator;
    private readonly ConnectionMetricsUtilities _utilities;
    private WebSocketConnectionMetricsProvider? _webSocketProvider;

    public HttpConnectionMetricsProvider(
        ILogger logger,
        Relay.Core.AI.AIOptimizationOptions options,
        ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
        Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase timeSeriesDb,
        Relay.Core.AI.SystemMetricsCalculator systemMetrics)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));

        // Initialize specialized estimators
        _protocolCalculator = new ProtocolMetricsCalculator(logger, requestAnalytics, timeSeriesDb, systemMetrics);
        _aspNetCoreEstimator = new AspNetCoreConnectionEstimator(logger, options, requestAnalytics, timeSeriesDb, systemMetrics, _protocolCalculator);
        _httpClientPoolEstimator = new HttpClientPoolEstimator(logger, options, requestAnalytics, timeSeriesDb, systemMetrics);
        _loadBalancerEstimator = new LoadBalancerConnectionEstimator(logger, options, timeSeriesDb, systemMetrics);
        _utilities = new ConnectionMetricsUtilities(logger, options, requestAnalytics, timeSeriesDb, systemMetrics, _protocolCalculator);
    }
    
    // Setter method to allow setting the WebSocket provider externally
    public void SetWebSocketProvider(WebSocketConnectionMetricsProvider webSocketProvider)
    {
        _webSocketProvider = webSocketProvider;
    }

    // Public methods for external access (used by ConnectionMetricsProvider facade)
    public int GetAspNetCoreConnectionCount() => _aspNetCoreEstimator.GetAspNetCoreConnectionCount();
    public int GetKestrelServerConnections() => _aspNetCoreEstimator.GetKestrelServerConnections();
    public int GetHttpClientPoolConnectionCount() => _httpClientPoolEstimator.GetHttpClientPoolConnectionCount();
    public int GetOutboundHttpConnectionCount() => _utilities.GetOutboundHttpConnectionCount();
    public int GetUpgradedConnectionCount() => _utilities.GetUpgradedConnectionCount(_webSocketProvider);
    public int GetLoadBalancerConnectionCount() => _loadBalancerEstimator.GetLoadBalancerConnectionCount();
    public int GetFallbackHttpConnectionCount() => _utilities.GetFallbackHttpConnectionCount();

    public int GetHttpConnectionCount()
    {
        var httpConnections = 0;

        // 1. Kestrel/ASP.NET Core connection tracking
        httpConnections += _aspNetCoreEstimator.GetAspNetCoreConnectionCount();

        // 2. HttpClient connection pool monitoring
        httpConnections += _httpClientPoolEstimator.GetHttpClientPoolConnectionCount();

        // 3. Outbound HTTP connections (service-to-service)
        httpConnections += _utilities.GetOutboundHttpConnectionCount();

        // 4. WebSocket upgrade connections (counted as HTTP initially)
        httpConnections += _utilities.GetUpgradedConnectionCount(_webSocketProvider);

        // 5. Load balancer connection tracking
        httpConnections += _loadBalancerEstimator.GetLoadBalancerConnectionCount();

        // 6. Estimate based on current request throughput as fallback
        if (httpConnections == 0)
        {
            var throughput = _utilities.CalculateConnectionThroughputFactor();
            httpConnections = (int)(throughput * 0.7); // 70% of throughput reflects active connections

            // Factor in concurrent request processing
            var activeRequests = _systemMetrics.GetActiveRequestCount();
            httpConnections += Math.Min(activeRequests, Environment.ProcessorCount * 2);

            // Consider connection keep-alive patterns
            var keepAliveConnections = _utilities.EstimateKeepAliveConnections();
            httpConnections += keepAliveConnections;
        }

        var finalCount = Math.Min(httpConnections, _options.MaxEstimatedHttpConnections);

        _logger.LogTrace("HTTP connection count calculated: {Count} " +
            "(ASP.NET Core: {AspNetCore}, HttpClient Pool: {HttpClientPool}, Outbound: {Outbound})",
            finalCount, _aspNetCoreEstimator.GetAspNetCoreConnectionCount(), _httpClientPoolEstimator.GetHttpClientPoolConnectionCount(),
            _utilities.GetOutboundHttpConnectionCount());

        return finalCount;
    }


















}