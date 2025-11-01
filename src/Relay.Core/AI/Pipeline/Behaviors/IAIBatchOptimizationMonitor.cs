using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Pipeline.Behaviors;

/// <summary>
/// Interface for monitoring AI batch optimization metrics.
/// </summary>
public interface IAIBatchOptimizationMonitor
{
    /// <summary>
    /// Gets the average wait time for batched requests.
    /// </summary>
    /// <param name="requestType">The type of request to get metrics for.</param>
    /// <returns>The average wait time in milliseconds.</returns>
    double GetAverageWaitTime(Type requestType);

    /// <summary>
    /// Gets the average efficiency of batch executions.
    /// </summary>
    /// <param name="requestType">The type of request to get metrics for.</param>
    /// <returns>The average efficiency as a value between 0 and 1.</returns>
    double GetAverageEfficiency(Type requestType);

    /// <summary>
    /// Gets the rate of requests that were processed in batches.
    /// </summary>
    /// <param name="requestType">The type of request to get metrics for.</param>
    /// <returns>The batching rate as a value between 0 and 1.</returns>
    double GetBatchingRate(Type requestType);

    /// <summary>
    /// Gets the request rate for a specific request type.
    /// </summary>
    /// <param name="requestType">The type of request to get metrics for.</param>
    /// <returns>The number of requests per second.</returns>
    double GetRequestRate(Type requestType);

    /// <summary>
    /// Gets all registered request types that have metrics.
    /// </summary>
    /// <returns>A collection of request types.</returns>
    IEnumerable<Type> GetTrackedRequestTypes();

    /// <summary>
    /// Gets all batch metrics for a request type.
    /// </summary>
    /// <param name="requestType">The type of request to get metrics for.</param>
    /// <returns>Detailed batch metrics.</returns>
    BatchMetricsSnapshot GetBatchMetrics(Type requestType);
}

/// <summary>
/// Snapshot of batch metrics for a request type.
/// </summary>
public class BatchMetricsSnapshot
{
    public double AverageWaitTime { get; init; }
    public double AverageEfficiency { get; init; }
    public double BatchingRate { get; init; }
    public double RequestRate { get; init; }
    public long TotalRequests { get; init; }
    public long SuccessfulRequests { get; init; }
    public long FailedRequests { get; init; }
    public long TotalBatchedRequests { get; init; }
    public long BatchExecutions { get; init; }
}