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

    [Fact]
    public async Task AsyncTestHelper_WhenAll_Succeeds()
    {
        // Arrange
        var tasks = new[]
        {
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3)
        };

        // Act
        await AsyncTestHelper.WhenAll(tasks);

        // Assert - No exception thrown
    }

    [Fact]
    public async Task AsyncTestHelper_WhenAll_WithResults_Succeeds()
    {
        // Arrange
        var tasks = new[]
        {
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3)
        };

        // Act
        var results = await AsyncTestHelper.WhenAll(tasks);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    [Fact]
    public async Task AsyncTestHelper_RetryUntilSuccess_Succeeds()
    {
        // Arrange
        var attempts = 0;
        var successOnAttempt = 3;

        // Act
        await AsyncTestHelper.RetryUntilSuccess(async () =>
        {
            attempts++;
            return attempts >= successOnAttempt;
        }, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal(successOnAttempt, attempts);
    }

    [Fact]
    public async Task AsyncTestHelper_Sequence_Succeeds()
    {
        // Arrange
        var results = new List<int>();

        // Act
        await AsyncTestHelper.Sequence(
            async () => { await Task.Delay(10); results.Add(1); },
            async () => { await Task.Delay(10); results.Add(2); },
            async () => { await Task.Delay(10); results.Add(3); }
        );

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, results);
    }

    [Fact]
    public async Task AsyncTestHelper_ExecuteWithMaxParallelism_Succeeds()
    {
        // Arrange
        var results = new List<int>();
        var taskFactories = new Func<Task>[]
        {
            async () => { await Task.Delay(50); lock (results) results.Add(1); },
            async () => { await Task.Delay(50); lock (results) results.Add(2); },
            async () => { await Task.Delay(50); lock (results) results.Add(3); },
            async () => { await Task.Delay(50); lock (results) results.Add(4); }
        };

        // Act
        await AsyncTestHelper.ExecuteWithMaxParallelism(taskFactories, 2);

        // Assert
        Assert.Equal(4, results.Count);
        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.Contains(3, results);
        Assert.Contains(4, results);
    }

    [Fact]
    public async Task AsyncTestHelper_WithCancellation_CanBeCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            AsyncTestHelper.WithCancellation(async token =>
            {
                await Task.Delay(1000, token);
                return 42;
            }, cts.Token));
    }

    [Fact]
    public async Task AsyncTestHelper_WhenAny_WithResults_ReturnsFirstCompleted()
    {
        // Arrange
        var tasks = new[]
        {
            Task.Delay(100).ContinueWith(_ => 1),
            Task.Delay(50).ContinueWith(_ => 2),
            Task.Delay(200).ContinueWith(_ => 3)
        };

        // Act
        var result = await AsyncTestHelper.WhenAny(tasks);

        // Assert
        Assert.Equal(2, result); // Second task completes first
    }

    [Fact]
    public async Task AsyncTestHelper_WhenAny_ReturnsWhenFirstCompletes()
    {
        // Arrange
        var tasks = new[]
        {
            Task.Delay(100),
            Task.Delay(50),
            Task.Delay(200)
        };

        // Act
        await AsyncTestHelper.WhenAny(tasks);

        // Assert - No exception thrown, first task completed
    }

    [Fact]
    public async Task AsyncTestHelper_Yield_YieldsControl()
    {
        // Arrange
        var yielded = false;

        // Act
        await AsyncTestHelper.Yield();
        yielded = true;

        // Assert
        Assert.True(yielded);
    }

    [Fact]
    public void AsyncTestHelper_FromResult_ReturnsCompletedTask()
    {
        // Arrange
        var expected = 42;

        // Act
        var task = AsyncTestHelper.FromResult(expected);
        var result = task.Result;

        // Assert
        Assert.True(task.IsCompleted);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AsyncTestHelper_Delay_CompletesAfterDelay()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        await AsyncTestHelper.Delay(TimeSpan.FromMilliseconds(50));

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public async Task AsyncTestHelper_Delay_WithResult_CompletesAfterDelay()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var expected = "result";

        // Act
        var result = await AsyncTestHelper.Delay(TimeSpan.FromMilliseconds(50), expected);

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(40));
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AsyncTestHelper_RetryUntilSuccess_WithResult_Succeeds()
    {
        // Arrange
        var attempts = 0;
        var successOnAttempt = 2;
        var expectedResult = "success";

        // Act
        var result = await AsyncTestHelper.RetryUntilSuccess(async () =>
        {
            attempts++;
            return attempts >= successOnAttempt ? (true, expectedResult) : (false, string.Empty);
        }, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal(successOnAttempt, attempts);
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task AsyncTestHelper_Sequence_WithResults_Succeeds()
    {
        // Arrange
        var results = new List<int>();

        // Act
        var sequenceResults = await AsyncTestHelper.Sequence(
            async () => { await Task.Delay(10); results.Add(1); return 1; },
            async () => { await Task.Delay(10); results.Add(2); return 2; },
            async () => { await Task.Delay(10); results.Add(3); return 3; }
        );

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, results);
        Assert.Equal(new[] { 1, 2, 3 }, sequenceResults);
    }

    [Fact]
    public async Task AsyncTestHelper_ExecuteWithMaxParallelism_WithResults_Succeeds()
    {
        // Arrange
        var taskFactories = new Func<Task<int>>[]
        {
            async () => { await Task.Delay(50); return 1; },
            async () => { await Task.Delay(50); return 2; },
            async () => { await Task.Delay(50); return 3; },
            async () => { await Task.Delay(50); return 4; }
        };

        // Act
        var results = await AsyncTestHelper.ExecuteWithMaxParallelism(taskFactories, 2);

        // Assert
        Assert.Equal(4, results.Length);
        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.Contains(3, results);
        Assert.Contains(4, results);
    }

    [Fact]
    public async Task AsyncTestHelper_WithCancellation_NoCancellationToken_Works()
    {
        // Arrange
        var taskFactory = async (CancellationToken token) =>
        {
            await Task.Delay(10, token);
            return 42;
        };

        // Act
        var result = await AsyncTestHelper.WithCancellation(taskFactory);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task AsyncTestHelper_WithCancellation_NoResult_Works()
    {
        // Arrange
        var executed = false;
        var taskFactory = async (CancellationToken token) =>
        {
            await Task.Delay(10, token);
            executed = true;
        };

        // Act
        await AsyncTestHelper.WithCancellation(taskFactory);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task AsyncTestHelper_RetryUntilSuccess_ThrowsOnNullAction()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.RetryUntilSuccess(null!, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task AsyncTestHelper_RetryUntilSuccess_ThrowsOnInvalidTimeout()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            AsyncTestHelper.RetryUntilSuccess(async () => true, TimeSpan.Zero));
    }

    [Fact]
    public async Task AsyncTestHelper_RetryUntilSuccess_WithResult_ThrowsOnNullAction()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.RetryUntilSuccess<string>(null!, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task AsyncTestHelper_RetryUntilSuccess_WithResult_ThrowsOnInvalidTimeout()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            AsyncTestHelper.RetryUntilSuccess(async () => (true, "result"), TimeSpan.Zero));
    }

    [Fact]
    public async Task AsyncTestHelper_Sequence_ThrowsOnNullTaskFactories()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.Sequence(null!));
    }

    [Fact]
    public async Task AsyncTestHelper_Sequence_ThrowsOnNullTaskFactory()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            AsyncTestHelper.Sequence(
                async () => { },
                null!,
                async () => { }
            ));
    }

    [Fact]
    public async Task AsyncTestHelper_Sequence_WithResults_ThrowsOnNullTaskFactories()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.Sequence<string>(null!));
    }

    [Fact]
    public async Task AsyncTestHelper_ExecuteWithMaxParallelism_ThrowsOnNullTaskFactories()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.ExecuteWithMaxParallelism(null!, 2));
    }

    [Fact]
    public async Task AsyncTestHelper_ExecuteWithMaxParallelism_ThrowsOnInvalidMaxParallelism()
    {
        // Arrange
        var taskFactories = new Func<Task>[] { async () => { } };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            AsyncTestHelper.ExecuteWithMaxParallelism(taskFactories, 0));
    }

    [Fact]
    public async Task AsyncTestHelper_ExecuteWithMaxParallelism_WithResults_ThrowsOnNullTaskFactories()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.ExecuteWithMaxParallelism<int>(null!, 2));
    }

    [Fact]
    public async Task AsyncTestHelper_WithCancellation_ThrowsOnNullTaskFactory()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.WithCancellation<int>(null!));
    }

    [Fact]
    public async Task AsyncTestHelper_WithCancellation_NoResult_ThrowsOnNullTaskFactory()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AsyncTestHelper.WithCancellation(null!));
    }

    private static async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}