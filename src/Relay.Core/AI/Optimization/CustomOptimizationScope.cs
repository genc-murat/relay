using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents a custom optimization scope.
    /// </summary>
    public sealed class CustomOptimizationScope : IDisposable
    {
        private bool _disposed = false;
        private readonly CustomOptimizationContext _context;
        private readonly ILogger? _logger;
        private readonly CustomOptimizationStatistics _statistics;
        private readonly DateTime _startTime;
        private int _actionsApplied;
        private int _actionsSucceeded;
        private int _actionsFailed;
        private readonly System.Collections.Concurrent.ConcurrentBag<OptimizationAction> _actions;

        private CustomOptimizationScope(CustomOptimizationContext context, ILogger? logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
            _statistics = new CustomOptimizationStatistics();
            _startTime = DateTime.UtcNow;
            _actions = new System.Collections.Concurrent.ConcurrentBag<OptimizationAction>();

            _logger?.LogTrace("Custom optimization scope created: Type={Type}, Level={Level}",
                context.OptimizationType, context.OptimizationLevel);
        }

        public static CustomOptimizationScope Create(CustomOptimizationContext context, ILogger? logger)
        {
            return new CustomOptimizationScope(context, logger);
        }

        /// <summary>
        /// Records an optimization action.
        /// </summary>
        public void RecordAction(string name, string description, bool success = true, string? errorMessage = null)
        {
            var action = new OptimizationAction
            {
                Name = name,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Success = success,
                ErrorMessage = errorMessage
            };

            _actions.Add(action);
            System.Threading.Interlocked.Increment(ref _actionsApplied);

            if (success)
                System.Threading.Interlocked.Increment(ref _actionsSucceeded);
            else
                System.Threading.Interlocked.Increment(ref _actionsFailed);

            _logger?.LogTrace("Optimization action recorded: {Name} - {Description} (Success: {Success})",
                name, description, success);
        }

        /// <summary>
        /// Records a timed optimization action.
        /// </summary>
        public async Task<T> RecordTimedActionAsync<T>(string name, string description, Func<Task<T>> action)
        {
            var actionRecord = new OptimizationAction
            {
                Name = name,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            var startTime = DateTime.UtcNow;
            System.Threading.Interlocked.Increment(ref _actionsApplied);

            try
            {
                var result = await action();
                actionRecord.Duration = DateTime.UtcNow - startTime;
                actionRecord.Success = true;

                System.Threading.Interlocked.Increment(ref _actionsSucceeded);

                _logger?.LogTrace("Timed action completed: {Name} - {Duration}ms",
                    name, actionRecord.Duration.TotalMilliseconds);

                _actions.Add(actionRecord);
                return result;
            }
            catch (Exception ex)
            {
                actionRecord.Duration = DateTime.UtcNow - startTime;
                actionRecord.Success = false;
                actionRecord.ErrorMessage = ex.Message;

                System.Threading.Interlocked.Increment(ref _actionsFailed);

                _logger?.LogWarning(ex, "Timed action failed: {Name} - {Duration}ms",
                    name, actionRecord.Duration.TotalMilliseconds);

                _actions.Add(actionRecord);
                throw;
            }
        }

        /// <summary>
        /// Gets custom optimization statistics.
        /// </summary>
        public CustomOptimizationStatistics GetStatistics()
        {
            _statistics.OptimizationActionsApplied = _actionsApplied;
            _statistics.ActionsSucceeded = _actionsSucceeded;
            _statistics.ActionsFailed = _actionsFailed;
            _statistics.Actions = _actions.ToList();

            // Calculate overall effectiveness based on success rate and action count
            if (_actionsApplied > 0)
            {
                var successRate = (double)_actionsSucceeded / _actionsApplied;
                var actionScore = Math.Min(1.0, _actionsApplied / 10.0); // More actions = better (up to 10)
                _statistics.OverallEffectiveness = (successRate * 0.7) + (actionScore * 0.3);
            }
            else
            {
                _statistics.OverallEffectiveness = 0.0;
            }

            return _statistics;
        }

        /// <summary>
        /// Gets profiling data if enabled.
        /// </summary>
        public Dictionary<string, object> GetProfilingData()
        {
            if (!_context.EnableProfiling)
                return new Dictionary<string, object>();

            var data = new Dictionary<string, object>
            {
                ["TotalDuration"] = (DateTime.UtcNow - _startTime).TotalMilliseconds,
                ["ActionsApplied"] = _actionsApplied,
                ["ActionsSucceeded"] = _actionsSucceeded,
                ["ActionsFailed"] = _actionsFailed,
                ["OptimizationType"] = _context.OptimizationType,
                ["OptimizationLevel"] = _context.OptimizationLevel
            };

            // Add action timings
            var actionTimings = _actions
                .OrderByDescending(a => a.Duration)
                .Take(10)
                .Select(a => new { a.Name, Duration = a.Duration.TotalMilliseconds, a.Success })
                .ToList();

            data["TopActions"] = actionTimings;

            return data;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var duration = DateTime.UtcNow - _startTime;
                var stats = GetStatistics();

                _logger?.LogDebug("Custom optimization scope disposed: Duration={Duration}ms, Type={Type}, Actions={Actions}, Succeeded={Succeeded}, Failed={Failed}, Effectiveness={Effectiveness:P}",
                    duration.TotalMilliseconds, _context.OptimizationType, stats.OptimizationActionsApplied,
                    stats.ActionsSucceeded, stats.ActionsFailed, stats.OverallEffectiveness);

                // Log profiling data if enabled
                if (_context.EnableProfiling)
                {
                    var profilingData = GetProfilingData();
                    _logger?.LogInformation("Custom optimization profiling: {@ProfilingData}", profilingData);
                }

                _disposed = true;
            }
        }
    }
}
