using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;

namespace Relay.Core.Resilience.CircuitBreaker;

/// <summary>
/// Circuit breaker pattern implementation for request handlers.
/// Prevents cascading failures by temporarily blocking calls to failing handlers.
/// </summary>
public class CircuitBreakerPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<CircuitBreakerPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly CircuitBreakerOptions _options;
    private readonly ITelemetryProvider? _telemetryProvider;
    private static readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

    // Test hook to clear circuit breakers
    internal static void ClearCircuitBreakers() => _circuitBreakers.Clear();

    public CircuitBreakerPipelineBehavior(
        ILogger<CircuitBreakerPipelineBehavior<TRequest, TResponse>> logger,
        IOptions<CircuitBreakerOptions> options,
        ITelemetryProvider? telemetryProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _telemetryProvider = telemetryProvider;
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
                _telemetryProvider?.RecordCircuitBreakerOperation(requestType, "request_rejected", false);
                _logger.LogWarning("Circuit breaker is OPEN for {RequestType}. Request rejected.", requestType);
                throw new CircuitBreakerOpenException(requestType);
            }

            // Try to transition to half-open
            var oldState = circuitBreaker.State.ToString();
            circuitBreaker.TransitionToHalfOpen();
            _telemetryProvider?.RecordCircuitBreakerStateChange(requestType, oldState, circuitBreaker.State.ToString());
            _logger.LogInformation("Circuit breaker transitioning to HALF-OPEN for {RequestType}", requestType);
        }

        try
        {
            var result = await next();

            // Success - record and potentially transition states
            circuitBreaker.RecordSuccess();
            _telemetryProvider?.RecordCircuitBreakerOperation(requestType, "request_succeeded", true);

            if (circuitBreaker.State == CircuitState.HalfOpen)
            {
                // Check if we should close the circuit after successful half-open trial
                if (circuitBreaker.ShouldCloseCircuit())
                {
                    var oldState = circuitBreaker.State.ToString();
                    circuitBreaker.TransitionToClosed();
                    _telemetryProvider?.RecordCircuitBreakerStateChange(requestType, oldState, circuitBreaker.State.ToString());
                    _logger.LogInformation("Circuit breaker CLOSED for {RequestType}", requestType);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            // Failure - record and potentially open circuit
            circuitBreaker.RecordFailure();
            _telemetryProvider?.RecordCircuitBreakerOperation(requestType, "request_failed", false, ex);

            if (circuitBreaker.ShouldOpenCircuit())
            {
                var oldState = circuitBreaker.State.ToString();
                circuitBreaker.TransitionToOpen();
                _telemetryProvider?.RecordCircuitBreakerStateChange(requestType, oldState, circuitBreaker.State.ToString());
                _logger.LogWarning("Circuit breaker OPENED for {RequestType} due to failure threshold exceeded", requestType);
            }

            throw;
        }
    }
}
