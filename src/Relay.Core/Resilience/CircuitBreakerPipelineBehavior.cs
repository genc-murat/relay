using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Resilience
{
    /// <summary>
    /// Circuit breaker pattern implementation for request handlers.
    /// Prevents cascading failures by temporarily blocking calls to failing handlers.
    /// </summary>
    public class CircuitBreakerPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<CircuitBreakerPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly CircuitBreakerOptions _options;
        private static readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

        public CircuitBreakerPipelineBehavior(
            ILogger<CircuitBreakerPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<CircuitBreakerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestType = typeof(TRequest).Name;
            var circuitBreaker = _circuitBreakers.GetOrAdd(requestType, _ => new CircuitBreakerState(_options));

            // Check if circuit is open
            if (circuitBreaker.State == CircuitState.Open)
            {
                if (DateTime.UtcNow < circuitBreaker.NextAttemptTime)
                {
                    _logger.LogWarning("Circuit breaker is OPEN for {RequestType}. Request rejected.", requestType);
                    throw new CircuitBreakerOpenException(requestType);
                }

                // Try to transition to half-open
                circuitBreaker.TransitionToHalfOpen();
                _logger.LogInformation("Circuit breaker transitioning to HALF-OPEN for {RequestType}", requestType);
            }

            try
            {
                var result = await next();
                
                // Success - record and potentially close circuit
                circuitBreaker.RecordSuccess();
                
                if (circuitBreaker.State == CircuitState.HalfOpen)
                {
                    circuitBreaker.TransitionToClosed();
                    _logger.LogInformation("Circuit breaker CLOSED for {RequestType}", requestType);
                }

                return result;
            }
            catch (Exception)
            {
                // Failure - record and potentially open circuit
                circuitBreaker.RecordFailure();
                
                if (circuitBreaker.ShouldOpenCircuit())
                {
                    circuitBreaker.TransitionToOpen();
                    _logger.LogWarning("Circuit breaker OPENED for {RequestType} due to failure threshold exceeded", requestType);
                }

                throw;
            }
        }
    }

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

    /// <summary>
    /// Circuit breaker states.
    /// </summary>
    public enum CircuitState
    {
        Closed,    // Normal operation
        Open,      // Circuit is open, rejecting requests
        HalfOpen   // Testing if the circuit can be closed
    }

    /// <summary>
    /// Configuration options for circuit breaker.
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Failure threshold percentage (0.0 to 1.0).
        /// </summary>
        public double FailureThreshold { get; set; } = 0.5; // 50%

        /// <summary>
        /// Minimum number of requests before circuit breaker activates.
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Duration to keep circuit open before attempting half-open.
        /// </summary>
        public TimeSpan OpenCircuitDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Time window for calculating failure rate.
        /// </summary>
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open.
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public string RequestType { get; }

        public CircuitBreakerOpenException(string requestType)
            : base($"Circuit breaker is open for request type: {requestType}")
        {
            RequestType = requestType;
        }
    }
}