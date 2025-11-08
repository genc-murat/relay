using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Relay.Core.Transactions;

/// <summary>
/// Health check for transaction system monitoring.
/// </summary>
/// <remarks>
/// This health check reports the current state of the transaction system including:
/// - Active transaction count
/// - Transaction success rate
/// - Average transaction duration
/// - Timeout occurrence rate
/// - Retry statistics
/// 
/// The health status is determined based on configurable thresholds:
/// - Healthy: Success rate above threshold, timeout rate below threshold
/// - Degraded: Success rate or timeout rate approaching thresholds
/// - Unhealthy: Success rate below threshold or timeout rate above threshold
/// 
/// Example registration:
/// <code>
/// services.AddHealthChecks()
///     .AddCheck&lt;TransactionHealthCheck&gt;(
///         "transaction_system",
///         failureStatus: HealthStatus.Degraded,
///         tags: new[] { "ready", "transactions" });
/// </code>
/// 
/// Example health check response:
/// <code>
/// {
///   "status": "Healthy",
///   "totalDuration": "00:00:00.0234567",
///   "entries": {
///     "transaction_system": {
///       "status": "Healthy",
///       "description": "Transaction system is healthy",
///       "data": {
///         "active_transactions": 5,
///         "total_transactions": 1000,
///         "success_rate": 0.98,
///         "failure_rate": 0.01,
///         "timeout_rate": 0.01,
///         "average_duration_ms": 150.5,
///         "transactions_by_isolation_level": {
///           "ReadCommitted": 800,
///           "Serializable": 200
///         }
///       }
///     }
///   }
/// }
/// </code>
/// </remarks>
public class TransactionHealthCheck : IHealthCheck
{
    private readonly ITransactionMetricsCollector _metricsCollector;
    private readonly TransactionHealthCheckOptions _options;
    private long _activeTransactions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionHealthCheck"/> class.
    /// </summary>
    /// <param name="metricsCollector">The metrics collector to retrieve transaction statistics from.</param>
    /// <param name="options">The health check configuration options.</param>
    public TransactionHealthCheck(
        ITransactionMetricsCollector metricsCollector,
        TransactionHealthCheckOptions? options = null)
    {
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _options = options ?? new TransactionHealthCheckOptions();
    }

    /// <summary>
    /// Increments the active transaction counter.
    /// </summary>
    /// <remarks>
    /// This method should be called when a transaction begins.
    /// Thread-safe and can be called concurrently.
    /// </remarks>
    public void IncrementActiveTransactions()
    {
        Interlocked.Increment(ref _activeTransactions);
    }

    /// <summary>
    /// Decrements the active transaction counter.
    /// </summary>
    /// <remarks>
    /// This method should be called when a transaction completes (commit or rollback).
    /// Thread-safe and can be called concurrently.
    /// </remarks>
    public void DecrementActiveTransactions()
    {
        Interlocked.Decrement(ref _activeTransactions);
    }

    /// <summary>
    /// Performs the health check evaluation.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the health check result.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = _metricsCollector.GetMetrics();
            var activeCount = Interlocked.Read(ref _activeTransactions);

            var data = new Dictionary<string, object>
            {
                ["active_transactions"] = activeCount,
                ["total_transactions"] = metrics.TotalTransactions,
                ["successful_transactions"] = metrics.SuccessfulTransactions,
                ["failed_transactions"] = metrics.FailedTransactions,
                ["rolled_back_transactions"] = metrics.RolledBackTransactions,
                ["timeout_transactions"] = metrics.TimeoutTransactions,
                ["success_rate"] = metrics.SuccessRate,
                ["failure_rate"] = metrics.FailureRate,
                ["timeout_rate"] = metrics.TimeoutRate,
                ["average_duration_ms"] = metrics.AverageDurationMs
            };

            // Add isolation level breakdown if available
            if (metrics.TransactionsByIsolationLevel.Any())
            {
                data["transactions_by_isolation_level"] = metrics.TransactionsByIsolationLevel;
            }

            // Add savepoint statistics if available
            if (metrics.SavepointOperations.Any())
            {
                data["savepoint_operations"] = metrics.SavepointOperations;
            }

            // Determine health status based on thresholds
            var status = DetermineHealthStatus(metrics, activeCount);
            var description = GetHealthDescription(status, metrics, activeCount);

