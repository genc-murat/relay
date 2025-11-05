using System;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.CircuitBreaker.Metrics;

namespace Relay.Core.AI.CircuitBreaker.Telemetry
{
    /// <summary>
    /// Basic telemetry implementation using logging.
    /// </summary>
    public class LoggingCircuitBreakerTelemetry : ICircuitBreakerTelemetry
    {
        private readonly ILogger _logger;
        private readonly string _circuitBreakerName;

        public LoggingCircuitBreakerTelemetry(ILogger logger, string circuitBreakerName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _circuitBreakerName = circuitBreakerName ?? throw new ArgumentNullException(nameof(circuitBreakerName));
        }

        public void RecordStateChange(CircuitBreakerState previousState, CircuitBreakerState newState, string reason)
        {
            _logger.LogInformation(
                "Circuit breaker '{Name}' state changed: {PreviousState} -> {NewState}. Reason: {Reason}",
                _circuitBreakerName, previousState, newState, reason);
        }

        public void RecordSuccess(TimeSpan duration, bool isSlowCall)
        {
            if (isSlowCall)
            {
                _logger.LogWarning(
                    "Circuit breaker '{Name}' slow call completed in {Duration}ms",
                    _circuitBreakerName, duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "Circuit breaker '{Name}' call succeeded in {Duration}ms",
                    _circuitBreakerName, duration.TotalMilliseconds);
            }
        }

        public void RecordFailure(Exception exception, TimeSpan duration, bool isTimeout)
        {
            var level = isTimeout ? LogLevel.Warning : LogLevel.Error;
            _logger.Log(level,
                exception,
                "Circuit breaker '{Name}' call failed after {Duration}ms. Timeout: {IsTimeout}",
                _circuitBreakerName, duration.TotalMilliseconds, isTimeout);
        }

        public void RecordRejectedCall(CircuitBreakerState state)
        {
            _logger.LogWarning(
                "Circuit breaker '{Name}' rejected call while in {State} state",
                _circuitBreakerName, state);
        }

        public void UpdateMetrics(CircuitBreakerMetrics metrics)
        {
            _logger.LogDebug(
                "Circuit breaker '{Name}' metrics - Total: {Total}, Success: {Success}, Failure: {Failure}, " +
                "Slow: {Slow}, Rejected: {Rejected}, State: {State}",
                _circuitBreakerName, metrics.TotalCalls, metrics.SuccessfulCalls, metrics.FailedCalls,
                metrics.SlowCalls, metrics.RejectedCalls, metrics.ConsecutiveFailures);
        }
    }
}