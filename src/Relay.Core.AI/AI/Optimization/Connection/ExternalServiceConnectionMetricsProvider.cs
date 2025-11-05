using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class ExternalServiceConnectionMetricsProvider
{
    private readonly ILogger _logger;
    private readonly Relay.Core.AI.AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TimeSeriesDatabase _timeSeriesDb;
    private readonly Relay.Core.AI.SystemMetricsCalculator _systemMetrics;

    public ExternalServiceConnectionMetricsProvider(
        ILogger logger,
        Relay.Core.AI.AIOptimizationOptions options,
        ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
        TimeSeriesDatabase timeSeriesDb,
        Relay.Core.AI.SystemMetricsCalculator systemMetrics)
    {
        _logger = logger;
        _options = options;
        _requestAnalytics = requestAnalytics;
        _timeSeriesDb = timeSeriesDb;
        _systemMetrics = systemMetrics;
    }

    public int GetExternalServiceConnectionCount()
    {
        try
        {
            var externalConnections = 0;

            // Redis connection pool
            externalConnections += GetRedisConnectionCount();

            // Message queue connections (RabbitMQ, ServiceBus, etc.)
            externalConnections += GetMessageQueueConnectionCount();

            // External API connections
            externalConnections += GetExternalApiConnectionCount();

            // Microservice connections
            externalConnections += GetMicroserviceConnectionCount();

            return Math.Min(externalConnections, _options.MaxEstimatedExternalConnections);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating external service connections");
            return EstimateExternalConnectionsByLoad();
        }
    }

    public int GetRedisConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("Redis_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var recent = storedMetrics.Last().Value;
                _logger.LogTrace("Redis connection count from metrics: {Count}", recent);
                return (int)recent;
            }

            // Redis typically uses connection multiplexing - very few connections
            // Estimate based on cache hit rate and system load
            var throughput = _systemMetrics.CalculateCurrentThroughput();
            var loadLevel = ClassifyCurrentLoadLevel();

            int redisConnections = loadLevel switch
            {
                LoadLevel.Critical => 5,  // Maximum connections under stress
                LoadLevel.High => 4,
                LoadLevel.Medium => 3,
                LoadLevel.Low => 2,
                LoadLevel.Idle => 1,
                _ => 2
            };

            // Store for future reference
            _timeSeriesDb.StoreMetric("Redis_ConnectionCount", redisConnections, DateTime.UtcNow);

            _logger.LogDebug("Estimated Redis connections: {Count} (Load: {Load}, Throughput: {Throughput:F2})",
                redisConnections, loadLevel, throughput);

            return redisConnections;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating Redis connections");
            return 2; // Safe default - Redis uses multiplexing
        }
    }

    public int GetMessageQueueConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("MessageQueue_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var recent = storedMetrics.Last().Value;
                _logger.LogTrace("Message queue connection count from metrics: {Count}", recent);
                return (int)recent;
            }

            // Estimate based on async processing patterns
            var asyncRequests = _requestAnalytics.Values
                .Where(a => a.AverageExecutionTime.TotalMilliseconds > 1000) // Long-running = likely async
                .Sum(a => a.TotalExecutions);

            // Message queues typically use persistent connections
            // 1 connection per consumer/publisher pair
            var estimatedConnections = asyncRequests > 0 ? Math.Max(1, Math.Min(5, (int)(asyncRequests / 100))) : 0;

            // Store for future reference
            _timeSeriesDb.StoreMetric("MessageQueue_ConnectionCount", estimatedConnections, DateTime.UtcNow);

            _logger.LogDebug("Estimated message queue connections: {Count} (from {Requests} async requests)",
                estimatedConnections, asyncRequests);

            return estimatedConnections;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating message queue connections");
            return 1; // Safe default
        }
    }

    public int GetExternalApiConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("ExternalApi_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var recent = storedMetrics.Last().Value;
                _logger.LogTrace("External API connection count from metrics: {Count}", recent);
                return (int)recent;
            }

            // Estimate external API connections based on recent activity
            var externalApiCalls = _requestAnalytics.Values.Sum(x => x.ExecutionTimesCount) / 10;
            var estimatedConnections = Math.Min(externalApiCalls, 20); // Cap at reasonable limit

            // Store for future reference
            _timeSeriesDb.StoreMetric("ExternalApi_ConnectionCount", estimatedConnections, DateTime.UtcNow);

            _logger.LogDebug("Estimated external API connections: {Count}", estimatedConnections);

            return estimatedConnections;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating external API connections");
            return 5; // Safe default
        }
    }

    public int GetMicroserviceConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("Microservice_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var recent = storedMetrics.Last().Value;
                _logger.LogTrace("Microservice connection count from metrics: {Count}", recent);
                return (int)recent;
            }

            // Estimate based on external API calls
            var externalApiCalls = _requestAnalytics.Values
                .Sum(a => a.ExecutionTimesCount) / Math.Max(1, _requestAnalytics.Count);

            // Assume some external calls are to microservices
            // Connection pooling: ~5:1 ratio
            var estimatedConnections = Math.Max(1, Math.Min(15, externalApiCalls / 5));

            // Factor in current load
            var loadLevel = ClassifyCurrentLoadLevel();
            var loadMultiplier = loadLevel switch
            {
                LoadLevel.Critical => 1.5,
                LoadLevel.High => 1.3,
                LoadLevel.Medium => 1.0,
                LoadLevel.Low => 0.8,
                LoadLevel.Idle => 0.5,
                _ => 1.0
            };

            var finalCount = (int)(estimatedConnections * loadMultiplier);

            // Store for future reference
            _timeSeriesDb.StoreMetric("Microservice_ConnectionCount", finalCount, DateTime.UtcNow);

            _logger.LogDebug("Estimated microservice connections: {Count} (API calls: {Calls}, Load: {Load})",
                finalCount, externalApiCalls, loadLevel);

            return finalCount;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error calculating microservice connections");
            return 3; // Safe default
        }
    }

    private int EstimateExternalConnectionsByLoad()
    {
        var systemLoad = GetDatabasePoolUtilization() + GetThreadPoolUtilization();
        return (int)(systemLoad * 10); // Scale with overall system load
    }

    // Delegates to SystemMetricsCalculator
    private double GetDatabasePoolUtilization() => _systemMetrics.GetDatabasePoolUtilization();
    private double GetThreadPoolUtilization() => _systemMetrics.GetThreadPoolUtilization();

    private LoadLevel ClassifyCurrentLoadLevel()
    {
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
}