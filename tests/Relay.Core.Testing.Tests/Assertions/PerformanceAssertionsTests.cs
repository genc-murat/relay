using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing.Tests.Assertions;

public class PerformanceAssertionsTests
{
    // Tests for ShouldCompleteWithinAsync(Task, TimeSpan)
    [Fact]
    public async Task ShouldCompleteWithinAsync_Task_CompletesWithinTime_Passes()
    {
        // Arrange
        var task = Task.Delay(10); // 10ms delay
        var maxDuration = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        await task.ShouldCompleteWithinAsync(maxDuration);
    }

    [Fact]
    public async Task ShouldCompleteWithinAsync_Task_ExceedsTime_Throws()
    {
        // Arrange
        var task = Task.Delay(200); // 200ms delay
        var maxDuration = TimeSpan.FromMilliseconds(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(
            async () => await PerformanceAssertions.ShouldCompleteWithinAsync(task, maxDuration));

        Assert.Contains("did not complete within", exception.Message);
    }

    // Tests for ShouldCompleteWithinAsync<T>(Task<T>, TimeSpan)
    [Fact]
    public async Task ShouldCompleteWithinAsync_Generic_CompletesWithinTime_ReturnsResult()
    {
        // Arrange
        var task = Task.FromResult(42);
        var maxDuration = TimeSpan.FromMilliseconds(100);

        // Act
        var result = await task.ShouldCompleteWithinAsync(maxDuration);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ShouldCompleteWithinAsync_Generic_ExceedsTime_Throws()
    {
        // Arrange
        var task = Task.Delay(200).ContinueWith(_ => 42); // 200ms delay then return 42
        var maxDuration = TimeSpan.FromMilliseconds(50);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(
            async () => await PerformanceAssertions.ShouldCompleteWithinAsync(task, maxDuration));

        Assert.Contains("did not complete within", exception.Message);
    }

    // Tests for ShouldAllocateLessThan(Action, long, int)
    [Fact]
    public void ShouldAllocateLessThan_AllocatesLessThanLimit_Passes()
    {
        // Arrange
        Action action = () => { };
        long maxBytes = 10000; // Higher limit for empty action
        int iterations = 3;

        // Act & Assert
        action.ShouldAllocateLessThan(maxBytes, iterations);
    }

    [Fact]
    public void ShouldAllocateLessThan_AllocatesMoreThanLimit_Throws()
    {
        // Arrange
        Action action = () => { var array = new byte[2000]; }; // Allocate 2000 bytes
        long maxBytes = 1000;
        int iterations = 3;

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => action.ShouldAllocateLessThan(maxBytes, iterations));

        Assert.Contains("allocated more than", exception.Message);
    }

    [Fact]
    public void ShouldAllocateLessThan_InvalidIterations_ThrowsArgumentException()
    {
        // Arrange
        Action action = () => { };
        long maxBytes = 1000;
        int iterations = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => action.ShouldAllocateLessThan(maxBytes, iterations));

        Assert.Equal("iterations", exception.ParamName);
    }

    // Tests for ShouldAllocateLessThanAsync(Func<Task>, long, int)
    [Fact]
    public async Task ShouldAllocateLessThanAsync_AllocatesLessThanLimit_Passes()
    {
        // Arrange
        Func<Task> action = async () => { await Task.CompletedTask; };
        long maxBytes = 100000; // 100KB limit for minimal async operation
        int iterations = 3;

        // Act & Assert
        await action.ShouldAllocateLessThanAsync(maxBytes, iterations);
    }

    [Fact]
    public async Task ShouldAllocateLessThanAsync_AllocatesMoreThanLimit_Throws()
    {
        // Arrange
        Func<Task> action = async () => { await Task.Delay(1); var array = new byte[2000]; };
        long maxBytes = 1000;
        int iterations = 3;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Xunit.Sdk.XunitException>(
            async () => await action.ShouldAllocateLessThanAsync(maxBytes, iterations));

        Assert.Contains("allocated more than", exception.Message);
    }

    [Fact]
    public async Task ShouldAllocateLessThanAsync_InvalidIterations_ThrowsArgumentException()
    {
        // Arrange
        Func<Task> action = async () => await Task.Delay(1);
        long maxBytes = 1000;
        int iterations = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await action.ShouldAllocateLessThanAsync(maxBytes, iterations));

