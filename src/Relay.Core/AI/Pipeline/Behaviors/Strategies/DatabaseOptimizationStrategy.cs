using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Contexts;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Strategy for applying database optimization techniques.
/// </summary>
public class DatabaseOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAIOptimizationEngine _aiEngine;
    private readonly AIOptimizationOptions _options;

    public DatabaseOptimizationStrategy(
        ILogger logger,
        IAIOptimizationEngine aiEngine,
        AIOptimizationOptions options,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
        _aiEngine = aiEngine ?? throw new ArgumentNullException(nameof(aiEngine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.DatabaseOptimization;

    public override ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Database optimizations are beneficial when database load is high or when explicitly recommended with high confidence
        return new ValueTask<bool>(MeetsConfidenceThreshold(recommendation, _options.MinConfidenceScore) &&
                                   (systemLoad.DatabasePoolUtilization > 0.7 || systemLoad.ThroughputPerSecond > 50 ||
                                    recommendation.ConfidenceScore > 0.8)); // Allow high-confidence recommendations even with lower load
    }

    public override async ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Extract database optimization parameters from AI recommendation
        var enableQueryOptimization = GetParameter(recommendation, "EnableQueryOptimization", true);
        var enableConnectionPooling = GetParameter(recommendation, "EnableConnectionPooling", true);
        var enableReadOnlyHint = GetParameter(recommendation, "EnableReadOnlyHint", false);
        var enableBatchingHint = GetParameter(recommendation, "EnableBatchingHint", false);
        var enableNoTracking = GetParameter(recommendation, "EnableNoTracking", true);
        var maxRetries = GetParameter(recommendation, "MaxRetries", 3);
        var retryDelay = GetParameter(recommendation, "RetryDelayMs", 100);
        var queryTimeout = GetParameter(recommendation, "QueryTimeoutSeconds", 30);

        Logger.LogDebug("Applying database optimization for {RequestType}: QueryOpt={QueryOpt}, Pooling={Pooling}, ReadOnly={ReadOnly}, NoTracking={NoTracking}",
            typeof(TRequest).Name, enableQueryOptimization, enableConnectionPooling, enableReadOnlyHint, enableNoTracking);

        await Task.CompletedTask;

        // Wrap handler with database optimization logic
        return async () =>
        {
            var dbContext = new DatabaseOptimizationContext
            {
                EnableQueryOptimization = enableQueryOptimization,
                EnableConnectionPooling = enableConnectionPooling,
                EnableReadOnlyHint = enableReadOnlyHint,
                EnableBatchingHint = enableBatchingHint,
                EnableNoTracking = enableNoTracking,
                MaxRetries = maxRetries,
                RetryDelayMs = retryDelay,
                QueryTimeoutSeconds = queryTimeout,
                RequestType = typeof(TRequest)
            };

            using var scope = DatabaseOptimizationScope.Create(dbContext, Logger);

            var retryCount = 0;
            var startTime = DateTime.UtcNow;

            while (retryCount <= maxRetries)
            {
                try
                {
                    // Execute handler with database optimizations
                    var response = await next();

                    var duration = DateTime.UtcNow - startTime;
                    var stats = scope.GetStatistics();

                    // Record successful execution
                    RecordDatabaseOptimizationMetrics(typeof(TRequest), duration, stats, dbContext, success: true);

                    Logger.LogDebug("Database optimization for {RequestType}: Duration={Duration}ms, Queries={Queries}, Connections={Connections}, RetryCount={RetryCount}",
                        typeof(TRequest).Name, duration.TotalMilliseconds, stats.QueriesExecuted, stats.ConnectionsOpened, retryCount);

                    return response;
                }
                catch (Exception ex) when (IsTransientDatabaseError(ex) && retryCount < maxRetries)
                {
                    retryCount++;
                    scope.RecordRetry();

                    Logger.LogWarning(ex, "Transient database error for {RequestType}, retry {RetryCount}/{MaxRetries}",
                        typeof(TRequest).Name, retryCount, maxRetries);

                    // Exponential backoff
                    var delay = retryDelay * (int)Math.Pow(2, retryCount - 1);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    var duration = DateTime.UtcNow - startTime;
                    var stats = scope.GetStatistics();

                    Logger.LogError(ex, "Database optimization execution failed for {RequestType} after {RetryCount} retries",
                        typeof(TRequest).Name, retryCount);

                    // Record failure
                    RecordDatabaseOptimizationMetrics(typeof(TRequest), duration, stats, dbContext, success: false);

                    throw;
                }
            }

            throw new InvalidOperationException($"Database operation failed after {maxRetries} retries");
        };
    }

    private bool IsTransientDatabaseError(Exception ex)
    {
        // Check for common transient database errors
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        var exceptionType = ex.GetType().Name.ToLowerInvariant();

        return message.Contains("timeout") ||
               message.Contains("deadlock") ||
               message.Contains("connection") ||
               message.Contains("network") ||
               message.Contains("transport") ||
               exceptionType.Contains("timeout") ||
               exceptionType.Contains("sqlexception");
    }

    private void RecordDatabaseOptimizationMetrics(
        Type requestType,
        TimeSpan duration,
        DatabaseOptimizationStatistics stats,
        DatabaseOptimizationContext context,
        bool success)
    {
        var properties = new Dictionary<string, object>
        {
            ["QueriesExecuted"] = stats.QueriesExecuted,
            ["ConnectionsOpened"] = stats.ConnectionsOpened,
            ["ConnectionsReused"] = stats.ConnectionsReused,
            ["TotalQueryTime"] = stats.TotalQueryTime.TotalMilliseconds,
            ["AverageQueryTime"] = stats.AverageQueryTime.TotalMilliseconds,
            ["SlowestQueryTime"] = stats.SlowestQueryTime.TotalMilliseconds,
            ["RetryCount"] = stats.RetryCount,
            ["EnableQueryOptimization"] = context.EnableQueryOptimization,
            ["EnableConnectionPooling"] = context.EnableConnectionPooling,
            ["EnableNoTracking"] = context.EnableNoTracking,
            ["ConnectionPoolEfficiency"] = stats.ConnectionPoolEfficiency,
            ["QueryEfficiency"] = stats.QueryEfficiency
        };

        RecordMetrics(requestType, duration, success, properties);
    }
}