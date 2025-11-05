using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Helper class for testing async operations with timeouts.
/// </summary>
public static class TaskTimeoutHelper
{
    /// <summary>
    /// Executes a task with a timeout and returns the result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the task.</returns>
    /// <exception cref="TimeoutException">Thrown when the task times out.</exception>
    public static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cts.Token);

        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds}ms");
        }

        cts.Cancel(); // Cancel the timeout task
        return await task; // Task completed successfully
    }

    /// <summary>
    /// Executes a task with a timeout.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when the task times out.</exception>
    public static async Task WithTimeout(Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cts.Token);

        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds}ms");
        }

        cts.Cancel(); // Cancel the timeout task
        await task; // Ensure the task completed successfully
    }

    /// <summary>
    /// Asserts that a task completes within the specified timeout.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to test.</param>
    /// <param name="timeout">The expected completion time.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the task.</returns>
    /// <exception cref="TimeoutException">Thrown when the task does not complete within the timeout.</exception>
    public static async Task<T> ShouldCompleteWithin<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return await WithTimeout(task, timeout, cancellationToken);
    }

    /// <summary>
    /// Asserts that a task completes within the specified timeout.
    /// </summary>
    /// <param name="task">The task to test.</param>
    /// <param name="timeout">The expected completion time.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when the task does not complete within the timeout.</exception>
    public static async Task ShouldCompleteWithin(Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await WithTimeout(task, timeout, cancellationToken);
    }

    /// <summary>
    /// Asserts that a task times out within the specified duration.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to test.</param>
    /// <param name="timeout">The timeout duration to wait for.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when the task completes before timing out.</exception>
    public static async Task ShouldTimeoutWithin<T>(Task<T> task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cts.Token);

        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask != timeoutTask)
        {
            cts.Cancel();
            throw new TimeoutException($"Task completed before expected timeout of {timeout.TotalMilliseconds}ms");
        }

        // Task timed out as expected
        cts.Cancel();
    }

    /// <summary>
    /// Asserts that a task times out within the specified duration.
    /// </summary>
    /// <param name="task">The task to test.</param>
    /// <param name="timeout">The timeout duration to wait for.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when the task completes before timing out.</exception>
    public static async Task ShouldTimeoutWithin(Task task, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cts.Token);

        var completedTask = await Task.WhenAny(task, timeoutTask);

        if (completedTask != timeoutTask)
        {
            cts.Cancel();
            throw new TimeoutException($"Task completed before expected timeout of {timeout.TotalMilliseconds}ms");
        }

        // Task timed out as expected
        cts.Cancel();
    }

    /// <summary>
    /// Measures the execution time of a task.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="task">The task to measure.</param>
    /// <returns>A tuple containing the result and the execution time.</returns>
    public static async Task<(T Result, TimeSpan ExecutionTime)> MeasureExecutionTime<T>(Task<T> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var startTime = DateTime.UtcNow;
        var result = await task;
        var executionTime = DateTime.UtcNow - startTime;

        return (result, executionTime);
    }

    /// <summary>
    /// Measures the execution time of a task.
    /// </summary>
    /// <param name="task">The task to measure.</param>
    /// <returns>The execution time.</returns>
    public static async Task<TimeSpan> MeasureExecutionTime(Task task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        var startTime = DateTime.UtcNow;
        await task;
        var executionTime = DateTime.UtcNow - startTime;

        return executionTime;
    }

    /// <summary>
    /// Creates a task that completes after the specified delay.
    /// </summary>
    /// <param name="delay">The delay duration.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that completes after the delay.</returns>
    public static Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Creates a task that completes after the specified delay with a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="delay">The delay duration.</param>
    /// <param name="result">The result to return.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that completes after the delay with the specified result.</returns>
    public static async Task<T> Delay<T>(TimeSpan delay, T result, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
        return result;
    }
}