        Assert.Equal("iterations", exception.ParamName);
    }

    // Tests for LoadTestResult assertions
    [Fact]
    public void ShouldHaveThroughputOf_ThroughputAboveMinimum_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 100,
            FailedRequests = 0,
            TotalDuration = TimeSpan.FromSeconds(1)
        };
        double minRequestsPerSecond = 50;

        // Act & Assert
        result.ShouldHaveThroughputOf(minRequestsPerSecond);
    }

    [Fact]
    public void ShouldHaveThroughputOf_ThroughputBelowMinimum_Throws()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 10,
            FailedRequests = 0,
            TotalDuration = TimeSpan.FromSeconds(1)
        };
        double minRequestsPerSecond = 50;

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => result.ShouldHaveThroughputOf(minRequestsPerSecond));

        Assert.Contains("below the minimum threshold", exception.Message);
    }

    [Fact]
    public void ShouldHaveSuccessRateAbove_SuccessRateAboveMinimum_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 95,
            FailedRequests = 5
        };
        double minSuccessRate = 0.9;

        // Act & Assert
        result.ShouldHaveSuccessRateAbove(minSuccessRate);
    }

    [Fact]
    public void ShouldHaveSuccessRateAbove_SuccessRateBelowMinimum_Throws()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 50,
            FailedRequests = 50
        };
        double minSuccessRate = 0.9;

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => PerformanceAssertions.ShouldHaveSuccessRateAbove(result, minSuccessRate));

        Assert.Contains("below the minimum threshold", exception.Message);
    }

    [Fact]
    public void ShouldHaveP95ResponseTimeBelow_P95BelowMaximum_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            P95ResponseTime = 100 // 100ms
        };
        var maxP95ResponseTime = TimeSpan.FromMilliseconds(200);

        // Act & Assert
        result.ShouldHaveP95ResponseTimeBelow(maxP95ResponseTime);
    }

    [Fact]
    public void ShouldHaveP95ResponseTimeBelow_P95AboveMaximum_Throws()
    {
        // Arrange
        var result = new LoadTestResult
        {
            P95ResponseTime = 300 // 300ms
        };
        var maxP95ResponseTime = TimeSpan.FromMilliseconds(200);

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => PerformanceAssertions.ShouldHaveP95ResponseTimeBelow(result, maxP95ResponseTime));

        Assert.Contains("exceeds the maximum threshold", exception.Message);
    }

    [Fact]
    public void ShouldHaveAverageResponseTimeBelow_AverageBelowMaximum_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            AverageResponseTime = 50 // 50ms
        };
        var maxAverageResponseTime = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        result.ShouldHaveAverageResponseTimeBelow(maxAverageResponseTime);
    }

    [Fact]
    public void ShouldHaveAverageResponseTimeBelow_AverageAboveMaximum_Throws()
    {
        // Arrange
        var result = new LoadTestResult
        {
            AverageResponseTime = 150 // 150ms
        };
        var maxAverageResponseTime = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => PerformanceAssertions.ShouldHaveAverageResponseTimeBelow(result, maxAverageResponseTime));

        Assert.Contains("exceeds the maximum threshold", exception.Message);
    }

    [Fact]
    public void ShouldHaveNoFailedRequests_NoFailedRequests_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 100,
            FailedRequests = 0
        };

        // Act & Assert
        result.ShouldHaveNoFailedRequests();
    }

    [Fact]
    public void ShouldHaveNoFailedRequests_HasFailedRequests_Throws()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 95,
            FailedRequests = 5
        };

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => result.ShouldHaveNoFailedRequests());

        Assert.Contains("failed request(s)", exception.Message);
    }

    [Fact]
    public void ShouldCompleteWithin_LoadTest_CompletesWithinTime_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            TotalDuration = TimeSpan.FromSeconds(5)
        };
        var maxDuration = TimeSpan.FromSeconds(10);

        // Act & Assert
        result.ShouldCompleteWithin(maxDuration);
    }

    [Fact]
    public void ShouldCompleteWithin_LoadTest_ExceedsTime_Throws()
    {
        // Arrange
        var result = new LoadTestResult
        {
            TotalDuration = TimeSpan.FromSeconds(15)
        };
        var maxDuration = TimeSpan.FromSeconds(10);

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(
            () => result.ShouldCompleteWithin(maxDuration));

        Assert.Contains("exceeds the maximum threshold", exception.Message);
    }
}