using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Relay.MessageBroker.Bulkhead;

/// <summary>
/// Implementation of the bulkhead pattern for resource isolation and preventing cascading failures.
/// </summary>
public sealed class Bulkhead : IBulkhead, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _queue;
    private readonly BulkheadOptions _options;
    private readonly ILogger<Bulkhead> _logger;
    private readonly string _name;

    private int _activeOperations;
    private int _queuedOperations;
    private long _rejectedOperations;
    private long _executedOperations;
    private readonly ConcurrentQueue<TimeSpan> _waitTimes;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bulkhead"/> class.
    /// </summary>
    /// <param name="options">The bulkhead options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="name">The name of this bulkhead instance.</param>
    public Bulkhead(
        IOptions<BulkheadOptions> options,
        ILogger<Bulkhead> logger,
        string name = "default")
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _name = name ?? throw new ArgumentNullException(nameof(name));

        _options.Validate();

        _semaphore = new SemaphoreSlim(_options.MaxConcurrentOperations, _options.MaxConcurrentOperations);
        _queue = new ConcurrentQueue<TaskCompletionSource<bool>>();
        _waitTimes = new ConcurrentQueue<TimeSpan>();

        _logger.LogInformation(
            "Bulkhead '{Name}' initialized. Max concurrent: {MaxConcurrent}, Max queued: {MaxQueued}",
            _name,
            _options.MaxConcurrentOperations,
            _options.MaxQueuedOperations);
    }

    /// <inheritdoc/>
    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var stopwatch = Stopwatch.StartNew();

        // Try to enter the bulkhead
        if (!await TryEnterAsync(cancellationToken))
        {
            var activeOps = Interlocked.CompareExchange(ref _activeOperations, 0, 0);
            var queuedOps = Interlocked.CompareExchange(ref _queuedOperations, 0, 0);

            Interlocked.Increment(ref _rejectedOperations);

            _logger.LogWarning(
                "Bulkhead '{Name}' rejected operation. Active: {Active}, Queued: {Queued}, Total rejected: {Rejected}",
                _name,
                activeOps,
                queuedOps,
                _rejectedOperations);

            throw new BulkheadRejectedException(
                $"Bulkhead '{_name}' is full. Active operations: {activeOps}, Queued operations: {queuedOps}",
                activeOps,
                queuedOps);
        }

        stopwatch.Stop();
        RecordWaitTime(stopwatch.Elapsed);

        try
        {
            Interlocked.Increment(ref _activeOperations);
            Interlocked.Increment(ref _executedOperations);

            _logger.LogTrace(
                "Bulkhead '{Name}' executing operation. Active: {Active}, Wait time: {WaitTime}ms",
                _name,
                _activeOperations,
                stopwatch.ElapsedMilliseconds);

            return await operation(cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
            Exit();

            _logger.LogTrace(
                "Bulkhead '{Name}' completed operation. Active: {Active}",
                _name,
                _activeOperations);
        }
    }

    /// <inheritdoc/>
    public BulkheadMetrics GetMetrics()
    {
        var activeOps = Interlocked.CompareExchange(ref _activeOperations, 0, 0);
        var queuedOps = Interlocked.CompareExchange(ref _queuedOperations, 0, 0);
        var rejectedOps = Interlocked.Read(ref _rejectedOperations);
        var executedOps = Interlocked.Read(ref _executedOperations);

        // Calculate average wait time
        var avgWaitTime = TimeSpan.Zero;
        if (_waitTimes.Count > 0)
        {
            var totalTicks = 0L;
            var count = 0;
            foreach (var waitTime in _waitTimes)
            {
                totalTicks += waitTime.Ticks;
                count++;
            }
            avgWaitTime = count > 0 ? TimeSpan.FromTicks(totalTicks / count) : TimeSpan.Zero;
        }

        return new BulkheadMetrics
        {
            ActiveOperations = activeOps,
            QueuedOperations = queuedOps,
            RejectedOperations = rejectedOps,
            ExecutedOperations = executedOps,
            MaxConcurrentOperations = _options.MaxConcurrentOperations,
            MaxQueuedOperations = _options.MaxQueuedOperations,
            AverageWaitTime = avgWaitTime
        };
    }

    /// <summary>
    /// Tries to enter the bulkhead by acquiring a semaphore slot or queuing.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if entry was successful, false if the bulkhead is full.</returns>
    private async ValueTask<bool> TryEnterAsync(CancellationToken cancellationToken)
    {
        // Try to acquire immediately
        if (_semaphore.CurrentCount > 0)
        {
            try
            {
                await _semaphore.WaitAsync(0, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        // Check if we can queue
        var currentQueued = Interlocked.CompareExchange(ref _queuedOperations, 0, 0);
        if (currentQueued >= _options.MaxQueuedOperations)
        {
            _logger.LogDebug(
                "Bulkhead '{Name}' queue is full. Current queued: {Queued}, Max: {Max}",
                _name,
                currentQueued,
                _options.MaxQueuedOperations);
            return false;
        }

        // Queue the operation
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Enqueue(tcs);
        Interlocked.Increment(ref _queuedOperations);

        _logger.LogTrace(
            "Bulkhead '{Name}' queued operation. Queued: {Queued}",
            _name,
            _queuedOperations);

        try
        {
            // Wait for our turn with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.AcquisitionTimeout);

            // Wait for either the semaphore or cancellation
            var waitTask = _semaphore.WaitAsync(cts.Token);
            var signalTask = tcs.Task;

            var completedTask = await Task.WhenAny(waitTask, signalTask);

            if (completedTask == waitTask)
            {
                await waitTask; // Propagate any exceptions
                Interlocked.Decrement(ref _queuedOperations);
                return true;
            }

            // Signal task completed - operation was signaled by Exit()
            Interlocked.Decrement(ref _queuedOperations);
            return await signalTask;
        }
        catch (OperationCanceledException)
        {
            Interlocked.Decrement(ref _queuedOperations);
            _logger.LogDebug(
                "Bulkhead '{Name}' operation cancelled while queued",
                _name);
            return false;
        }
    }

    /// <summary>
    /// Exits the bulkhead by releasing the semaphore and processing the queue.
    /// </summary>
    private void Exit()
    {
        // Process queue if there are waiting operations
        if (_queue.TryDequeue(out var tcs))
        {
            // Signal the queued operation that it can proceed
            tcs.TrySetResult(true);
        }
        else
        {
            // No queued operations, release the semaphore
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Records a wait time for metrics calculation.
    /// </summary>
    /// <param name="waitTime">The wait time to record.</param>
    private void RecordWaitTime(TimeSpan waitTime)
    {
        _waitTimes.Enqueue(waitTime);

        // Keep only the last 1000 wait times for average calculation
        while (_waitTimes.Count > 1000)
        {
            _waitTimes.TryDequeue(out _);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation(
            "Disposing Bulkhead '{Name}'. Total executed: {Executed}, Total rejected: {Rejected}",
            _name,
            _executedOperations,
            _rejectedOperations);

        _semaphore?.Dispose();

        // Cancel all queued operations
        while (_queue.TryDequeue(out var tcs))
        {
            tcs.TrySetCanceled();
        }

        _logger.LogInformation("Bulkhead '{Name}' disposed", _name);
    }
}
