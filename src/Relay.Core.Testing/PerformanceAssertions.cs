using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// Performance-related assertion helpers.
/// </summary>
public static class PerformanceAssertions
{
    /// <summary>
    /// Asserts that a task completes within the specified maximum duration.
    /// </summary>
    /// <param name="task">The task to test.</param>
    /// <param name="maxDuration">The maximum allowed duration.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static async Task ShouldCompleteWithinAsync(this Task task, TimeSpan maxDuration)
    {
        var startTime = DateTime.UtcNow;
        await task;
        var actualDuration = DateTime.UtcNow - startTime;

        if (actualDuration > maxDuration)
        {
            throw new Xunit.Sdk.XunitException(
                $"Task did not complete within {maxDuration.TotalMilliseconds}ms. " +
                $"Actual duration: {actualDuration.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Asserts that a task completes within the specified maximum duration.
    /// </summary>
    /// <typeparam name="T">The type of the task result.</typeparam>
    /// <param name="task">The task to test.</param>
    /// <param name="maxDuration">The maximum allowed duration.</param>
    /// <returns>The task result.</returns>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static async Task<T> ShouldCompleteWithinAsync<T>(this Task<T> task, TimeSpan maxDuration)
    {
        var startTime = DateTime.UtcNow;
        var result = await task;
        var actualDuration = DateTime.UtcNow - startTime;

        if (actualDuration > maxDuration)
        {
            throw new Xunit.Sdk.XunitException(
                $"Task did not complete within {maxDuration.TotalMilliseconds}ms. " +
                $"Actual duration: {actualDuration.TotalMilliseconds}ms");
        }

        return result;
    }

    /// <summary>
    /// Asserts that an action allocates less than the specified maximum bytes.
    /// </summary>
    /// <param name="action">The action to test.</param>
    /// <param name="maxBytes">The maximum allowed memory allocation in bytes.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldAllocateLessThan(this Action action, long maxBytes)
    {
        // Note: This is a simplified implementation. A real implementation would use
        // memory profiling tools like BenchmarkDotNet or custom memory tracking.
        // For now, we'll just execute the action without actual memory tracking.

        var startMemory = GC.GetTotalMemory(true);
        action();
        var endMemory = GC.GetTotalMemory(false);

        var allocatedBytes = endMemory - startMemory;

        if (allocatedBytes > maxBytes)
        {
            throw new Xunit.Sdk.XunitException(
                $"Action allocated more than {maxBytes} bytes. " +
                $"Actual allocation: {allocatedBytes} bytes");
        }
    }

    /// <summary>
    /// Asserts that a load test result has the specified minimum throughput.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <param name="minRequestsPerSecond">The minimum required requests per second.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveThroughputOf(this LoadTestResult result, double minRequestsPerSecond)
    {
        var actualThroughput = result.RequestsPerSecond;

        if (actualThroughput < minRequestsPerSecond)
        {
            throw new Xunit.Sdk.XunitException(
                $"Load test throughput {actualThroughput:F2} req/sec is below the minimum threshold of {minRequestsPerSecond:F2} req/sec. " +
                $"Total requests: {result.SuccessfulRequests + result.FailedRequests}, " +
                $"Duration: {result.TotalDuration.TotalSeconds:F2}s, " +
                $"Success rate: {result.SuccessRate:P2}");
        }
    }

    /// <summary>
    /// Asserts that a load test result has a success rate above the specified minimum.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <param name="minSuccessRate">The minimum required success rate (0.0 to 1.0).</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveSuccessRateAbove(this LoadTestResult result, double minSuccessRate)
    {
        var actualSuccessRate = result.SuccessRate;

        if (actualSuccessRate < minSuccessRate)
        {
            throw new Xunit.Sdk.XunitException(
                $"Load test success rate {actualSuccessRate:P2} is below the minimum threshold of {minSuccessRate:P2}. " +
                $"Successful: {result.SuccessfulRequests}, Failed: {result.FailedRequests}, " +
                $"Total: {result.SuccessfulRequests + result.FailedRequests}");
        }
    }

    /// <summary>
    /// Asserts that a load test result has a P95 response time below the specified maximum.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <param name="maxP95ResponseTime">The maximum allowed P95 response time.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveP95ResponseTimeBelow(this LoadTestResult result, TimeSpan maxP95ResponseTime)
    {
        var actualP95 = TimeSpan.FromMilliseconds(result.P95ResponseTime);

        if (actualP95 > maxP95ResponseTime)
        {
            throw new Xunit.Sdk.XunitException(
                $"Load test P95 response time {actualP95.TotalMilliseconds:F2}ms exceeds the maximum threshold of {maxP95ResponseTime.TotalMilliseconds:F2}ms. " +
                $"Average: {result.AverageResponseTime:F2}ms, Median: {result.MedianResponseTime:F2}ms, " +
                $"P99: {result.P99ResponseTime:F2}ms");
        }
    }

    /// <summary>
    /// Asserts that a load test result has an average response time below the specified maximum.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <param name="maxAverageResponseTime">The maximum allowed average response time.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveAverageResponseTimeBelow(this LoadTestResult result, TimeSpan maxAverageResponseTime)
    {
        var actualAverage = TimeSpan.FromMilliseconds(result.AverageResponseTime);

        if (actualAverage > maxAverageResponseTime)
        {
            throw new Xunit.Sdk.XunitException(
                $"Load test average response time {actualAverage.TotalMilliseconds:F2}ms exceeds the maximum threshold of {maxAverageResponseTime.TotalMilliseconds:F2}ms. " +
                $"Median: {result.MedianResponseTime:F2}ms, P95: {result.P95ResponseTime:F2}ms, " +
                $"P99: {result.P99ResponseTime:F2}ms");
        }
    }

    /// <summary>
    /// Asserts that a load test completed without any failed requests.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldHaveNoFailedRequests(this LoadTestResult result)
    {
        if (result.FailedRequests > 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Load test had {result.FailedRequests} failed request(s). " +
                $"Successful: {result.SuccessfulRequests}, Total: {result.SuccessfulRequests + result.FailedRequests}, " +
                $"Success rate: {result.SuccessRate:P2}");
        }
    }

    /// <summary>
    /// Asserts that a load test completed within the specified maximum duration.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <param name="maxDuration">The maximum allowed duration.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public static void ShouldCompleteWithin(this LoadTestResult result, TimeSpan maxDuration)
    {
        if (result.TotalDuration > maxDuration)
        {
            throw new Xunit.Sdk.XunitException(
                $"Load test duration {result.TotalDuration.TotalSeconds:F2}s exceeds the maximum threshold of {maxDuration.TotalSeconds:F2}s. " +
                $"Requests: {result.SuccessfulRequests + result.FailedRequests}, " +
                $"Throughput: {result.RequestsPerSecond:F2} req/sec");
        }
    }
}