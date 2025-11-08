using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Testing.Tests.Assertions;

public class AsyncAssertionExtensionsTests
{
    [Fact]
    public async Task ShouldCompleteWithin_TaskT_Succeeds_WhenCompletesWithinTimeout()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act & Assert
        var result = await task.ShouldCompleteWithin(TimeSpan.FromSeconds(1));
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ShouldCompleteWithin_TaskT_Throws_WhenTimesOut()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => 42);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            task.ShouldCompleteWithin(TimeSpan.FromMilliseconds(100)));
        Assert.Contains("timed out", exception.Message.ToLower());
    }

    [Fact]
    public async Task ShouldCompleteWithin_Task_Succeeds_WhenCompletesWithinTimeout()
    {
        // Arrange
        var task = Task.Delay(10);

        // Act & Assert
        await task.ShouldCompleteWithin(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldCompleteWithin_Task_Throws_WhenTimesOut()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            task.ShouldCompleteWithin(TimeSpan.FromMilliseconds(100)));
        Assert.Contains("timed out", exception.Message.ToLower());
    }

    [Fact]
    public async Task ShouldTimeoutWithin_TaskT_Succeeds_WhenTimesOut()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => 42);

        // Act & Assert
        await task.ShouldTimeoutWithin(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task ShouldTimeoutWithin_TaskT_Throws_WhenCompletesBeforeTimeout()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            task.ShouldTimeoutWithin(TimeSpan.FromSeconds(1)));
        Assert.Contains("task completed before expected timeout", exception.Message.ToLower());
    }

    [Fact]
    public async Task ShouldTimeoutWithin_Task_Succeeds_WhenTimesOut()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2));

        // Act & Assert
        await task.ShouldTimeoutWithin(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task ShouldTimeoutWithin_Task_Throws_WhenCompletesBeforeTimeout()
    {
        // Arrange
        var task = Task.Delay(10);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            task.ShouldTimeoutWithin(TimeSpan.FromSeconds(1)));
        Assert.Contains("task completed before expected timeout", exception.Message.ToLower());
    }

    [Fact]
    public async Task ShouldNotThrow_TaskT_Succeeds_WhenNoException()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act & Assert
        var result = await task.ShouldNotThrow();
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ShouldNotThrow_TaskT_Throws_WhenExceptionThrown()
    {
        // Arrange
        var task = Task.FromException<int>(new InvalidOperationException("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldNotThrow());
        Assert.Contains("Test exception", exception.Message);
    }

    [Fact]
    public async Task ShouldNotThrow_Task_Succeeds_WhenNoException()
    {
        // Arrange
        var task = Task.CompletedTask;

        // Act & Assert
        await task.ShouldNotThrow();
    }

    [Fact]
    public async Task ShouldNotThrow_Task_Throws_WhenExceptionThrown()
    {
        // Arrange
        var task = Task.FromException(new InvalidOperationException("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldNotThrow());
        Assert.Contains("Test exception", exception.Message);
    }

    [Fact]
    public async Task ShouldThrow_Task_Succeeds_WhenCorrectExceptionThrown()
    {
        // Arrange
        var task = Task.FromException(new InvalidOperationException("Test exception"));

        // Act & Assert
        var caughtException = await task.ShouldThrow<InvalidOperationException>();
        Assert.Equal("Test exception", caughtException.Message);
    }

    [Fact]
    public async Task ShouldThrow_Task_Throws_WhenNoExceptionThrown()
    {
        // Arrange
        var task = Task.CompletedTask;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldThrow<InvalidOperationException>());
        Assert.Contains("Expected task to throw InvalidOperationException, but it completed successfully", exception.Message);
    }

    [Fact]
    public async Task ShouldThrow_Task_Throws_WhenWrongExceptionThrown()
    {
        // Arrange
        var task = Task.FromException(new ArgumentException("Wrong exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldThrow<InvalidOperationException>());
        Assert.Contains("Expected task to throw InvalidOperationException, but it threw ArgumentException", exception.Message);
    }

    [Fact]
    public async Task ShouldProduceSequence_Succeeds_WhenSequencesMatch()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        await asyncEnumerable.ShouldProduceSequence(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ShouldProduceSequence_Throws_WhenSequencesDoNotMatch()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            asyncEnumerable.ShouldProduceSequence(new[] { 1, 2, 4 }));
        Assert.Contains("Expected sequence 1, 2, 4, but got 1, 2, 3", exception.Message);
    }

    [Fact]
    public async Task ShouldProduceSequence_Throws_WhenAsyncEnumerableIsNull()
    {
        // Arrange
        IAsyncEnumerable<int> asyncEnumerable = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            asyncEnumerable.ShouldProduceSequence(new[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task ShouldProduceSequence_Throws_WhenExpectedIsNull()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            asyncEnumerable.ShouldProduceSequence(null!));
    }

    [Fact]
    public async Task ShouldProduceAtLeast_Succeeds_WhenHasEnoughItems()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3, 4, 5);

        // Act & Assert
        await asyncEnumerable.ShouldProduceAtLeast(3);
    }

    [Fact]
    public async Task ShouldProduceAtLeast_Throws_WhenHasFewerItems()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            asyncEnumerable.ShouldProduceAtLeast(3));
        Assert.Contains("Expected at least 3 items, but got 2", exception.Message);
    }

    [Fact]
    public async Task ShouldProduceAtLeast_Throws_WhenAsyncEnumerableIsNull()
    {
        // Arrange
        IAsyncEnumerable<int> asyncEnumerable = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            asyncEnumerable.ShouldProduceAtLeast(1));
    }

    [Fact]
    public async Task ShouldProduceAtLeast_Throws_WhenMinimumCountIsNegative()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            asyncEnumerable.ShouldProduceAtLeast(-1));
    }

    [Fact]
    public async Task ShouldProduceExactly_Succeeds_WhenExactCount()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        await asyncEnumerable.ShouldProduceExactly(3);
    }

    [Fact]
    public async Task ShouldProduceExactly_Throws_WhenWrongCount()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            asyncEnumerable.ShouldProduceExactly(3));
        Assert.Contains("Expected exactly 3 items, but got 2", exception.Message);
    }

    [Fact]
    public async Task ShouldProduceExactly_Throws_WhenAsyncEnumerableIsNull()
    {
        // Arrange
        IAsyncEnumerable<int> asyncEnumerable = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            asyncEnumerable.ShouldProduceExactly(1));
    }

    [Fact]
    public async Task ShouldProduceExactly_Throws_WhenExpectedCountIsNegative()
    {
        // Arrange
        var asyncEnumerable = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            asyncEnumerable.ShouldProduceExactly(-1));
    }

    [Fact]
    public async Task ShouldCompleteBetween_TaskT_Succeeds_WhenWithinRange()
    {
        // Arrange
        var task = Task.Delay(50).ContinueWith(_ => 42);

        // Act & Assert
        var result = await task.ShouldCompleteBetween(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100));
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ShouldCompleteBetween_TaskT_Throws_WhenTooQuick()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldCompleteBetween(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100)));
        Assert.Contains("completed too quickly", exception.Message);
    }

    [Fact]
    public async Task ShouldCompleteBetween_TaskT_Throws_WhenTooSlow()
    {
        // Arrange
        var task = Task.Delay(200).ContinueWith(_ => 42);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldCompleteBetween(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100)));
        Assert.Contains("completed too slowly", exception.Message);
    }

    [Fact]
    public async Task ShouldCompleteBetween_Task_Succeeds_WhenWithinRange()
    {
        // Arrange
        var task = Task.Delay(50);

        // Act & Assert
        await task.ShouldCompleteBetween(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task ShouldCompleteBetween_Task_Throws_WhenTooQuick()
    {
        // Arrange
        var task = Task.CompletedTask;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldCompleteBetween(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100)));
        Assert.Contains("completed too quickly", exception.Message);
    }

    [Fact]
    public async Task ShouldCompleteBetween_Task_Throws_WhenTooSlow()
    {
        // Arrange
        var task = Task.Delay(200);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
            task.ShouldCompleteBetween(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100)));
        Assert.Contains("completed too slowly", exception.Message);
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}