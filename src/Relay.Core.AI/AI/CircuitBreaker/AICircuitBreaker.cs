using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Strategies;
using Relay.Core.AI.CircuitBreaker.Telemetry;
using Relay.Core.AI.CircuitBreaker.Events;
using Relay.Core.AI.CircuitBreaker.Exceptions;
using Relay.Core.AI.CircuitBreaker.Metrics;

namespace Relay.Core.AI.CircuitBreaker
{
    /// <summary>
    /// AI-powered circuit breaker implementation with configurable strategies and telemetry.
    /// </summary>
    internal sealed class AICircuitBreaker<TResponse> : ICircuitBreakerEvents, IDisposable
    {
        private readonly AICircuitBreakerOptions _options;
        private readonly ICircuitBreakerStrategy _strategy;
        private readonly ILogger _logger;
        private readonly ICircuitBreakerTelemetry _telemetry;

        private CircuitBreakerState _state = CircuitBreakerState.Closed;
        private readonly SemaphoreSlim _stateSemaphore = new(1, 1);
        private DateTime _lastStateChange = DateTime.UtcNow;
        private int _consecutiveFailures = 0;
        private int _consecutiveSuccesses = 0;
        private int _halfOpenCalls = 0;

        // Metrics counters
        private long _totalCalls = 0;
        private long _successfulCalls = 0;
        private long _failedCalls = 0;
        private long _slowCalls = 0;
        private long _timeoutCalls = 0;
        private long _rejectedCalls = 0;

        // State timing
        private TimeSpan _totalOpenTime = TimeSpan.Zero;
        private TimeSpan _totalHalfOpenTime = TimeSpan.Zero;
        private TimeSpan _totalClosedTime = TimeSpan.Zero;
        private DateTime _stateStartTime = DateTime.UtcNow;

        // Response time tracking (sliding window with running statistics)
        private readonly ConcurrentQueue<double> _responseTimesMs = new();
        private const int MaxResponseTimeSamples = 1000;
        private readonly object _responseTimeLock = new();
        private double _responseTimeSum = 0;
        private int _responseTimeCount = 0;

        // Events
        public event EventHandler<CircuitBreakerStateChangedEventArgs>? StateChanged;
        public event EventHandler<CircuitBreakerFailureEventArgs>? OperationFailed;
        public event EventHandler<CircuitBreakerSuccessEventArgs>? OperationSucceeded;
        public event EventHandler<CircuitBreakerRejectedEventArgs>? CallRejected;

        public CircuitBreakerState State
        {
            get
            {
                _stateSemaphore.Wait();
                try
                {
                    return _state;
                }
                finally
                {
                    _stateSemaphore.Release();
                }
            }
        }

        public string Name => _options.Name ?? "Unnamed";

        public AICircuitBreaker(AICircuitBreakerOptions options, ILogger logger)
            : this(options, logger, null)
        {
        }

        public AICircuitBreaker(AICircuitBreakerOptions options, ILogger logger, ICircuitBreakerTelemetry? telemetry)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _options.Validate();

            _strategy = CircuitBreakerStrategyFactory.CreateStrategy(options.Policy);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetry = telemetry ?? (_options.EnableTelemetry
                ? new LoggingCircuitBreakerTelemetry(logger, options.Name ?? "Unnamed")
                : NoOpCircuitBreakerTelemetry.Instance);

            _logger.LogInformation("Initialized circuit breaker '{Name}' with {Strategy} strategy", Name, _strategy.Name);
        }

        public async ValueTask<TResponse> ExecuteAsync(
            Func<CancellationToken, ValueTask<TResponse>> operation,
            CancellationToken cancellationToken)
        {
            // Check if circuit should transition from Open to HalfOpen
            await CheckForAutomaticTransitionAsync();

            Interlocked.Increment(ref _totalCalls);

            // Check current state and handle rejection
            await _stateSemaphore.WaitAsync(cancellationToken);
            CircuitBreakerState currentState;
            bool shouldReject = false;
            try
            {
                currentState = _state;
                if (_state == CircuitBreakerState.Open)
                {
                    shouldReject = true;
                }
                else if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (_halfOpenCalls >= _options.HalfOpenMaxCalls)
                    {
                        shouldReject = true;
                    }
                    else
                    {
                        _halfOpenCalls++;
                    }
                }
            }
            finally
            {
                _stateSemaphore.Release();
            }

