using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;

namespace Relay.Core.AI.Optimization.Connection;

public class WebSocketConnectionMetricsProvider(
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

    // Lazy-initialized strategies
    private SignalREstimationStrategy? _signalRStrategy;
    private RawWebSocketEstimationStrategy? _rawWebSocketStrategy;
    private ServerSentEventEstimationStrategy? _sseStrategy;
    private LongPollingEstimationStrategy? _longPollingStrategy;
    private ConnectionCalculators? _calculators;

    private SignalREstimationStrategy GetSignalRStrategy() =>
        _signalRStrategy ??= new SignalREstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);

    private RawWebSocketEstimationStrategy GetRawWebSocketStrategy() =>
        _rawWebSocketStrategy ??= new RawWebSocketEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);

    private ServerSentEventEstimationStrategy GetSSEStrategy() =>
        _sseStrategy ??= new ServerSentEventEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);

    private LongPollingEstimationStrategy GetLongPollingStrategy() =>
        _longPollingStrategy ??= new LongPollingEstimationStrategy(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);

    private ConnectionCalculators GetCalculators() =>
        _calculators ??= new ConnectionCalculators(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);

    public int GetWebSocketConnectionCount()
    {
        try
        {
            var webSocketConnections = 0;

            // 1. SignalR Hub connections
            webSocketConnections += GetSignalRStrategy().EstimateConnections();

            // 2. Raw WebSocket connections (non-SignalR)
            webSocketConnections += GetRawWebSocketStrategy().EstimateConnections();

            // 3. Server-Sent Events (SSE) long-polling fallback connections
            webSocketConnections += GetSSEStrategy().EstimateConnections();

            // 4. Long-polling connections (WebSocket fallback)
            webSocketConnections += GetLongPollingStrategy().EstimateConnections();

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
                finalCount, GetSignalRStrategy().EstimateConnections(), GetRawWebSocketStrategy().EstimateConnections(),
                GetSSEStrategy().EstimateConnections(), GetLongPollingStrategy().EstimateConnections());

            return finalCount;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating WebSocket connections, using fallback");
            return GetFallbackWebSocketConnectionCount();
        }
    }

    private int FilterWebSocketConnections(int connections)
    {
        try
        {
            // Apply health-based filtering to connection estimates
            var healthRatio = GetCalculators().CalculateConnectionHealthRatio();
            var filteredConnections = (int)(connections * healthRatio);

            // Apply system load adjustments
            var loadLevel = GetCalculators().ClassifyCurrentLoadLevel();
            var loadAdjustment = GetCalculators().GetLoadBasedConnectionAdjustment(loadLevel);
            filteredConnections = (int)(filteredConnections * loadAdjustment);

            return Math.Max(0, filteredConnections);
        }
        catch
        {
            return connections; // Return original if filtering fails
        }
    }

    private int EstimateWebSocketConnectionsByActivity()
    {
        try
        {
            // Fallback estimation based on overall system activity
            var activeRequests = _systemMetrics.GetActiveRequestCount();
            var throughput = _systemMetrics.CalculateCurrentThroughput();

            // Base estimate from throughput (real-time apps have higher throughput)
            var baseEstimate = (int)(throughput * 0.8); // 80% of throughput as connections

            // Adjust for active requests
            var requestAdjustment = Math.Min(activeRequests / 10.0, 2.0); // Cap at 2x
            var estimate = (int)(baseEstimate * requestAdjustment);

            // Conservative cap
            return Math.Min(estimate, _options.MaxEstimatedWebSocketConnections / 10);
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
            // Very conservative fallback estimation
            var activeRequests = _systemMetrics.GetActiveRequestCount();
            return Math.Min(activeRequests / 20, 10); // Max 10 connections
        }
        catch
        {
            return 1; // Minimum fallback
        }
    }
}
