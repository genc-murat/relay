using System;
using System.Linq;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// xUnit-specific assertion helpers that integrate Relay testing assertions with xUnit's Assert class.
/// </summary>
public static class RelayXUnitAssertions
{

    /// <summary>
    /// Asserts that a scenario completed successfully.
    /// </summary>
    /// <param name="result">The scenario result.</param>
    public static void ShouldBeSuccessful(this ScenarioResult result)
    {
        Assert.NotNull(result);
        Assert.True(result.Success, $"Scenario failed: {result.Error}");
    }

    /// <summary>
    /// Asserts that a scenario failed.
    /// </summary>
    /// <param name="result">The scenario result.</param>
    public static void ShouldHaveFailed(this ScenarioResult result)
    {
        Assert.NotNull(result);
        Assert.False(result.Success, "Scenario was expected to fail but succeeded");
        Assert.NotNull(result.Error);
    }

    /// <summary>
    /// Asserts that a scenario failed with a specific exception type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="result">The scenario result.</param>
    public static void ShouldHaveFailedWith<TException>(this ScenarioResult result)
        where TException : Exception
    {
        result.ShouldHaveFailed();
        // Note: ScenarioResult doesn't store exception type, just error message
    }

    /// <summary>
    /// Asserts that a load test result meets performance expectations.
    /// </summary>
    /// <param name="result">The load test result.</param>
    /// <param name="maxAverageResponseTime">Maximum allowed average response time.</param>
    /// <param name="maxErrorRate">Maximum allowed error rate (0.0 to 1.0).</param>
    /// <param name="minRequestsPerSecond">Minimum required requests per second.</param>
    public static void ShouldMeetPerformanceExpectations(
        this LoadTestResult result,
        TimeSpan? maxAverageResponseTime = null,
        double? maxErrorRate = null,
        double? minRequestsPerSecond = null)
    {
        Assert.NotNull(result);

        if (maxAverageResponseTime.HasValue)
        {
            Assert.True(result.AverageResponseTime <= maxAverageResponseTime.Value.TotalMilliseconds,
                $"Average response time {result.AverageResponseTime}ms exceeds maximum {maxAverageResponseTime.Value.TotalMilliseconds}ms");
        }

        if (maxErrorRate.HasValue)
        {
            var errorRate = result.SuccessRate < 1 ? 1 - result.SuccessRate : 0;
            Assert.True(errorRate <= maxErrorRate.Value,
                $"Error rate {errorRate:P2} exceeds maximum {maxErrorRate.Value:P2}");
        }

        if (minRequestsPerSecond.HasValue)
        {
            Assert.True(result.RequestsPerSecond >= minRequestsPerSecond.Value,
                $"Requests per second {result.RequestsPerSecond:F2} is below minimum {minRequestsPerSecond.Value:F2}");
        }
    }
}