            if (shouldReject)
            {
                Interlocked.Increment(ref _rejectedCalls);
                var rejectedArgs = new CircuitBreakerRejectedEventArgs(currentState);
                OnCallRejected(rejectedArgs);
                throw new CircuitBreakerOpenException($"Circuit breaker is {currentState}. Last state change: {_lastStateChange}");
            }
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Execute with timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_options.Timeout);

                var operationTask = operation(timeoutCts.Token);

                var result = await operationTask;
                stopwatch.Stop();

                // Record response time
                RecordResponseTime(stopwatch.Elapsed.TotalMilliseconds);

                // Check if call was slow
                var slowCallThreshold = _options.Timeout * _options.SlowCallThreshold;
                var isSlowCall = stopwatch.Elapsed > slowCallThreshold;
                if (isSlowCall)
                {
                    Interlocked.Increment(ref _slowCalls);
                }

                OnSuccess(stopwatch.Elapsed, isSlowCall);
                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // This was a timeout, not external cancellation
                stopwatch.Stop();
                Interlocked.Increment(ref _timeoutCalls);
                var timeoutException = new TimeoutException($"Operation timed out after {_options.Timeout.TotalMilliseconds}ms");
                var failureArgs = new CircuitBreakerFailureEventArgs(
                    timeoutException,
                    stopwatch.Elapsed,
                    true);
                OnFailure(failureArgs);
                throw timeoutException;
            }
            catch (OperationCanceledException)
            {
                // External cancellation - don't count as failure or total call
                stopwatch.Stop();
                Interlocked.Decrement(ref _totalCalls);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var failureArgs = new CircuitBreakerFailureEventArgs(ex, stopwatch.Elapsed, false);
                OnFailure(failureArgs);
                throw;
            }
        }

        private async Task CheckForAutomaticTransitionAsync()
        {
            await _stateSemaphore.WaitAsync();
            try
            {
                if (_state == CircuitBreakerState.Open)
                {
                    var timeSinceOpen = DateTime.UtcNow - _lastStateChange;
                    if (timeSinceOpen >= _options.BreakDuration)
                    {
                        await TransitionToAsync(CircuitBreakerState.HalfOpen, $"Break duration of {_options.BreakDuration.TotalSeconds}s elapsed");
                        _halfOpenCalls = 0;
                    }
                }
            }
            finally
            {
                _stateSemaphore.Release();
            }
        }

        private void OnSuccess(TimeSpan duration, bool isSlowCall)
        {
            Interlocked.Increment(ref _successfulCalls);

            _telemetry.RecordSuccess(duration, isSlowCall);

            var successArgs = new CircuitBreakerSuccessEventArgs(duration, isSlowCall);
            OperationSucceeded?.Invoke(this, successArgs);

            _stateSemaphore.Wait();
            try
            {
                _consecutiveFailures = 0;
                _consecutiveSuccesses++;

                if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (_strategy.ShouldClose(_consecutiveSuccesses, _consecutiveFailures, _options))
                    {
                        _ = TransitionToAsync(CircuitBreakerState.Closed, $"{_consecutiveSuccesses} consecutive successes in HalfOpen state");
                    }
                }
            }
            finally
            {
                _stateSemaphore.Release();
            }
        }

        private void OnFailure(CircuitBreakerFailureEventArgs failureArgs)
        {
            Interlocked.Increment(ref _failedCalls);

            _telemetry.RecordFailure(failureArgs.Exception, failureArgs.Duration, failureArgs.IsTimeout);

            OperationFailed?.Invoke(this, failureArgs);

            _stateSemaphore.Wait();
            try
            {
                _consecutiveSuccesses = 0;
                _consecutiveFailures++;

                if (_state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen)
                {
                    var metrics = GetMetrics();
                    if (_strategy.ShouldOpen(metrics, _options))
                    {
                        _ = TransitionToAsync(CircuitBreakerState.Open, $"{_consecutiveFailures} consecutive failures exceeded threshold");
                    }
                }
            }
            finally
            {
                _stateSemaphore.Release();
            }
        }

        private void OnCallRejected(CircuitBreakerRejectedEventArgs rejectedArgs)
        {
            _telemetry.RecordRejectedCall(rejectedArgs.State);

            CallRejected?.Invoke(this, rejectedArgs);
        }

        private async Task TransitionToAsync(CircuitBreakerState newState, string reason)
        {
            var previousState = _state;
            var now = DateTime.UtcNow;

            // Update state timing
            var stateDuration = now - _stateStartTime;
            switch (_state)
            {
                case CircuitBreakerState.Open:
                    _totalOpenTime += stateDuration;
                    break;
                case CircuitBreakerState.HalfOpen:
                    _totalHalfOpenTime += stateDuration;
                    break;
                case CircuitBreakerState.Closed:
                    _totalClosedTime += stateDuration;
                    break;
            }

            _state = newState;
            _lastStateChange = now;
            _stateStartTime = now;
            _consecutiveFailures = 0;
            _consecutiveSuccesses = 0;

            _telemetry.RecordStateChange(previousState, newState, reason);

            var stateChangedArgs = new CircuitBreakerStateChangedEventArgs(previousState, newState, reason);
            StateChanged?.Invoke(this, stateChangedArgs);

            _logger.LogInformation("Circuit breaker '{Name}' transitioned from {PreviousState} to {NewState}: {Reason}",
                Name, previousState, newState, reason);
        }

        private void RecordResponseTime(double responseTimeMs)
        {
            lock (_responseTimeLock)
            {
                _responseTimesMs.Enqueue(responseTimeMs);
                _responseTimeSum += responseTimeMs;
                _responseTimeCount++;

                if (_responseTimesMs.Count > MaxResponseTimeSamples)
                {
                    if (_responseTimesMs.TryDequeue(out var removed))
                    {
                        _responseTimeSum -= removed;
                        _responseTimeCount--;
                    }
                }
            }
        }

        public CircuitBreakerMetrics GetMetrics()
        {
            var now = DateTime.UtcNow;
            var currentStateDuration = now - _stateStartTime;

            // Calculate current state times
            var totalOpenTime = _totalOpenTime;
            var totalHalfOpenTime = _totalHalfOpenTime;
            var totalClosedTime = _totalClosedTime;

            switch (_state)
            {
                case CircuitBreakerState.Open:
                    totalOpenTime += currentStateDuration;
                    break;
                case CircuitBreakerState.HalfOpen:
                    totalHalfOpenTime += currentStateDuration;
                    break;
                case CircuitBreakerState.Closed:
                    totalClosedTime += currentStateDuration;
                    break;
            }

            // Calculate response time statistics
            double avgResponseTime;
            lock (_responseTimeLock)
            {
                avgResponseTime = _responseTimeCount > 0 ? _responseTimeSum / _responseTimeCount : 0;
            }

            // Calculate 95th percentile (approximate for performance)
            double percentile95 = 0;
            if (_responseTimesMs.Count > 0)
            {
                var responseTimes = _responseTimesMs.ToArray();
                Array.Sort(responseTimes);
                var index = (int)(responseTimes.Length * 0.95);
                percentile95 = responseTimes[Math.Min(index, responseTimes.Length - 1)];
            }

            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = Interlocked.Read(ref _totalCalls),
                SuccessfulCalls = Interlocked.Read(ref _successfulCalls),
                FailedCalls = Interlocked.Read(ref _failedCalls),
                SlowCalls = Interlocked.Read(ref _slowCalls),
                TimeoutCalls = Interlocked.Read(ref _timeoutCalls),
                RejectedCalls = Interlocked.Read(ref _rejectedCalls),
                ConsecutiveFailures = _consecutiveFailures,
                ConsecutiveSuccesses = _consecutiveSuccesses,
                TotalOpenTime = totalOpenTime,
                TotalHalfOpenTime = totalHalfOpenTime,
                TotalClosedTime = totalClosedTime,
                LastStateChange = _lastStateChange,
                AverageResponseTimeMs = avgResponseTime,
                Percentile95ResponseTimeMs = percentile95
            };

            _telemetry.UpdateMetrics(metrics);
            return metrics;
        }

        public void Dispose()
        {
            _stateSemaphore.Dispose();
        }
    }
}
