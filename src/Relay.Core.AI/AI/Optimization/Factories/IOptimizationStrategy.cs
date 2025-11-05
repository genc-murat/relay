using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI;

/// <summary>
/// Base interface for optimization strategies.
/// </summary>
public interface IOptimizationStrategy
{
    /// <summary>
    /// Gets the name of the strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of the strategy (higher values = higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this strategy can handle the given operation.
    /// </summary>
    bool CanHandle(string operation);

    /// <summary>
    /// Executes the optimization strategy.
    /// </summary>
    ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default);
}
