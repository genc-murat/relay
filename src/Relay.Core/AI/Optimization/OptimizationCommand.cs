using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization
{
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

    /// <summary>
    /// Base class for optimization commands.
    /// </summary>
    public abstract class OptimizationCommandBase : IOptimizationCommand
    {
        protected readonly OptimizationEngine _engine;
        protected readonly OptimizationContext _context;
        protected StrategyExecutionResult? _result;

        protected OptimizationCommandBase(OptimizationEngine engine, OptimizationContext context)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual bool CanUndo => false;

        public abstract string Description { get; }

        public async ValueTask<StrategyExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _result = await _engine.OptimizeAsync(_context, cancellationToken);
            return _result;
        }

        public virtual ValueTask<bool> UndoAsync(CancellationToken cancellationToken = default)
        {
            // Default implementation - cannot undo
            return ValueTask.FromResult(false);
        }
    }

    /// <summary>
    /// Command for analyzing request patterns.
    /// </summary>
    public class AnalyzeRequestCommand : OptimizationCommandBase
    {
        public AnalyzeRequestCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Analyze request execution patterns and provide optimization recommendations";

        public override bool CanUndo => false; // Analysis doesn't change state
    }

    /// <summary>
    /// Command for predicting optimal batch sizes.
    /// </summary>
    public class PredictBatchSizeCommand : OptimizationCommandBase
    {
        public PredictBatchSizeCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Predict optimal batch size based on system load and request characteristics";

        public override bool CanUndo => false; // Prediction doesn't change state
    }

    /// <summary>
    /// Command for optimizing caching configuration.
    /// </summary>
    public class OptimizeCachingCommand : OptimizationCommandBase
    {
        public OptimizeCachingCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Analyze and optimize caching configuration for better performance";

        public override bool CanUndo => true; // Could potentially revert cache settings
    }

    /// <summary>
    /// Command for learning from optimization results.
    /// </summary>
    public class LearnFromResultsCommand : OptimizationCommandBase
    {
        public LearnFromResultsCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Learn from past optimization results to improve future recommendations";

        public override bool CanUndo => false; // Learning updates internal state
    }

    /// <summary>
    /// Command for analyzing system insights.
    /// </summary>
    public class AnalyzeSystemInsightsCommand : OptimizationCommandBase
    {
        public AnalyzeSystemInsightsCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Analyze system-wide metrics and provide global optimization insights";

        public override bool CanUndo => false; // Analysis doesn't change state
    }

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