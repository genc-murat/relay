using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Collects performance metrics for operations including timing, memory usage, and allocations.
/// </summary>
public class MetricsCollector
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    /// <summary>
    /// Collects metrics for the specified operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The operation to measure.</param>
    /// <returns>The collected operation metrics.</returns>
    public OperationMetrics Collect(string operationName, Action operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var startTime = DateTime.UtcNow;
        var startTimestamp = GetTimestamp();
        var startMemory = GC.GetTotalMemory(false);
        long endTimestamp = 0;
        long endMemory = 0;
        DateTime endTime;

        try
        {
            operation();
        }
        finally
        {
            endTimestamp = GetTimestamp();
            endMemory = GC.GetTotalMemory(false);
            endTime = DateTime.UtcNow;
        }

        var duration = TimeSpan.FromTicks((long)((endTimestamp - startTimestamp) * TimestampToTicks));
        var memoryUsed = Math.Max(0, endMemory - startMemory);

        // Note: Allocation tracking requires more advanced profiling.
        // For now, we use memory delta as a proxy.
        var allocations = 0L; // Placeholder - would need profiler integration

        return new OperationMetrics
        {
            OperationName = operationName,
            Duration = duration,
            MemoryUsed = memoryUsed,
            Allocations = allocations,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// Collects metrics for the specified asynchronous operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The asynchronous operation to measure.</param>
    /// <returns>The collected operation metrics.</returns>
    public async Task<OperationMetrics> CollectAsync(string operationName, Func<Task> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var startTime = DateTime.UtcNow;
        var startTimestamp = GetTimestamp();
        var startMemory = GC.GetTotalMemory(false);
        long endTimestamp = 0;
        long endMemory = 0;
        DateTime endTime;

        try
        {
            await operation();
        }
        finally
        {
            endTimestamp = GetTimestamp();
            endMemory = GC.GetTotalMemory(false);
            endTime = DateTime.UtcNow;
        }

        var duration = TimeSpan.FromTicks((long)((endTimestamp - startTimestamp) * TimestampToTicks));
        var memoryUsed = Math.Max(0, endMemory - startMemory);
        var allocations = 0L; // Placeholder

        return new OperationMetrics
        {
            OperationName = operationName,
            Duration = duration,
            MemoryUsed = memoryUsed,
            Allocations = allocations,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// Collects metrics for the specified operation that returns a value.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The operation to measure.</param>
    /// <returns>A tuple containing the result and the collected metrics.</returns>
    public (T Result, OperationMetrics Metrics) Collect<T>(string operationName, Func<T> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var startTime = DateTime.UtcNow;
        var startTimestamp = GetTimestamp();
        var startMemory = GC.GetTotalMemory(false);
        long endTimestamp = 0;
        long endMemory = 0;
        DateTime endTime;
        T result = default!;

        try
        {
            result = operation();
        }
        finally
        {
            endTimestamp = GetTimestamp();
            endMemory = GC.GetTotalMemory(false);
            endTime = DateTime.UtcNow;
        }

        var duration = TimeSpan.FromTicks((long)((endTimestamp - startTimestamp) * TimestampToTicks));
        var memoryUsed = Math.Max(0, endMemory - startMemory);
        var allocations = 0L;

        var metrics = new OperationMetrics
        {
            OperationName = operationName,
            Duration = duration,
            MemoryUsed = memoryUsed,
            Allocations = allocations,
            StartTime = startTime,
            EndTime = endTime
        };

        return (result, metrics);
    }

    /// <summary>
    /// Collects metrics for the specified asynchronous operation that returns a value.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">The asynchronous operation to measure.</param>
    /// <returns>A tuple containing the result and the collected metrics.</returns>
    public async Task<(T Result, OperationMetrics Metrics)> CollectAsync<T>(string operationName, Func<Task<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var startTime = DateTime.UtcNow;
        var startTimestamp = GetTimestamp();
        var startMemory = GC.GetTotalMemory(false);
        long endTimestamp = 0;
        long endMemory = 0;
        DateTime endTime;
        T result = default!;

        try
        {
            result = await operation();
        }
        finally
        {
            endTimestamp = GetTimestamp();
            endMemory = GC.GetTotalMemory(false);
            endTime = DateTime.UtcNow;
        }

        var duration = TimeSpan.FromTicks((long)((endTimestamp - startTimestamp) * TimestampToTicks));
        var memoryUsed = Math.Max(0, endMemory - startMemory);
        var allocations = 0L;

        var metrics = new OperationMetrics
        {
            OperationName = operationName,
            Duration = duration,
            MemoryUsed = memoryUsed,
            Allocations = allocations,
            StartTime = startTime,
            EndTime = endTime
        };

        return (result, metrics);
    }

    private static long GetTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }
}