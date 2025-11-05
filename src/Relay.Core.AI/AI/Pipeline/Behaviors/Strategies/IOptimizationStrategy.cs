using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Interface for AI-powered optimization strategies.
/// </summary>
public interface IOptimizationStrategy<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// The optimization strategy type this implementation handles.
    /// </summary>
    OptimizationStrategy StrategyType { get; }

    /// <summary>
    /// Determines if this strategy can be applied given the current conditions.
    /// </summary>
    ValueTask<bool> CanApplyAsync(
        TRequest request,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the optimization strategy to the request handler.
    /// </summary>
    ValueTask<RequestHandlerDelegate<TResponse>> ApplyAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        OptimizationRecommendation recommendation,
        SystemLoadMetrics systemLoad,
        CancellationToken cancellationToken);
}