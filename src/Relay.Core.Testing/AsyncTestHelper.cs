using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Helper class for async testing operations.
/// </summary>
public static class AsyncTestHelper
{
    /// <summary>
    /// Runs multiple tasks concurrently and waits for all to complete.
    /// </summary>
    /// <param name="tasks">The tasks to run.</param>
    /// <returns>A task that completes when all tasks have completed.</returns>
    public static async Task WhenAll(params Task[] tasks)
    {
        if (tasks == null) throw new ArgumentNullException(nameof(tasks));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Runs multiple tasks concurrently and waits for all to complete.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="tasks">The tasks to run.</param>
    /// <returns>A task that completes with the results when all tasks have completed.</returns>
    public static async Task<T[]> WhenAll<T>(params Task<T>[] tasks)
    {
        if (tasks == null) throw new ArgumentNullException(nameof(tasks));
        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Runs multiple tasks concurrently and returns the result of the first one to complete.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="tasks">The tasks to run.</param>
    /// <returns>A task that completes with the result of the first completed task.</returns>
    public static async Task<T> WhenAny<T>(params Task<T>[] tasks)
    {
        if (tasks == null) throw new ArgumentNullException(nameof(tasks));
        var completedTask = await Task.WhenAny(tasks);
        return await completedTask;
    }

    /// <summary>
    /// Runs multiple tasks concurrently and returns when the first one completes.
    /// </summary>
    /// <param name="tasks">The tasks to run.</param>
    /// <returns>A task that completes when the first task completes.</returns>
    public static async Task WhenAny(params Task[] tasks)
    {
        if (tasks == null) throw new ArgumentNullException(nameof(tasks));
        await Task.WhenAny(tasks);
    }

    /// <summary>
    /// Creates a task that yields control back to the scheduler.
    /// </summary>
    /// <returns>A task that yields control.</returns>
    public static async Task Yield()
    {
        await Task.Yield();
    }

    /// <summary>
    /// Creates a completed task with the specified result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>A completed task with the result.</returns>
    public static Task<T> FromResult<T>(T result)
    {
        return Task.FromResult(result);
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
    /// <returns>A task that completes after the delay with the result.</returns>
    public static async Task<T> Delay<T>(TimeSpan delay, T result, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
        return result;
    }

    /// <summary>
    /// Executes an action repeatedly until it succeeds or times out.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="retryInterval">The interval between retries.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <exception cref="TimeoutException">Thrown when the action does not succeed within the timeout.</exception>
    public static async Task RetryUntilSuccess(Func<Task<bool>> action, TimeSpan timeout, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        var interval = retryInterval ?? TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (await action())
                    return;
            }
            catch
            {
                // Continue retrying
            }

            await Task.Delay(interval, cancellationToken);
        }

        throw new TimeoutException($"Action did not succeed within {timeout.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Executes an action repeatedly until it succeeds or times out.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="retryInterval">The interval between retries.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the successful action.</returns>
    /// <exception cref="TimeoutException">Thrown when the action does not succeed within the timeout.</exception>
    public static async Task<T> RetryUntilSuccess<T>(Func<Task<(bool Success, T Result)>> action, TimeSpan timeout, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be greater than zero");

        var interval = retryInterval ?? TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var (success, result) = await action();
                if (success)
                    return result;
            }
            catch
            {
                // Continue retrying
            }

            await Task.Delay(interval, cancellationToken);
        }

        throw new TimeoutException($"Action did not succeed within {timeout.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Creates a sequence of tasks that execute in order.
    /// </summary>
    /// <param name="taskFactories">The factories for creating tasks.</param>
    /// <returns>A task that completes when all tasks have completed in sequence.</returns>
    public static async Task Sequence(params Func<Task>[] taskFactories)
    {
        if (taskFactories == null) throw new ArgumentNullException(nameof(taskFactories));

        foreach (var factory in taskFactories)
        {
            if (factory == null) throw new ArgumentException("Task factory cannot be null", nameof(taskFactories));
            await factory();
        }
    }

    /// <summary>
    /// Creates a sequence of tasks that execute in order.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="taskFactories">The factories for creating tasks.</param>
    /// <returns>A task that completes with the results when all tasks have completed in sequence.</returns>
    public static async Task<T[]> Sequence<T>(params Func<Task<T>>[] taskFactories)
    {
        if (taskFactories == null) throw new ArgumentNullException(nameof(taskFactories));

        var results = new List<T>();
        foreach (var factory in taskFactories)
        {
            if (factory == null) throw new ArgumentException("Task factory cannot be null", nameof(taskFactories));
            results.Add(await factory());
        }

        return results.ToArray();
    }

    /// <summary>
    /// Executes tasks with a maximum degree of parallelism.
    /// </summary>
    /// <param name="taskFactories">The factories for creating tasks.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of tasks to run concurrently.</param>
    /// <returns>A task that completes when all tasks have completed.</returns>
    public static async Task ExecuteWithMaxParallelism(IEnumerable<Func<Task>> taskFactories, int maxDegreeOfParallelism)
    {
        if (taskFactories == null) throw new ArgumentNullException(nameof(taskFactories));
        if (maxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Max degree of parallelism must be greater than zero");

        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = taskFactories.Select(async factory =>
        {
            await semaphore.WaitAsync();
            try
            {
                await factory();
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes tasks with a maximum degree of parallelism.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="taskFactories">The factories for creating tasks.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of tasks to run concurrently.</param>
    /// <returns>A task that completes with the results when all tasks have completed.</returns>
    public static async Task<T[]> ExecuteWithMaxParallelism<T>(IEnumerable<Func<Task<T>>> taskFactories, int maxDegreeOfParallelism)
    {
        if (taskFactories == null) throw new ArgumentNullException(nameof(taskFactories));
        if (maxDegreeOfParallelism <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Max degree of parallelism must be greater than zero");

        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = taskFactories.Select(async factory =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await factory();
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Creates a cancellable task that can be cancelled externally.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="taskFactory">The factory for creating the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that can be cancelled.</returns>
    public static async Task<T> WithCancellation<T>(Func<CancellationToken, Task<T>> taskFactory, CancellationToken cancellationToken = default)
    {
        if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            return await taskFactory(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new OperationCanceledException("Task was cancelled", cancellationToken);
        }
    }

    /// <summary>
    /// Creates a cancellable task that can be cancelled externally.
    /// </summary>
    /// <param name="taskFactory">The factory for creating the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that can be cancelled.</returns>
    public static async Task WithCancellation(Func<CancellationToken, Task> taskFactory, CancellationToken cancellationToken = default)
    {
        if (taskFactory == null) throw new ArgumentNullException(nameof(taskFactory));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            await taskFactory(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            throw new OperationCanceledException("Task was cancelled", cancellationToken);
        }
    }
}