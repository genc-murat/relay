using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// AI-powered circuit breaker implementation.
    /// </summary>
    internal sealed class AICircuitBreaker<TResponse>
    {
        private readonly int _failureThreshold;
        private readonly int _successThreshold;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _breakDuration;
        private readonly int _halfOpenMaxCalls;
        private readonly ILogger _logger;

        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private readonly object _stateLock = new();
        private DateTime _lastStateChange = DateTime.UtcNow;
        private int _consecutiveFailures = 0;
        private int _consecutiveSuccesses = 0;
        private int _halfOpenCalls = 0;

        private long _totalCalls = 0;
        private long _successfulCalls = 0;
        private long _failedCalls = 0;
        private long _slowCalls = 0;

        public CircuitBreakerState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        public AICircuitBreaker(
            int failureThreshold,
            int successThreshold,
            TimeSpan timeout,
            TimeSpan breakDuration,
            int halfOpenMaxCalls,
            ILogger logger)
        {
            _failureThreshold = failureThreshold;
            _successThreshold = successThreshold;
            _timeout = timeout;
            _breakDuration = breakDuration;
            _halfOpenMaxCalls = halfOpenMaxCalls;
            _logger = logger;
        }

        public async ValueTask<TResponse> ExecuteAsync(
            Func<CancellationToken, ValueTask<TResponse>> operation,
            CancellationToken cancellationToken)
        {
            // Check if circuit should transition from Open to HalfOpen
            CheckForAutomaticTransition();

            // Check current state
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    throw new CircuitBreakerOpenException($"Circuit breaker is open. Last state change: {_lastStateChange}");
                }

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (_halfOpenCalls >= _halfOpenMaxCalls)
                    {
                        throw new CircuitBreakerOpenException("Circuit breaker is half-open and max calls reached");
                    }
                    _halfOpenCalls++;
                }
            }

            System.Threading.Interlocked.Increment(ref _totalCalls);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Execute with timeout
                var timeoutTask = Task.Delay(_timeout, cancellationToken);
                var operationTask = operation(cancellationToken).AsTask();

                var completedTask = await Task.WhenAny(operationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    System.Threading.Interlocked.Increment(ref _slowCalls);
                    OnFailure();
                    throw new TimeoutException($"Operation timed out after {_timeout.TotalMilliseconds}ms");
                }

                var result = await operationTask;
                stopwatch.Stop();

                // Check if call was slow
                if (stopwatch.Elapsed > _timeout * 0.8)
                {
                    System.Threading.Interlocked.Increment(ref _slowCalls);
                }

                OnSuccess();
                return result;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                OnFailure();
                throw;
            }
        }

        private void CheckForAutomaticTransition()
        {
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    var timeSinceOpen = DateTime.UtcNow - _lastStateChange;
                    if (timeSinceOpen >= _breakDuration)
                    {
                        TransitionTo(CircuitBreakerState.HalfOpen);
                        _halfOpenCalls = 0;
                        _logger.LogInformation("Circuit breaker transitioned to HalfOpen after {Duration}s", timeSinceOpen.TotalSeconds);
                    }
                }
            }
        }

        private void OnSuccess()
        {
            System.Threading.Interlocked.Increment(ref _successfulCalls);

            lock (_stateLock)
            {
                _consecutiveFailures = 0;
                _consecutiveSuccesses++;

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (_consecutiveSuccesses >= _successThreshold)
                    {
                        TransitionTo(CircuitBreakerState.Closed);
                        _logger.LogInformation("Circuit breaker transitioned to Closed after {Successes} consecutive successes", _consecutiveSuccesses);
                    }
                }
            }
        }

        private void OnFailure()
        {
            System.Threading.Interlocked.Increment(ref _failedCalls);

            lock (_stateLock)
            {
                _consecutiveSuccesses = 0;
                _consecutiveFailures++;

                if (_state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen)
                {
                    if (_consecutiveFailures >= _failureThreshold)
                    {
                        TransitionTo(CircuitBreakerState.Open);
                        _logger.LogWarning("Circuit breaker transitioned to Open after {Failures} consecutive failures", _consecutiveFailures);
                    }
                }
            }
        }

        private void TransitionTo(CircuitBreakerState newState)
        {
            _state = newState;
            _lastStateChange = DateTime.UtcNow;
            _consecutiveFailures = 0;
            _consecutiveSuccesses = 0;
        }

        public CircuitBreakerMetrics GetMetrics()
        {
            return new CircuitBreakerMetrics
            {
                TotalCalls = System.Threading.Interlocked.Read(ref _totalCalls),
                SuccessfulCalls = System.Threading.Interlocked.Read(ref _successfulCalls),
                FailedCalls = System.Threading.Interlocked.Read(ref _failedCalls),
                SlowCalls = System.Threading.Interlocked.Read(ref _slowCalls)
            };
        }
    }
}
