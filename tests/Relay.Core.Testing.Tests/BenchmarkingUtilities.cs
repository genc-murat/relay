using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Utilities for benchmarking request execution performance
/// </summary>
public static class BenchmarkingUtilities
{
    /// <summary>
    /// Measures the performance of a request execution
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="relay">The relay instance</param>
    /// <param name="request">The request to measure</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance benchmark results</returns>
    public static async Task<BenchmarkResult> BenchmarkAsync<TResponse>(
        IRelay relay,
        IRequest<TResponse> request,
        int iterations = 100,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TimeSpan>();
        var startTime = DateTimeOffset.UtcNow;
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iterationStart = DateTimeOffset.UtcNow;
            await relay.SendAsync(request, cancellationToken);
            var iterationEnd = DateTimeOffset.UtcNow;

            results.Add(iterationEnd - iterationStart);
        }

        var finalMemory = GC.GetTotalMemory(false);
        var totalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum + time);
        var avgTime = TimeSpan.FromTicks(totalTime.Ticks / iterations);
        var minTime = results.Min();
        var maxTime = results.Max();

        // Calculate standard deviation
        var avgTicks = totalTime.Ticks / iterations;
        var variance = results.Select(t => Math.Pow(t.Ticks - avgTicks, 2)).Average();
        var stdDev = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        return new BenchmarkResult
        {
            RequestType = typeof(TResponse).Name,
            HandlerType = "Test",
            Iterations = iterations,
            TotalTime = totalTime,
            MinTime = minTime,
            MaxTime = maxTime,
            StandardDeviation = stdDev,
            TotalAllocatedBytes = Math.Max(0, finalMemory - initialMemory),
            Timestamp = startTime
        };
    }

    /// <summary>
    /// Measures the performance of a void request execution
    /// </summary>
    /// <param name="relay">The relay instance</param>
    /// <param name="request">The request to measure</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance benchmark results</returns>
    public static async Task<BenchmarkResult> BenchmarkAsync(
        IRelay relay,
        IRequest request,
        int iterations = 100,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TimeSpan>();
        var startTime = DateTimeOffset.UtcNow;
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iterationStart = DateTimeOffset.UtcNow;
            await relay.SendAsync(request, cancellationToken);
            var iterationEnd = DateTimeOffset.UtcNow;

            results.Add(iterationEnd - iterationStart);
        }

        var finalMemory = GC.GetTotalMemory(false);
        var totalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum + time);
        var avgTime = TimeSpan.FromTicks(totalTime.Ticks / iterations);
        var minTime = results.Min();
        var maxTime = results.Max();

        // Calculate standard deviation
        var avgTicks = totalTime.Ticks / iterations;
        var variance = results.Select(t => Math.Pow(t.Ticks - avgTicks, 2)).Average();
        var stdDev = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        return new BenchmarkResult
        {
            RequestType = request.GetType().Name,
            HandlerType = "Test",
            Iterations = iterations,
            TotalTime = totalTime,
            MinTime = minTime,
            MaxTime = maxTime,
            StandardDeviation = stdDev,
            TotalAllocatedBytes = Math.Max(0, finalMemory - initialMemory),
            Timestamp = startTime
        };
    }
}

/// <summary>
/// Represents the result of a benchmark operation
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// The type of request that was benchmarked
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// The type of handler that processed the request
    /// </summary>
    public string HandlerType { get; set; } = string.Empty;

    /// <summary>
    /// Number of iterations performed
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Total time taken for all iterations
    /// </summary>
    public TimeSpan TotalTime { get; set; }

    /// <summary>
    /// Minimum time for a single iteration
    /// </summary>
    public TimeSpan MinTime { get; set; }

    /// <summary>
    /// Maximum time for a single iteration
    /// </summary>
    public TimeSpan MaxTime { get; set; }

    /// <summary>
    /// Standard deviation of iteration times
    /// </summary>
    public TimeSpan StandardDeviation { get; set; }

    /// <summary>
    /// Total bytes allocated during the benchmark
    /// </summary>
    public long TotalAllocatedBytes { get; set; }

    /// <summary>
    /// When the benchmark was started
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
