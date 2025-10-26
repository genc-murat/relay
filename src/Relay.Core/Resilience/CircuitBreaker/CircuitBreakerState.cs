using System;
using System.Threading;

namespace Relay.Core.Resilience.CircuitBreaker;

/// <summary>
/// Circuit breaker state management.
/// </summary>
public class CircuitBreakerState
{
    private readonly CircuitBreakerOptions _options;
    private long _failureCount;
    private long _successCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;

    public CircuitBreakerState(CircuitBreakerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public CircuitState State => _state;
    public DateTime NextAttemptTime => _lastFailureTime.Add(_options.OpenCircuitDuration);

    public void RecordSuccess()
    {
        Interlocked.Increment(ref _successCount);
        
        if (_state == CircuitState.HalfOpen && _successCount >= _options.MinimumThroughput)
        {
            // Reset failure count on successful half-open
            Interlocked.Exchange(ref _failureCount, 0);
        }
    }

    public void RecordFailure()
    {
        Interlocked.Increment(ref _failureCount);
        _lastFailureTime = DateTime.UtcNow;
    }

    public bool ShouldOpenCircuit()
    {
        var totalRequests = _successCount + _failureCount;
        if (totalRequests < _options.MinimumThroughput)
            return false;

        var failureRate = (double)_failureCount / totalRequests;
        return failureRate >= _options.FailureThreshold;
    }

    public bool ShouldCloseCircuit()
    {
        // In HalfOpen state, close circuit after minimum number of consecutive successes
        return _state == CircuitState.HalfOpen && _successCount >= _options.MinimumThroughput;
    }

    public void TransitionToOpen()
    {
        _state = CircuitState.Open;
        _lastFailureTime = DateTime.UtcNow;
    }

    public void TransitionToHalfOpen()
    {
        _state = CircuitState.HalfOpen;
        // Reset counters for half-open trial
        Interlocked.Exchange(ref _successCount, 0);
        Interlocked.Exchange(ref _failureCount, 0);
    }

    public void TransitionToClosed()
    {
        _state = CircuitState.Closed;
        Interlocked.Exchange(ref _failureCount, 0);
    }
}
