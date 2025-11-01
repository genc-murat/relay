using Microsoft.Extensions.Logging;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

public class ProtocolMetricsCalculator(
    ILogger logger,
    ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
    Analysis.TimeSeries.TimeSeriesDatabase timeSeriesDb,
    SystemMetricsCalculator systemMetrics)
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
    private readonly Analysis.TimeSeries.TimeSeriesDatabase _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
    private readonly SystemMetricsCalculator _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));

    public virtual double CalculateProtocolMultiplexingFactor()
    {
        try
        {
            // HTTP/2 and HTTP/3 support request multiplexing
            // One connection can handle multiple concurrent requests

            // Try to get stored protocol metrics first
            var http1Metrics = _timeSeriesDb.GetRecentMetrics("Protocol_HTTP1", 50);
            var http2Metrics = _timeSeriesDb.GetRecentMetrics("Protocol_HTTP2", 50);
            var http3Metrics = _timeSeriesDb.GetRecentMetrics("Protocol_HTTP3", 50);

            double http1Percentage = 0.4; // Default: 40% HTTP/1.1
            double http2Percentage = 0.5; // Default: 50% HTTP/2
            double http3Percentage = 0.1; // Default: 10% HTTP/3

            // Calculate actual protocol distribution from metrics if available
            var hasMetrics = http1Metrics.Count != 0 || http2Metrics.Count != 0 || http3Metrics.Count != 0;
            if (hasMetrics)
            {
                var http1Count = http1Metrics.Count != 0 ? http1Metrics.Average(m => m.Value) : 0;
                var http2Count = http2Metrics.Count != 0 ? http2Metrics.Average(m => m.Value) : 0;
                var http3Count = http3Metrics.Count != 0 ? http3Metrics.Average(m => m.Value) : 0;
                var totalProtocolRequests = http1Count + http2Count + http3Count;

                if (totalProtocolRequests > 0)
                {
                    http1Percentage = http1Count / totalProtocolRequests;
                    http2Percentage = http2Count / totalProtocolRequests;
                    http3Percentage = http3Count / totalProtocolRequests;

                    _logger.LogDebug("Protocol distribution: HTTP/1.1={Http1:P}, HTTP/2={Http2:P}, HTTP/3={Http3:P}",
                        http1Percentage, http2Percentage, http3Percentage);
                }
            }
            else
            {
                // Estimate from request analytics patterns
                var totalRequests = _requestAnalytics.Values.Sum(x => x.TotalExecutions);

                if (totalRequests > 100)
                {
                    // Adaptive estimation based on system characteristics
                    var avgExecutionTime = _requestAnalytics.Values
                        .Where(x => x.TotalExecutions > 0)
                        .Average(x => x.AverageExecutionTime.TotalMilliseconds);

                    // Modern services with low latency likely use HTTP/2+
                    if (avgExecutionTime < 50)
                    {
                        http1Percentage = 0.2; // 20% HTTP/1.1
                        http2Percentage = 0.6; // 60% HTTP/2
                        http3Percentage = 0.2; // 20% HTTP/3
                    }
                    else if (avgExecutionTime < 200)
                    {
                        http1Percentage = 0.3; // 30% HTTP/1.1
                        http2Percentage = 0.6; // 60% HTTP/2
                        http3Percentage = 0.1; // 10% HTTP/3
                    }
                    // Otherwise use defaults
                }
            }

            // Calculate multiplexing efficiency for each protocol
            // HTTP/1.1: No multiplexing, 1 connection per request
            var http1Efficiency = 1.0;

            // HTTP/2: Stream multiplexing with typical 100 concurrent streams
            // Real-world efficiency varies by server load and stream management
            var concurrentStreamsHttp2 = CalculateOptimalConcurrentStreams(http2Percentage);
            var http2Efficiency = 1.0 / Math.Max(1.0, concurrentStreamsHttp2);

            // HTTP/3: QUIC multiplexing, often better than HTTP/2 due to no head-of-line blocking
            var concurrentStreamsHttp3 = CalculateOptimalConcurrentStreams(http3Percentage) * 1.2; // 20% better
            var http3Efficiency = 1.0 / Math.Max(1.0, concurrentStreamsHttp3);

            // Calculate weighted average factor
            var factor = (http1Percentage * http1Efficiency) +
                         (http2Percentage * http2Efficiency) +
                         (http3Percentage * http3Efficiency);

            // Apply system load adjustment
            // High load reduces multiplexing efficiency due to contention
            var systemLoad = GetDatabasePoolUtilization();
            if (systemLoad > 0.8)
            {
                factor *= 1.2; // Increase connection need by 20% under high load
            }
            else if (systemLoad < 0.3)
            {
                factor *= 0.9; // Decrease connection need by 10% under low load
            }

            // Store calculated metrics for future reference
            _timeSeriesDb.StoreMetric("ProtocolMultiplexingFactor", factor, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("Protocol_HTTP1_Percentage", http1Percentage, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("Protocol_HTTP2_Percentage", http2Percentage, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("Protocol_HTTP3_Percentage", http3Percentage, DateTime.UtcNow);

            // Clamp factor to reasonable bounds (0.1 to 1.0)
            return Math.Max(0.1, Math.Min(1.0, factor));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating protocol multiplexing factor");
            return 0.7; // Default: 30% efficiency from multiplexing
        }
    }

    public double CalculateOptimalConcurrentStreams(double protocolPercentage)
    {
        try
        {
            // Calculate optimal concurrent streams based on usage and system capacity
            var activeRequests = GetActiveRequestCount();
            var avgResponseTime = _requestAnalytics.Values
                .Where(x => x.TotalExecutions > 0)
                .Average(x => x.AverageExecutionTime.TotalMilliseconds);

            // Base concurrent streams (HTTP/2 default is typically 100-128)
            var baseStreams = 100.0;

            // Adjust based on response time
            if (avgResponseTime < 50)
            {
                // Fast responses can handle more concurrent streams
                baseStreams = 128.0;
            }
            else if (avgResponseTime > 500)
            {
                // Slow responses need fewer concurrent streams to avoid overwhelming
                baseStreams = 50.0;
            }

            // Adjust based on active request volume
            if (activeRequests > 1000)
            {
                // High volume: increase stream reuse
                baseStreams = Math.Min(baseStreams * 1.5, 200.0);
            }
            else if (activeRequests < 10)
            {
                // Low volume: reduce stream allocation
                baseStreams = Math.Max(baseStreams * 0.5, 20.0);
            }

            // Protocol percentage influences effective utilization
            // Higher percentage means better optimization of the protocol
            var utilizationFactor = 0.5 + (protocolPercentage * 0.5); // 50% to 100% utilization

            return baseStreams * utilizationFactor;
        }
        catch
        {
            return 50.0; // Safe default for concurrent streams
        }
    }

    public double CalculateKeepAliveConnectionFactor()
    {
        try
        {
            // Keep-alive connections remain open after request completion
            // This increases the total connection count

            var avgResponseTime = _systemMetrics.CalculateAverageResponseTime();
            var throughput = _systemMetrics.CalculateCurrentThroughput();

            if (throughput == 0)
                return 1.5; // Default 50% increase

            // Higher throughput with fast responses = more reused connections
            // Lower throughput with slow responses = more persistent idle connections

            if (avgResponseTime.TotalMilliseconds < 100 && throughput > 10)
            {
                // Fast API with high throughput - efficient reuse
                return 1.3; // 30% increase
            }
            else if (avgResponseTime.TotalMilliseconds > 1000)
            {
                // Slow responses - connections held longer
                return 1.7; // 70% increase
            }
            else
            {
                // Normal scenario
                return 1.5; // 50% increase
            }
        }
        catch
        {
            return 1.5; // Default multiplier
        }
    }

    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
    private double GetDatabasePoolUtilization() => _systemMetrics.GetDatabasePoolUtilization();
}