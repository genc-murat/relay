using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Command invoker for managing optimization operations.
    /// </summary>
    public class OptimizationCommandInvoker
    {
        private readonly Stack<IOptimizationCommand> _executedCommands = new();

        /// <summary>
        /// Executes a command and tracks it for potential undo.
        /// </summary>
        public async ValueTask<StrategyExecutionResult> ExecuteCommandAsync(
            IOptimizationCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await command.ExecuteAsync(cancellationToken);

            if (result.Success && command.CanUndo)
            {
                _executedCommands.Push(command);
            }

            return result;
        }

        /// <summary>
        /// Undoes the last executed command if possible.
        /// </summary>
        public async ValueTask<bool> UndoLastCommandAsync(CancellationToken cancellationToken = default)
        {
            if (_executedCommands.Count == 0)
                return false;

            var command = _executedCommands.Pop();
            return await command.UndoAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the number of commands that can be undone.
        /// </summary>
        public int UndoableCommandCount => _executedCommands.Count;
    }
}