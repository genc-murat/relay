using Microsoft.Extensions.Logging;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Exceptions;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Strategy for applying circuit breaker optimizations.
/// </summary>
public class CircuitBreakerOptimizationStrategy<TRequest, TResponse> : BaseOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Circuit breaker registry - one per request type
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _circuitBreakers = new();

    public CircuitBreakerOptimizationStrategy(
        ILogger logger,
        IMetricsProvider? metricsProvider = null)
        : base(logger, metricsProvider)
    {
    }

    public override OptimizationStrategy StrategyType => OptimizationStrategy.CircuitBreaker;

    public override ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Circuit breaker is beneficial for protecting against cascading failures
        // Apply when confidence is reasonable and system shows signs of stress
        return new ValueTask<bool>(MeetsConfidenceThreshold(recommendation, 0.4) &&
                                   (systemLoad.ErrorRate > 0.05 || systemLoad.CpuUtilization > 0.8));
    }

    public override ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken)
    {
        // Extract circuit breaker parameters from AI recommendation
        var failureThreshold = GetParameter(recommendation, "FailureThreshold", 5);
        var successThreshold = GetParameter(recommendation, "SuccessThreshold", 2);
        var timeout = GetParameter(recommendation, "Timeout", 30000);
        var breakDuration = GetParameter(recommendation, "BreakDuration", 60000);
        var halfOpenMaxCalls = GetParameter(recommendation, "HalfOpenMaxCalls", 1);

        Logger.LogDebug("Applying circuit breaker optimization for {RequestType}: FailureThreshold={FailureThreshold}, Timeout={Timeout}ms, BreakDuration={BreakDuration}ms",
            typeof(TRequest).Name, failureThreshold, timeout, breakDuration);

        // Get or create circuit breaker for this request type
        var circuitBreaker = GetCircuitBreaker(
            typeof(TRequest),
            failureThreshold,
            successThreshold,
            TimeSpan.FromMilliseconds(timeout),
            TimeSpan.FromMilliseconds(breakDuration),
            halfOpenMaxCalls);

        // Wrap handler with circuit breaker logic
        return new ValueTask<RequestHandlerDelegate<TResponse>>(async () =>
        {
            try
            {
                var result = await circuitBreaker.ExecuteAsync(
                    async ct =>
                    {
                        var response = await next();
                        return response;
                    },
                    CancellationToken.None);

                // Record successful execution
                RecordCircuitBreakerMetrics(typeof(TRequest), circuitBreaker.State, circuitBreaker.GetMetrics(), success: true);

                return result;
            }
            catch (CircuitBreakerOpenException ex)
            {
                Logger.LogWarning("Circuit breaker is OPEN for {RequestType} - request rejected", typeof(TRequest).Name);

                // Record circuit open
                RecordCircuitBreakerMetrics(typeof(TRequest), CircuitBreakerState.Open, circuitBreaker.GetMetrics(), success: false);

                // Provide fallback response or rethrow
                if (TryGetFallbackResponse(recommendation, out var fallbackResponse))
                {
                    Logger.LogDebug("Using fallback response for {RequestType}", typeof(TRequest).Name);
                    return fallbackResponse!;
                }

                throw new InvalidOperationException($"Circuit breaker is open for {typeof(TRequest).Name}", ex);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Circuit breaker execution failed for {RequestType}", typeof(TRequest).Name);

                // Record failure
                RecordCircuitBreakerMetrics(typeof(TRequest), circuitBreaker.State, circuitBreaker.GetMetrics(), success: false);

                throw;
            }
        });
    }

    private AICircuitBreaker<TResponse> GetCircuitBreaker(
        Type requestType,
        int failureThreshold,
        int successThreshold,
        TimeSpan timeout,
        TimeSpan breakDuration,
        int halfOpenMaxCalls)
    {
        var key = $"CircuitBreaker:{requestType.FullName}";

        // Try to get existing circuit breaker
        if (_circuitBreakers.TryGetValue(key, out var existing) && existing is AICircuitBreaker<TResponse> cb)
        {
            return cb;
        }

        // Create new circuit breaker
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = failureThreshold,
            SuccessThreshold = successThreshold,
            Timeout = timeout,
            BreakDuration = breakDuration,
            HalfOpenMaxCalls = halfOpenMaxCalls,
            Name = $"CircuitBreaker:{requestType.FullName}"
        };

        var circuitBreaker = new AICircuitBreaker<TResponse>(options, Logger);

        _circuitBreakers[key] = circuitBreaker;

        Logger.LogInformation("Created circuit breaker for {RequestType}: FailureThreshold={FailureThreshold}, BreakDuration={BreakDuration}s",
            requestType.Name, failureThreshold, breakDuration.TotalSeconds);

        return circuitBreaker;
    }

    private bool TryGetFallbackResponse(OptimizationRecommendation recommendation, out TResponse? fallbackResponse)
    {
        fallbackResponse = default;

        if (recommendation.Parameters.TryGetValue("FallbackResponse", out var fallback))
        {
            if (fallback is TResponse typedFallback)
            {
                fallbackResponse = typedFallback;
                return true;
            }
        }

        // Check if response type has a public default constructor
        if (typeof(TResponse).IsClass && typeof(TResponse).GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null) != null)
        {
            fallbackResponse = Activator.CreateInstance<TResponse>();
            return true;
        }

        return false;
    }

    private void RecordCircuitBreakerMetrics(
        Type requestType,
        CircuitBreakerState state,
        CircuitBreakerMetrics metrics,
        bool success)
    {
        var properties = new Dictionary<string, object>
        {
            ["CircuitBreakerState"] = state.ToString(),
            ["TotalCalls"] = metrics.TotalCalls,
            ["SuccessfulCalls"] = metrics.SuccessfulCalls,
            ["FailedCalls"] = metrics.FailedCalls,
            ["SlowCalls"] = metrics.SlowCalls,
            ["FailureRate"] = metrics.FailureRate,
            ["SuccessRate"] = metrics.SuccessRate,
            ["SlowCallRate"] = metrics.SlowCallRate
        };

        RecordMetrics(requestType, TimeSpan.Zero, success, properties);
    }
}