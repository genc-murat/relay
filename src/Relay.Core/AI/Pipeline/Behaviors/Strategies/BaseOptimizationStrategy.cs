using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Base class for optimization strategies providing common functionality.
/// </summary>
public abstract class BaseOptimizationStrategy<TRequest, TResponse> : IOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    protected readonly ILogger Logger;
    protected readonly IMetricsProvider? MetricsProvider;

    protected BaseOptimizationStrategy(ILogger logger, IMetricsProvider? metricsProvider = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        MetricsProvider = metricsProvider;
    }

    public abstract OptimizationStrategy StrategyType { get; }

    public abstract ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken);

    public abstract ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken);

    /// <summary>
    /// Extracts a typed parameter from the recommendation with a default value.
    /// </summary>
    protected T GetParameter<T>(OptimizationRecommendation recommendation, string parameterName, T defaultValue)
    {
        if (recommendation.Parameters?.TryGetValue(parameterName, out var value) == true)
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Records execution metrics for AI learning.
    /// </summary>
    protected void RecordMetrics(Type requestType, TimeSpan duration, bool success, Dictionary<string, object>? properties = null)
    {
        if (MetricsProvider == null)
            return;

        try
        {
            var metrics = new HandlerExecutionMetrics
            {
                RequestType = requestType,
                Duration = duration,
                Success = success,
                Timestamp = DateTimeOffset.UtcNow,
                Properties = properties ?? new Dictionary<string, object>()
            };

            MetricsProvider.RecordHandlerExecution(metrics);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to record metrics for {Strategy}", StrategyType);
        }
    }

    /// <summary>
    /// Checks if the recommendation confidence meets the minimum threshold.
    /// </summary>
    protected bool MeetsConfidenceThreshold(OptimizationRecommendation recommendation, double minConfidenceScore)
    {
        return recommendation.ConfidenceScore >= minConfidenceScore;
    }
}