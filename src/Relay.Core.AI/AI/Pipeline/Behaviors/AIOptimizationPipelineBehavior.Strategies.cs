using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors;

/// <summary>
/// Partial class containing optimization strategy application methods.
/// </summary>
public sealed partial class AIOptimizationPipelineBehavior<TRequest, TResponse>
{
    private async ValueTask<RequestHandlerDelegate<TResponse>> ApplyOptimizations(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        List<OptimizationStrategy> appliedOptimizations,
        CancellationToken cancellationToken)
    {
        var optimizedNext = next;

        // Apply optimizations based on AI recommendations using strategy pattern
        if (recommendation.ConfidenceScore >= _options.MinConfidenceScore &&
            recommendation.Strategy != OptimizationStrategy.None)
        {
            try
            {
                var strategy = _strategyFactory.CreateStrategy(recommendation.Strategy);

                // Check if the strategy can be applied
                if (await strategy.CanApplyAsync(request, recommendation, systemLoad, cancellationToken))
                {
                    optimizedNext = await strategy.ApplyAsync(request, optimizedNext, recommendation, systemLoad, cancellationToken);
                    appliedOptimizations.Add(recommendation.Strategy);

                    _logger.LogDebug("Applied {Strategy} optimization for {RequestType}",
                        recommendation.Strategy, typeof(TRequest).Name);
                }
                else
                {
                    _logger.LogDebug("Strategy {Strategy} determined not applicable for {RequestType}",
                        recommendation.Strategy, typeof(TRequest).Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply {Strategy} optimization for {RequestType}, continuing without optimization",
                    recommendation.Strategy, typeof(TRequest).Name);
            }
        }

        return optimizedNext;
    }
}