#if IncludeMSTest
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Relay.Core.Testing;

/// <summary>
/// MSTest-specific assertion helpers that integrate Relay testing assertions with MSTest's Assert class.
/// </summary>
public static class RelayMSTestAssertions
{
    /// <summary>
    /// Asserts that a request was handled by the mediator.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="testRelay">The TestRelay instance.</param>
    /// <param name="predicate">Optional predicate to filter requests.</param>
    public static void ShouldHaveHandled<TRequest>(this TestRelay testRelay, Func<TRequest, bool>? predicate = null)
        where TRequest : class
    {
        var result = testRelay.ShouldHaveHandled(predicate);
        Assert.IsTrue(result.IsSuccessful, result.ErrorMessage);
    }

    /// <summary>
    /// Asserts that a notification was published.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="testRelay">The TestRelay instance.</param>
    /// <param name="predicate">Optional predicate to filter notifications.</param>
    public static void ShouldHavePublished<TNotification>(this TestRelay testRelay, Func<TNotification, bool>? predicate = null)
        where TNotification : class
    {
        var result = testRelay.ShouldHavePublished(predicate);
        Assert.IsTrue(result.IsSuccessful, result.ErrorMessage);
    }

    /// <summary>
    /// Asserts that notifications were published in the specified order.
    /// </summary>
    /// <param name="testRelay">The TestRelay instance.</param>
    /// <param name="expectedOrder">The expected notification types in order.</param>
    public static void ShouldHavePublishedInOrder(this TestRelay testRelay, params Type[] expectedOrder)
    {
        var result = testRelay.ShouldHavePublishedInOrder(expectedOrder);
        Assert.IsTrue(result.IsSuccessful, result.ErrorMessage);
    }

    /// <summary>
    /// Asserts that requests were handled in the specified order.
    /// </summary>
    /// <param name="testRelay">The TestRelay instance.</param>
    /// <param name="expectedOrder">The expected request types in order.</param>
    public static void ShouldHaveHandledInOrder(this TestRelay testRelay, params Type[] expectedOrder)
    {
        var result = testRelay.ShouldHaveHandledInOrder(expectedOrder);
        Assert.IsTrue(result.IsSuccessful, result.ErrorMessage);
    }

    /// <summary>
    /// Asserts that no requests of the specified type were handled.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="testRelay">The TestRelay instance.</param>
    public static void ShouldNotHaveHandled<TRequest>(this TestRelay testRelay)
        where TRequest : class
    {
        var result = testRelay.ShouldNotHaveHandled<TRequest>();
        Assert.IsTrue(result.IsSuccessful, result.ErrorMessage);
    }

    /// <summary>
    /// Asserts that no notifications of the specified type were published.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="testRelay">The TestRelay instance.</param>
    public static void ShouldNotHavePublished<TNotification>(this TestRelay testRelay)
        where TNotification : class
    {
        var result = testRelay.ShouldNotHavePublished<TNotification>();
        Assert.IsTrue(result.IsSuccessful, result.ErrorMessage);
    }

    /// <summary>
    /// Asserts that a scenario completed successfully.
    /// </summary>
    /// <param name="result">The scenario result.</param>
    public static void ShouldBeSuccessful(this ScenarioResult result)
    {
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful, $"Scenario failed: {result.ErrorMessage}");
        Assert.IsNull(result.Exception);
    }

    /// <summary>
    /// Asserts that a scenario failed.
    /// </summary>
    /// <param name="result">The scenario result.</param>
    public static void ShouldHaveFailed(this ScenarioResult result)
    {
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsSuccessful, "Scenario was expected to fail but succeeded");
        Assert.IsNotNull(result.Exception);
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
        Assert.IsInstanceOfType(result.Exception, typeof(TException));
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
        Assert.IsNotNull(result);

        if (maxAverageResponseTime.HasValue)
        {
            Assert.IsTrue(result.AverageResponseTime <= maxAverageResponseTime.Value,
                $"Average response time {result.AverageResponseTime} exceeds maximum {maxAverageResponseTime.Value}");
        }

        if (maxErrorRate.HasValue)
        {
            var errorRate = result.TotalRequests == 0 ? 0 : (double)result.FailedRequests / result.TotalRequests;
            Assert.IsTrue(errorRate <= maxErrorRate.Value,
                $"Error rate {errorRate:P2} exceeds maximum {maxErrorRate.Value:P2}");
        }

        if (minRequestsPerSecond.HasValue)
        {
            Assert.IsTrue(result.RequestsPerSecond >= minRequestsPerSecond.Value,
                $"Requests per second {result.RequestsPerSecond:F2} is below minimum {minRequestsPerSecond.Value:F2}");
        }
    }
}
#endif