            return Task.FromResult(new HealthCheckResult(
                status: status,
                description: description,
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(
                status: context.Registration.FailureStatus,
                description: "Failed to evaluate transaction health",
                exception: ex));
        }
    }

    private HealthStatus DetermineHealthStatus(TransactionMetrics metrics, long activeCount)
    {
        // If no transactions have been processed, system is healthy
        if (metrics.TotalTransactions == 0)
        {
            return HealthStatus.Healthy;
        }

        // Check if too many active transactions (potential deadlock or performance issue)
        if (activeCount > _options.MaxActiveTransactionsThreshold)
        {
            return HealthStatus.Degraded;
        }

        // Check success rate
        if (metrics.SuccessRate < _options.UnhealthySuccessRateThreshold)
        {
            return HealthStatus.Unhealthy;
        }

        if (metrics.SuccessRate < _options.DegradedSuccessRateThreshold)
        {
            return HealthStatus.Degraded;
        }

        // Check timeout rate
        if (metrics.TimeoutRate > _options.UnhealthyTimeoutRateThreshold)
        {
            return HealthStatus.Unhealthy;
        }

        if (metrics.TimeoutRate > _options.DegradedTimeoutRateThreshold)
        {
            return HealthStatus.Degraded;
        }

        // Check average duration
        if (metrics.AverageDurationMs > _options.UnhealthyAverageDurationMs)
        {
            return HealthStatus.Unhealthy;
        }

        if (metrics.AverageDurationMs > _options.DegradedAverageDurationMs)
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }

    private string GetHealthDescription(HealthStatus status, TransactionMetrics metrics, long activeCount)
    {
        return status switch
        {
            HealthStatus.Healthy => 
                $"Transaction system is healthy. {metrics.TotalTransactions} transactions processed with {metrics.SuccessRate:P1} success rate.",
            
            HealthStatus.Degraded => 
                $"Transaction system is degraded. Active: {activeCount}, Success rate: {metrics.SuccessRate:P1}, Timeout rate: {metrics.TimeoutRate:P1}, Avg duration: {metrics.AverageDurationMs:F1}ms",
            
            HealthStatus.Unhealthy => 
                $"Transaction system is unhealthy. Active: {activeCount}, Success rate: {metrics.SuccessRate:P1}, Timeout rate: {metrics.TimeoutRate:P1}, Avg duration: {metrics.AverageDurationMs:F1}ms",
            
            _ => "Transaction system status unknown"
        };
    }
}

/// <summary>
/// Configuration options for transaction health check thresholds.
/// </summary>
/// <remarks>
/// These thresholds determine when the transaction system is considered healthy, degraded, or unhealthy.
/// Adjust these values based on your application's requirements and SLAs.
/// 
/// Example configuration:
/// <code>
/// services.Configure&lt;TransactionHealthCheckOptions&gt;(options =>
/// {
///     options.DegradedSuccessRateThreshold = 0.95;
///     options.UnhealthySuccessRateThreshold = 0.90;
///     options.DegradedTimeoutRateThreshold = 0.05;
///     options.UnhealthyTimeoutRateThreshold = 0.10;
/// });
/// </code>
/// </remarks>
public class TransactionHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the success rate threshold below which the system is considered degraded.
    /// </summary>
    /// <remarks>
    /// Default is 0.95 (95%). If success rate falls below this value, health status becomes Degraded.
    /// </remarks>
    public double DegradedSuccessRateThreshold { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets the success rate threshold below which the system is considered unhealthy.
    /// </summary>
    /// <remarks>
    /// Default is 0.90 (90%). If success rate falls below this value, health status becomes Unhealthy.
    /// </remarks>
    public double UnhealthySuccessRateThreshold { get; set; } = 0.90;

    /// <summary>
    /// Gets or sets the timeout rate threshold above which the system is considered degraded.
    /// </summary>
    /// <remarks>
    /// Default is 0.05 (5%). If timeout rate exceeds this value, health status becomes Degraded.
    /// </remarks>
    public double DegradedTimeoutRateThreshold { get; set; } = 0.05;

    /// <summary>
    /// Gets or sets the timeout rate threshold above which the system is considered unhealthy.
    /// </summary>
    /// <remarks>
    /// Default is 0.10 (10%). If timeout rate exceeds this value, health status becomes Unhealthy.
    /// </remarks>
    public double UnhealthyTimeoutRateThreshold { get; set; } = 0.10;

    /// <summary>
    /// Gets or sets the average duration threshold in milliseconds above which the system is considered degraded.
    /// </summary>
    /// <remarks>
    /// Default is 5000ms (5 seconds). If average transaction duration exceeds this value, health status becomes Degraded.
    /// </remarks>
    public double DegradedAverageDurationMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the average duration threshold in milliseconds above which the system is considered unhealthy.
    /// </summary>
    /// <remarks>
    /// Default is 10000ms (10 seconds). If average transaction duration exceeds this value, health status becomes Unhealthy.
    /// </remarks>
    public double UnhealthyAverageDurationMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of active transactions before the system is considered degraded.
    /// </summary>
    /// <remarks>
    /// Default is 100. If active transaction count exceeds this value, health status becomes Degraded.
    /// This helps detect potential deadlocks or performance bottlenecks.
    /// </remarks>
    public int MaxActiveTransactionsThreshold { get; set; } = 100;
}
