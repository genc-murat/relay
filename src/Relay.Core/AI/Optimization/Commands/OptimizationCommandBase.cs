using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization
{
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
}