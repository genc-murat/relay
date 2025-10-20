using System.Collections.Concurrent;
using System.Diagnostics;

namespace Relay.MessageBroker.CircuitBreaker;

/// <summary>
/// Circuit breaker implementation for protecting message broker operations.
/// </summary>
public sealed class CircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly object _lock = new();
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTimeOffset _lastStateChange = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastAttemptTime = DateTimeOffset.UtcNow;
    private int _consecutiveFailures;
    private int _consecutiveSuccesses;
    
    private readonly ConcurrentQueue<CallResult> _callResults = new();
    private long _totalCalls;
    private long _successfulCalls;
    private long _failedCalls;
    private long _slowCalls;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="options">Circuit breaker options.</param>
    public CircuitBreaker(CircuitBreakerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <inheritdoc/>
    public CircuitBreakerMetrics Metrics
    {
        get
        {
            return new CircuitBreakerMetrics
            {
                TotalCalls = Interlocked.Read(ref _totalCalls),
                SuccessfulCalls = Interlocked.Read(ref _successfulCalls),
                FailedCalls = Interlocked.Read(ref _failedCalls),
                SlowCalls = Interlocked.Read(ref _slowCalls)
            };
        }
    }

    /// <inheritdoc/>
    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }

        ThrowIfCircuitOpen();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            RecordSuccess(stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Check if exception should be ignored
            if (ShouldIgnoreException(ex))
            {
                // Don't record as failure, but still track slow calls if applicable
                if (_options.TrackSlowCalls && stopwatch.Elapsed > _options.SlowCallDurationThreshold)
                {
                    Interlocked.Increment(ref _slowCalls);
                }
                throw;
            }

            RecordFailure(stopwatch.Elapsed);
            throw;
        }
        finally
        {
            if (_options.TrackSlowCalls && stopwatch.Elapsed > _options.SlowCallDurationThreshold)
            {
                Interlocked.Increment(ref _slowCalls);
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return;
        }

        ThrowIfCircuitOpen();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            RecordSuccess(stopwatch.Elapsed);
        }
        catch
        {
            stopwatch.Stop();
            RecordFailure(stopwatch.Elapsed);
            throw;
        }
        finally
        {
            if (_options.TrackSlowCalls && stopwatch.Elapsed > _options.SlowCallDurationThreshold)
            {
                Interlocked.Increment(ref _slowCalls);
            }
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        lock (_lock)
        {
            TransitionTo(CircuitBreakerState.Closed, "Manual reset");
            _consecutiveFailures = 0;
            _consecutiveSuccesses = 0;
            ClearMetrics();
        }
    }

    /// <inheritdoc/>
    public void Isolate()
    {
        lock (_lock)
        {
            TransitionTo(CircuitBreakerState.Open, "Manual isolation");
        }
    }

    private void ThrowIfCircuitOpen()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.Open)
            {
                var timeSinceLastAttempt = DateTimeOffset.UtcNow - _lastStateChange;
                if (timeSinceLastAttempt >= _options.Timeout)
                {
                    TransitionTo(CircuitBreakerState.HalfOpen, "Timeout expired, entering half-open state");
                }
                else
                {
                    _options.OnRejected?.Invoke(new CircuitBreakerRejectedEventArgs
                    {
                        CurrentState = _state,
                        OperationName = "MessageBrokerOperation"
                    });
                    throw new CircuitBreakerOpenException($"Circuit breaker is open. Time remaining: {_options.Timeout - timeSinceLastAttempt}");
                }
            }

            if (_state == CircuitBreakerState.HalfOpen)
            {
                // In half-open state, allow requests to pass through
                // The success/failure recording will handle state transitions
                _lastAttemptTime = DateTimeOffset.UtcNow;
            }
        }
    }

    private void RecordSuccess(TimeSpan duration)
    {
        Interlocked.Increment(ref _totalCalls);
        Interlocked.Increment(ref _successfulCalls);

        _callResults.Enqueue(new CallResult
        {
            Success = true,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = duration
        });

        CleanupOldResults();

        lock (_lock)
        {
            _consecutiveFailures = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _consecutiveSuccesses++;
                if (_consecutiveSuccesses >= _options.SuccessThreshold)
                {
                    TransitionTo(CircuitBreakerState.Closed, "Success threshold reached in half-open state");
                    _consecutiveSuccesses = 0;
                }
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                var metrics = CalculateMetrics();
                if (ShouldOpenCircuit(metrics))
                {
                    TransitionTo(CircuitBreakerState.Open,
                        $"Failure threshold reached. Failures: {_consecutiveFailures}, Rate: {metrics.FailureRate:P}");
                }
            }
        }
    }

    private bool ShouldIgnoreException(Exception exception)
    {
        // If custom predicate is set, use it
        if (_options.ExceptionPredicate != null)
        {
            return !_options.ExceptionPredicate(exception);
        }

        // Otherwise, check if exception type is in ignored types
        if (_options.IgnoredExceptionTypes != null)
        {
            var exceptionType = exception.GetType();
            return _options.IgnoredExceptionTypes.Any(type => type.IsAssignableFrom(exceptionType));
        }

        return false;
    }

    private void RecordFailure(TimeSpan duration)
    {
        Interlocked.Increment(ref _totalCalls);

        _callResults.Enqueue(new CallResult
        {
            Success = false,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = duration
        });

        CleanupOldResults();

        lock (_lock)
        {
            Interlocked.Increment(ref _failedCalls);
            _consecutiveFailures++;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionTo(CircuitBreakerState.Open, "Half-open request failed");
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                if (_consecutiveFailures >= _options.FailureThreshold)
                {
                    TransitionTo(CircuitBreakerState.Open, "Failure threshold reached");
                }
                else
                {
                    var metrics = CalculateMetrics();
                    if (ShouldOpenCircuit(metrics))
                    {
                        TransitionTo(CircuitBreakerState.Open,
                            $"Failure rate threshold reached. Failures: {_consecutiveFailures}, Rate: {metrics.FailureRate:P}");
                    }
                }
            }
        }
    }

    private bool ShouldOpenCircuit(CircuitBreakerMetrics metrics)
    {
        if (_consecutiveFailures >= _options.FailureThreshold)
        {
            return true;
        }

        if (metrics.TotalCalls >= _options.MinimumThroughput)
        {
            if (metrics.FailureRate >= _options.FailureRateThreshold)
            {
                return true;
            }

            if (_options.TrackSlowCalls && metrics.SlowCallRate >= _options.SlowCallRateThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private CircuitBreakerMetrics CalculateMetrics()
    {
        var recentCalls = _callResults.Where(r => 
            DateTimeOffset.UtcNow - r.Timestamp <= _options.SamplingDuration).ToList();

        var totalCalls = recentCalls.Count;
        var successfulCalls = recentCalls.Count(r => r.Success);
        var failedCalls = totalCalls - successfulCalls;
        var slowCalls = recentCalls.Count(r => r.Duration > _options.SlowCallDurationThreshold);

        return new CircuitBreakerMetrics
        {
            TotalCalls = totalCalls,
            SuccessfulCalls = successfulCalls,
            FailedCalls = failedCalls,
            SlowCalls = slowCalls
        };
    }

    private void CleanupOldResults()
    {
        var cutoffTime = DateTimeOffset.UtcNow - _options.SamplingDuration;
        
        while (_callResults.TryPeek(out var result) && result.Timestamp < cutoffTime)
        {
            _callResults.TryDequeue(out _);
        }
    }

    private void ClearMetrics()
    {
        _callResults.Clear();
        Interlocked.Exchange(ref _totalCalls, 0);
        Interlocked.Exchange(ref _successfulCalls, 0);
        Interlocked.Exchange(ref _failedCalls, 0);
        Interlocked.Exchange(ref _slowCalls, 0);
    }

    private void TransitionTo(CircuitBreakerState newState, string reason)
    {
        var previousState = _state;
        _state = newState;
        _lastStateChange = DateTimeOffset.UtcNow;

        _options.OnStateChanged?.Invoke(new CircuitBreakerStateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState,
            Reason = reason,
            Metrics = Metrics
        });
    }

    private sealed class CallResult
    {
        public bool Success { get; init; }
        public DateTimeOffset Timestamp { get; init; }
        public TimeSpan Duration { get; init; }
    }
}
