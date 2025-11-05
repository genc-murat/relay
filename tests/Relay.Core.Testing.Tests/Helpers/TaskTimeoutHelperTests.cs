using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class TaskTimeoutHelperTests
{
    [Fact]
    public async Task WithTimeout_Generic_CompletesBeforeTimeout_ReturnsResult()
    {
        // Arrange
        var expectedResult = "Success";
        var task = Task.FromResult(expectedResult);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = await TaskTimeoutHelper.WithTimeout(task, timeout);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task WithTimeout_Generic_TimesOut_ThrowsTimeoutException()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => "Result");
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.WithTimeout(task, timeout));

        Assert.Contains("timed out after 100ms", exception.Message);
    }

    [Fact]
    public async Task WithTimeout_Generic_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskTimeoutHelper.WithTimeout<string>(null, timeout));
    }

    [Fact]
    public async Task WithTimeout_Generic_ZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var task = Task.FromResult("test");
        var timeout = TimeSpan.Zero;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TaskTimeoutHelper.WithTimeout(task, timeout));
    }

    [Fact]
    public async Task WithTimeout_Generic_NegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var task = Task.FromResult("test");
        var timeout = TimeSpan.FromSeconds(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TaskTimeoutHelper.WithTimeout(task, timeout));
    }

    [Fact]
    public async Task WithTimeout_Generic_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<string>();
        cts.Token.Register(() => tcs.TrySetCanceled(cts.Token));
        var task = tcs.Task;
        var timeout = TimeSpan.FromSeconds(5);

        // Cancel the token
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            TaskTimeoutHelper.WithTimeout(task, timeout, cts.Token));
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_CompletesBeforeTimeout_CompletesSuccessfully()
    {
        // Arrange
        var task = Task.Delay(50);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        await TaskTimeoutHelper.WithTimeout(task, timeout);

        // Assert - No exception thrown
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_TimesOut_ThrowsTimeoutException()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2));
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.WithTimeout(task, timeout));

        Assert.Contains("timed out after 100ms", exception.Message);
    }

    [Fact]
    public async Task WithTimeout_NonGeneric_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskTimeoutHelper.WithTimeout(null, timeout));
    }

    [Fact]
    public async Task ShouldCompleteWithin_Generic_CompletesBeforeTimeout_ReturnsResult()
    {
        // Arrange
        var expectedResult = 42;
        var task = Task.FromResult(expectedResult);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = await TaskTimeoutHelper.ShouldCompleteWithin(task, timeout);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ShouldCompleteWithin_Generic_TimesOut_ThrowsTimeoutException()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => 42);
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.ShouldCompleteWithin(task, timeout));
    }

    [Fact]
    public async Task ShouldCompleteWithin_NonGeneric_CompletesBeforeTimeout_CompletesSuccessfully()
    {
        // Arrange
        var task = Task.Delay(50);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        await TaskTimeoutHelper.ShouldCompleteWithin(task, timeout);

        // Assert - No exception thrown
    }

    [Fact]
    public async Task ShouldTimeoutWithin_Generic_TaskCompletesBeforeTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var task = Task.FromResult("Completed");
        var timeout = TimeSpan.FromSeconds(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout));

        Assert.Contains("completed before expected timeout", exception.Message);
    }

    [Fact]
    public async Task ShouldTimeoutWithin_Generic_TaskTimesOut_ReturnsSuccessfully()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => "Result");
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act - Should complete without throwing since task times out as expected
        await TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout);

        // Assert - No exception thrown
    }

    [Fact]
    public async Task ShouldTimeoutWithin_Generic_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskTimeoutHelper.ShouldTimeoutWithin<string>(null, timeout));
    }

    [Fact]
    public async Task ShouldTimeoutWithin_Generic_ZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var task = Task.FromResult("test");
        var timeout = TimeSpan.Zero;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout));
    }

    [Fact]
    public async Task ShouldTimeoutWithin_NonGeneric_TaskCompletesBeforeTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var task = Task.CompletedTask;
        var timeout = TimeSpan.FromSeconds(1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout));

        Assert.Contains("completed before expected timeout", exception.Message);
    }

    [Fact]
    public async Task ShouldTimeoutWithin_NonGeneric_TaskTimesOut_ReturnsSuccessfully()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2));
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act
        await TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout);

        // Assert - No exception thrown
    }

    [Fact]
    public async Task MeasureExecutionTime_Generic_ReturnsResultAndExecutionTime()
    {
        // Arrange
        var expectedResult = "Test Result";
        var task = Task.FromResult(expectedResult);

        // Act
        var (result, executionTime) = await TaskTimeoutHelper.MeasureExecutionTime(task);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.True(executionTime >= TimeSpan.Zero);
        Assert.True(executionTime < TimeSpan.FromSeconds(1)); // Should be very fast
    }

    [Fact]
    public async Task MeasureExecutionTime_Generic_NullTask_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskTimeoutHelper.MeasureExecutionTime<string>(null));
    }

    [Fact]
    public async Task MeasureExecutionTime_NonGeneric_ReturnsExecutionTime()
    {
        // Arrange
        var task = Task.Delay(50);

        // Act
        var executionTime = await TaskTimeoutHelper.MeasureExecutionTime(task);

        // Assert
        Assert.True(executionTime >= TimeSpan.FromMilliseconds(40)); // Allow some tolerance
        Assert.True(executionTime < TimeSpan.FromSeconds(2)); // Allow more time for slower systems
    }

    [Fact]
    public async Task MeasureExecutionTime_NonGeneric_NullTask_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskTimeoutHelper.MeasureExecutionTime(null));
    }

    [Fact]
    public async Task Delay_ReturnsTaskThatCompletesAfterDelay()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        // Act
        await TaskTimeoutHelper.Delay(delay);

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed >= delay);
        Assert.True(elapsed < delay + TimeSpan.FromMilliseconds(100)); // Allow more tolerance
    }

    [Fact]
    public async Task Delay_WithCancellation_RespectsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var delay = TimeSpan.FromSeconds(10);

        cts.CancelAfter(100); // Cancel after 100ms

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            TaskTimeoutHelper.Delay(delay, cts.Token));
    }

    [Fact]
    public async Task Delay_Generic_WithCancellation_RespectsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var delay = TimeSpan.FromSeconds(10);
        var result = 123;

        cts.CancelAfter(100); // Cancel after 100ms

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            TaskTimeoutHelper.Delay(delay, result, cts.Token));
    }

    [Fact]
    public async Task IntegrationTest_WithTimeoutAndCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var task = Task.Delay(TimeSpan.FromSeconds(5), cts.Token).ContinueWith(_ => "Result");
        var timeout = TimeSpan.FromSeconds(10); // Timeout longer than task

        // Act
        var result = await TaskTimeoutHelper.WithTimeout(task, timeout, cts.Token);

        // Assert
        Assert.Equal("Result", result);
    }

    [Fact]
    public async Task IntegrationTest_TimeoutTakesPrecedenceOverCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var task = Task.Delay(TimeSpan.FromSeconds(10), cts.Token).ContinueWith(_ => "Result");
        var timeout = TimeSpan.FromMilliseconds(100); // Timeout shorter than task

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.WithTimeout(task, timeout, cts.Token));

        Assert.Contains("timed out after 100ms", exception.Message);
    }
}