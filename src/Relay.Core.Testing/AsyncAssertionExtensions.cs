using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Extension methods for async assertions.
/// </summary>
public static class AsyncAssertionExtensions
{
    /// <summary>
    /// Asserts that the task completes within the specified timeout.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to assert.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <returns>The result of the task.</returns>
    /// <exception cref="TimeoutException">Thrown when the task times out.</exception>
    public static async Task<T> ShouldCompleteWithin<T>(this Task<T> task, TimeSpan timeout, string? message = null)
    {
        return await TaskTimeoutHelper.WithTimeout(task, timeout);
    }

    /// <summary>
    /// Asserts that the task completes within the specified timeout.
    /// </summary>
    /// <param name="task">The task to assert.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="TimeoutException">Thrown when the task times out.</exception>
    public static async Task ShouldCompleteWithin(this Task task, TimeSpan timeout, string? message = null)
    {
        await TaskTimeoutHelper.WithTimeout(task, timeout);
    }

    /// <summary>
    /// Asserts that the task times out within the specified duration.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to assert.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="TimeoutException">Thrown when the task completes before timing out.</exception>
    public static async Task ShouldTimeoutWithin<T>(this Task<T> task, TimeSpan timeout, string? message = null)
    {
        await TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout);
    }

    /// <summary>
    /// Asserts that the task times out within the specified duration.
    /// </summary>
    /// <param name="task">The task to assert.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="TimeoutException">Thrown when the task completes before timing out.</exception>
    public static async Task ShouldTimeoutWithin(this Task task, TimeSpan timeout, string? message = null)
    {
        await TaskTimeoutHelper.ShouldTimeoutWithin(task, timeout);
    }

    /// <summary>
    /// Asserts that the task completes successfully without throwing an exception.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to assert.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <returns>The result of the task.</returns>
    /// <exception cref="Exception">Thrown when the task throws an exception.</exception>
    public static async Task<T> ShouldNotThrow<T>(this Task<T> task, string? message = null)
    {
        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            var errorMessage = message ?? $"Task threw an exception: {ex.Message}";
            throw new AssertionException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Asserts that the task completes successfully without throwing an exception.
    /// </summary>
    /// <param name="task">The task to assert.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="Exception">Thrown when the task throws an exception.</exception>
    public static async Task ShouldNotThrow(this Task task, string? message = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            var errorMessage = message ?? $"Task threw an exception: {ex.Message}";
            throw new AssertionException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Asserts that the task throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="TException">The type of exception expected.</typeparam>
    /// <param name="task">The task to assert.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <returns>The caught exception.</returns>
    /// <exception cref="AssertionException">Thrown when the task does not throw the expected exception.</exception>
    public static async Task<TException> ShouldThrow<TException>(this Task task, string? message = null)
        where TException : Exception
    {
        try
        {
            await task;
            var errorMessage = message ?? $"Expected task to throw {typeof(TException).Name}, but it completed successfully";
            throw new AssertionException(errorMessage);
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            var errorMessage = message ?? $"Expected task to throw {typeof(TException).Name}, but it threw {ex.GetType().Name}: {ex.Message}";
            throw new AssertionException(errorMessage, ex);
        }
    }



    /// <summary>
    /// Asserts that the async enumerable produces the expected sequence of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the sequence.</typeparam>
    /// <param name="asyncEnumerable">The async enumerable to assert.</param>
    /// <param name="expected">The expected sequence of items.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="AssertionException">Thrown when the sequences do not match.</exception>
    public static async Task ShouldProduceSequence<T>(this IAsyncEnumerable<T> asyncEnumerable, IEnumerable<T> expected, string? message = null)
    {
        if (asyncEnumerable == null) throw new ArgumentNullException(nameof(asyncEnumerable));
        if (expected == null) throw new ArgumentNullException(nameof(expected));

        var actualList = new List<T>();
        await foreach (var item in asyncEnumerable)
        {
            actualList.Add(item);
        }

        var expectedList = expected.ToList();
        if (!actualList.SequenceEqual(expectedList))
        {
            var errorMessage = message ?? $"Expected sequence {string.Join(", ", expectedList)}, but got {string.Join(", ", actualList)}";
            throw new AssertionException(errorMessage);
        }
    }

    /// <summary>
    /// Asserts that the async enumerable produces at least the specified number of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the sequence.</typeparam>
    /// <param name="asyncEnumerable">The async enumerable to assert.</param>
    /// <param name="minimumCount">The minimum number of items expected.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="AssertionException">Thrown when the sequence has fewer items than expected.</exception>
    public static async Task ShouldProduceAtLeast<T>(this IAsyncEnumerable<T> asyncEnumerable, int minimumCount, string? message = null)
    {
        if (asyncEnumerable == null) throw new ArgumentNullException(nameof(asyncEnumerable));
        if (minimumCount < 0) throw new ArgumentOutOfRangeException(nameof(minimumCount), "Minimum count must be non-negative");

        var count = 0;
        await foreach (var item in asyncEnumerable)
        {
            count++;
            if (count >= minimumCount)
                return;
        }

        var errorMessage = message ?? $"Expected at least {minimumCount} items, but got {count}";
        throw new AssertionException(errorMessage);
    }

    /// <summary>
    /// Asserts that the async enumerable produces exactly the specified number of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the sequence.</typeparam>
    /// <param name="asyncEnumerable">The async enumerable to assert.</param>
    /// <param name="expectedCount">The expected number of items.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="AssertionException">Thrown when the sequence has a different number of items.</exception>
    public static async Task ShouldProduceExactly<T>(this IAsyncEnumerable<T> asyncEnumerable, int expectedCount, string? message = null)
    {
        if (asyncEnumerable == null) throw new ArgumentNullException(nameof(asyncEnumerable));
        if (expectedCount < 0) throw new ArgumentOutOfRangeException(nameof(expectedCount), "Expected count must be non-negative");

        var count = 0;
        await foreach (var item in asyncEnumerable)
        {
            count++;
        }

        if (count != expectedCount)
        {
            var errorMessage = message ?? $"Expected exactly {expectedCount} items, but got {count}";
            throw new AssertionException(errorMessage);
        }
    }

    /// <summary>
    /// Asserts that the task completes within the expected time range.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to assert.</param>
    /// <param name="minimumTime">The minimum expected completion time.</param>
    /// <param name="maximumTime">The maximum expected completion time.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <returns>The result of the task.</returns>
    /// <exception cref="AssertionException">Thrown when the task completes outside the expected time range.</exception>
    public static async Task<T> ShouldCompleteBetween<T>(this Task<T> task, TimeSpan minimumTime, TimeSpan maximumTime, string? message = null)
    {
        var (result, executionTime) = await TaskTimeoutHelper.MeasureExecutionTime(task);

        if (executionTime < minimumTime)
        {
            var errorMessage = message ?? $"Task completed too quickly: {executionTime.TotalMilliseconds}ms (expected at least {minimumTime.TotalMilliseconds}ms)";
            throw new AssertionException(errorMessage);
        }

        if (executionTime > maximumTime)
        {
            var errorMessage = message ?? $"Task completed too slowly: {executionTime.TotalMilliseconds}ms (expected at most {maximumTime.TotalMilliseconds}ms)";
            throw new AssertionException(errorMessage);
        }

        return result;
    }

    /// <summary>
    /// Asserts that the task completes within the expected time range.
    /// </summary>
    /// <param name="task">The task to assert.</param>
    /// <param name="minimumTime">The minimum expected completion time.</param>
    /// <param name="maximumTime">The maximum expected completion time.</param>
    /// <param name="message">An optional message to include in the exception.</param>
    /// <exception cref="AssertionException">Thrown when the task completes outside the expected time range.</exception>
    public static async Task ShouldCompleteBetween(this Task task, TimeSpan minimumTime, TimeSpan maximumTime, string? message = null)
    {
        var executionTime = await TaskTimeoutHelper.MeasureExecutionTime(task);

        if (executionTime < minimumTime)
        {
            var errorMessage = message ?? $"Task completed too quickly: {executionTime.TotalMilliseconds}ms (expected at least {minimumTime.TotalMilliseconds}ms)";
            throw new AssertionException(errorMessage);
        }

        if (executionTime > maximumTime)
        {
            var errorMessage = message ?? $"Task completed too slowly: {executionTime.TotalMilliseconds}ms (expected at most {maximumTime.TotalMilliseconds}ms)";
            throw new AssertionException(errorMessage);
        }
    }
}