using System;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;

namespace Relay.Core.AI.CircuitBreaker.Strategies;

/// <summary>
/// Adaptive circuit breaker strategy that adjusts failure thresholds based on system load
/// and performance trends.
/// </summary>
public class AdaptiveCircuitBreakerStrategy : ICircuitBreakerStrategy
{
    private readonly double _baseFailureThreshold;
    private readonly double _loadSensitivity;

    /// <summary>
    /// Initializes a new instance of the AdaptiveCircuitBreakerStrategy.
    /// </summary>
    /// <param name="baseFailureThreshold">Base failure threshold. Default is 5.</param>
    /// <param name="loadSensitivity">How sensitive the strategy is to load changes (0.0 to 1.0). Default is 0.3.</param>
    public AdaptiveCircuitBreakerStrategy(double baseFailureThreshold = 5, double loadSensitivity = 0.3)
    {
        if (baseFailureThreshold <= 0)
            throw new ArgumentException("Base failure threshold must be greater than 0", nameof(baseFailureThreshold));

        if (loadSensitivity < 0 || loadSensitivity > 1)
            throw new ArgumentException("Load sensitivity must be between 0 and 1", nameof(loadSensitivity));

        _baseFailureThreshold = baseFailureThreshold;
        _loadSensitivity = loadSensitivity;
    }

    /// <inheritdoc />
    public string Name => "Adaptive";

    /// <inheritdoc />
    public bool ShouldOpen(CircuitBreakerMetrics metrics, AICircuitBreakerOptions options)
    {
        // Calculate adaptive threshold based on recent performance
        var adaptiveThreshold = CalculateAdaptiveThreshold(metrics);

        return metrics.ConsecutiveFailures >= adaptiveThreshold;
    }

    /// <inheritdoc />
    public bool ShouldClose(int recentSuccesses, int recentFailures, AICircuitBreakerOptions options)
    {
        // For adaptive strategy, be more lenient when closing
        var adaptiveSuccessThreshold = Math.Max(1, options.SuccessThreshold - 1);
        return recentSuccesses >= adaptiveSuccessThreshold;
    }

    private double CalculateAdaptiveThreshold(CircuitBreakerMetrics metrics)
    {
        // If we have low availability, be more aggressive (lower threshold)
        var availabilityAdjustment = (1.0 - metrics.Availability) * _loadSensitivity;

        // If we have high failure rate, be more aggressive
        var failureRateAdjustment = metrics.FailureRate * _loadSensitivity;

        // Combine adjustments (higher adjustment = lower threshold = more aggressive)
        var totalAdjustment = availabilityAdjustment + failureRateAdjustment;

        // Apply adjustment to base threshold (minimum threshold is 1)
        var adaptiveThreshold = Math.Max(1, _baseFailureThreshold * (1.0 - totalAdjustment));

        return adaptiveThreshold;
    }
}