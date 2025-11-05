using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization;

/// <summary>
/// Command interface for optimization operations.
/// </summary>
public interface IOptimizationCommand
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    ValueTask<StrategyExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes the command if possible.
    /// </summary>
    ValueTask<bool> UndoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the command can be undone.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Gets the command description.
    /// </summary>
    string Description { get; }
}