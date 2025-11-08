using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class AsyncTestUtilitiesTests
{
    [Fact]
    public async Task TaskTimeoutHelper_WithTimeout_CompletesWithinTimeout()
    {
        // Arrange
        var task = Task.Delay(100);

        // Act
        await TaskTimeoutHelper.WithTimeout(task, TimeSpan.FromSeconds(1));

        // Assert - No exception thrown
    }

    [Fact]
    public async Task TaskTimeoutHelper_WithTimeout_ThrowsOnTimeout()
    {
        // Arrange
        var task = Task.Delay(1000);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            TaskTimeoutHelper.WithTimeout(task, TimeSpan.FromMilliseconds(100)));
    }

    [Fact]
    public async Task TaskTimeoutHelper_ShouldCompleteWithin_Succeeds()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act
        var result = await TaskTimeoutHelper.ShouldCompleteWithin(task, TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task TaskTimeoutHelper_ShouldTimeoutWithin_Succeeds()
    {
        // Arrange
        var task = Task.Delay(200);

        // Act & Assert - Should not throw because task times out as expected
        await TaskTimeoutHelper.ShouldTimeoutWithin(task, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task TaskTimeoutHelper_MeasureExecutionTime_ReturnsCorrectTime()
    {
        // Arrange
        var task = Task.Delay(100);

        // Act
        var executionTime = await TaskTimeoutHelper.MeasureExecutionTime(task);

        // Assert
        Assert.True(executionTime >= TimeSpan.FromMilliseconds(90));
        Assert.True(executionTime <= TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldCompleteWithin_Succeeds()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act
        var result = await task.ShouldCompleteWithin(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldNotThrow_Succeeds()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act
        var result = await task.ShouldNotThrow();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldThrow_Succeeds()
    {
        // Arrange
        var task = Task.FromException(new InvalidOperationException("Test exception"));

        // Act
        var exception = await task.ShouldThrow<InvalidOperationException>();

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldProduceSequence_Succeeds()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };
        var asyncEnumerable = AsyncEnumerable(items);

        // Act & Assert
        await asyncEnumerable.ShouldProduceSequence(items);
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldProduceAtLeast_Succeeds()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };
        var asyncEnumerable = AsyncEnumerable(items);

        // Act & Assert
        await asyncEnumerable.ShouldProduceAtLeast(3);
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldProduceExactly_Succeeds()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };
        var asyncEnumerable = AsyncEnumerable(items);

        // Act & Assert
        await asyncEnumerable.ShouldProduceExactly(5);
    }

    [Fact]
    public async Task AsyncAssertionExtensions_ShouldCompleteBetween_Succeeds()
    {
        // Arrange
        var task = Task.Delay(100).ContinueWith(_ => 42);

        // Act
        var result = await task.ShouldCompleteBetween(
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(200));

        // Assert
        Assert.Equal(42, result);
    }

    private static async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}