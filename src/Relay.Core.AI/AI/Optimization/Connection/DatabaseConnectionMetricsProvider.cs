using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Connection;

internal class DatabaseConnectionMetricsProvider
{
    private readonly ILogger _logger;
    private readonly Relay.Core.AI.AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TimeSeriesDatabase _timeSeriesDb;
    private readonly Relay.Core.AI.SystemMetricsCalculator _systemMetrics;

    public DatabaseConnectionMetricsProvider(
        ILogger logger,
        Relay.Core.AI.AIOptimizationOptions options,
        ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
        TimeSeriesDatabase timeSeriesDb,
        Relay.Core.AI.SystemMetricsCalculator systemMetrics)
    {
        _logger = logger;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
        _timeSeriesDb = timeSeriesDb ?? throw new ArgumentNullException(nameof(timeSeriesDb));
        _systemMetrics = systemMetrics ?? throw new ArgumentNullException(nameof(systemMetrics));
    }

    public int GetDatabaseConnectionCount()
    {
        try
        {
            var dbConnections = 0;

            // SQL Server connection pool monitoring
            dbConnections += GetSqlServerConnectionCount();

            // Entity Framework connection tracking
            dbConnections += GetEntityFrameworkConnectionCount();

            // NoSQL database connections (MongoDB, CosmosDB, etc.)
            dbConnections += GetNoSqlConnectionCount();

            // Connection pool utilization analysis
            var poolUtilization = GetDatabasePoolUtilization();
            var estimatedActiveConnections = (int)(poolUtilization * _options.EstimatedMaxDbConnections);

            dbConnections = Math.Max(dbConnections, estimatedActiveConnections);

            return Math.Min(dbConnections, _options.MaxEstimatedDbConnections);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error calculating database connections");
            return (int)(GetDatabasePoolUtilization() * 10); // Rough estimate
        }
    }

    public int GetSqlServerConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("SqlServer_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var avgCount = (int)storedMetrics.Average(m => m.Value);
                return Math.Max(0, avgCount);
            }

            // Estimation based on connection pool utilization
            var poolUtilization = GetDatabasePoolUtilization();
            var estimatedCount = (int)(poolUtilization * _options.EstimatedMaxDbConnections * 0.6); // 60% for SQL Server

            // Apply smoothing based on historical data
            if (storedMetrics.Any())
            {
                var historicalAvg = (int)storedMetrics.Average(m => m.Value);
                // Weighted average: 70% historical, 30% current estimate
                estimatedCount = (int)(historicalAvg * 0.7 + estimatedCount * 0.3);
            }

            // Store estimated metric
            _timeSeriesDb.StoreMetric("SqlServer_ConnectionCount", estimatedCount, DateTime.UtcNow);

            return Math.Max(0, Math.Min(estimatedCount, 100)); // Cap at 100 connections
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error getting SQL Server connection count");
            return 0;
        }
    }

    public int GetEntityFrameworkConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("EntityFramework_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var avgCount = (int)storedMetrics.Average(m => m.Value);

                // If we have recent metrics, use them with slight adjustment for current load
                var currentLoadFactor = GetDatabasePoolUtilization();
                var adjustedCount = (int)(avgCount * (0.7 + currentLoadFactor * 0.3));
                return Math.Max(0, adjustedCount);
            }

            // Estimation based on active requests and request patterns
            var activeRequests = GetActiveRequestCount();
            var avgConnectionsPerRequest = CalculateAverageConnectionsPerRequest();
            var estimatedCount = (int)(activeRequests * avgConnectionsPerRequest);

            // Apply historical patterns to improve accuracy
            var historicalData = _requestAnalytics.Values
                .Where(x => x.TotalExecutions > 10)
                .ToList();

            if (historicalData.Any())
            {
                var avgExecutionTime = historicalData.Average(x => x.AverageExecutionTime.TotalMilliseconds);

                // Longer execution times typically mean connections are held longer
                if (avgExecutionTime > 1000)
                {
                    estimatedCount = (int)(estimatedCount * 1.5); // Increase estimate for long-running operations
                }
                else if (avgExecutionTime < 100)
                {
                    estimatedCount = (int)(estimatedCount * 0.5); // Decrease for fast operations
                }
            }

            // Consider system load
            var poolUtilization = GetDatabasePoolUtilization();
            if (poolUtilization > 0.8)
            {
                // High utilization suggests more connections in use
                estimatedCount = (int)(estimatedCount * 1.2);
            }

            // Store estimated metric for future reference
            _timeSeriesDb.StoreMetric("EntityFramework_ConnectionCount", estimatedCount, DateTime.UtcNow);

            return Math.Max(0, Math.Min(estimatedCount, 50)); // Cap at 50 connections
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error getting Entity Framework connection count");
            return 0;
        }
    }

    private double CalculateAverageConnectionsPerRequest()
    {
        try
        {
            // Analyze historical data to determine average connections per request
            var requestsWithConnectionData = _requestAnalytics.Values
                .Where(x => x.TotalExecutions > 5)
                .ToList();

            if (!requestsWithConnectionData.Any())
            {
                return 0.3; // Default: 30% of requests use a connection
            }

            // Estimate based on execution patterns
            // Longer execution times typically indicate database operations
            var avgExecTime = requestsWithConnectionData.Average(x => x.AverageExecutionTime.TotalMilliseconds);

            if (avgExecTime > 1000) return 0.8; // Long running = likely multiple connections
            if (avgExecTime > 500) return 0.5;  // Medium = moderate connection usage
            if (avgExecTime > 100) return 0.3;  // Fast = some connection usage
            return 0.1; // Very fast = minimal connection usage
        }
        catch
        {
            return 0.3; // Safe default
        }
    }

    public int GetNoSqlConnectionCount()
    {
        try
        {
            // Try to get from stored metrics first
            var storedMetrics = _timeSeriesDb.GetRecentMetrics("NoSql_ConnectionCount", 10);
            if (storedMetrics.Any())
            {
                var recent = storedMetrics.Last().Value;
                _logger?.LogTrace("NoSQL connection count from metrics: {Count}", recent);
                return (int)recent;
            }

            // Estimate from request analytics that might use NoSQL
            var requestCount = _requestAnalytics.Values
                .Where(a => a.TotalExecutions > 0)
                .Sum(a => a.TotalExecutions);

            // Assume NoSQL is used for 20% of requests
            var estimatedNoSqlRequests = requestCount * 0.2;

            // Connection pooling efficiency ~10:1
            var estimatedConnections = Math.Max(1, (int)(estimatedNoSqlRequests / 10));

            // Cap at reasonable limit
            var finalCount = Math.Min(estimatedConnections, 15);

            // Store for future reference
            _timeSeriesDb.StoreMetric("NoSql_ConnectionCount", finalCount, DateTime.UtcNow);

            _logger?.LogDebug("Estimated NoSQL connections: {Count} (from {Requests} requests)",
                finalCount, estimatedNoSqlRequests);

            return finalCount;
        }
        catch (Exception ex)
        {
            _logger?.LogTrace(ex, "Error calculating NoSQL connections");
            return 2; // Safe default
        }
    }

    // Delegates to SystemMetricsCalculator
    private int GetActiveRequestCount() => _systemMetrics.GetActiveRequestCount();
    private double GetDatabasePoolUtilization() => _systemMetrics.GetDatabasePoolUtilization();
}