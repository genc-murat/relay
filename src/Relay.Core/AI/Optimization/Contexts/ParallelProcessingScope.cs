using System;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Contexts
{
    /// <summary>
    /// Helper class providing a scope for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingScope : IDisposable
    {
        private readonly int _maxDegreeOfParallelism;
        private readonly ILogger? _logger;
        private bool _disposed = false;
        private int _tasksStarted = 0;
        private int _tasksCompleted = 0;
        private long _totalExecutionTime = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        public ParallelProcessingStatistics Statistics { get; } = new();

        private ParallelProcessingScope(ParallelProcessingContext context, ILogger? logger)
        {
            _maxDegreeOfParallelism = context.MaxDegreeOfParallelism;
            _logger = logger;
        }

        public static ParallelProcessingScope Create(int maxDegreeOfParallelism, ILogger? logger)
        {
            return new ParallelProcessingScope(
                new ParallelProcessingContext { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                logger);
        }

        public static ParallelProcessingScope Create(ParallelProcessingContext context, ILogger? logger)
        {
            return new ParallelProcessingScope(context, logger);
        }

        public int MaxDegreeOfParallelism => _maxDegreeOfParallelism;

        /// <summary>
        /// Records task execution metrics.
        /// </summary>
        public void RecordTaskExecution(TimeSpan executionTime)
        {
            System.Threading.Interlocked.Increment(ref _tasksCompleted);
            System.Threading.Interlocked.Add(ref _totalExecutionTime, (long)executionTime.TotalMilliseconds);
        }

        /// <summary>
        /// Increments tasks started counter.
        /// </summary>
        public void IncrementTasksStarted()
        {
            System.Threading.Interlocked.Increment(ref _tasksStarted);
        }

        /// <summary>
        /// Gets current statistics.
        /// </summary>
        public ParallelProcessingStatistics GetStatistics()
        {
            Statistics.TasksStarted = _tasksStarted;
            Statistics.TasksCompleted = _tasksCompleted;
            Statistics.TasksExecuted = _tasksCompleted;
            Statistics.TotalDuration = DateTime.UtcNow - _startTime;
            Statistics.AverageTaskDuration = _tasksCompleted > 0
                ? TimeSpan.FromMilliseconds((double)_totalExecutionTime / _tasksCompleted)
                : TimeSpan.Zero;

            return Statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var totalDuration = DateTime.UtcNow - _startTime;

                Statistics.TasksStarted = _tasksStarted;
                Statistics.TasksCompleted = _tasksCompleted;
                Statistics.TotalDuration = totalDuration;
                Statistics.AverageTaskDuration = _tasksCompleted > 0
                    ? TimeSpan.FromMilliseconds((double)_totalExecutionTime / _tasksCompleted)
                    : TimeSpan.Zero;

                // Calculate speedup: (total task time) / (actual wall time)
                Statistics.Speedup = totalDuration.TotalMilliseconds > 0
                    ? _totalExecutionTime / totalDuration.TotalMilliseconds
                    : 0.0;

                // Efficiency: speedup / parallelism
                Statistics.Efficiency = _maxDegreeOfParallelism > 0
                    ? Statistics.Speedup / _maxDegreeOfParallelism
                    : 0.0;

                _logger?.LogDebug(
                    "Parallel processing scope disposed: Tasks={Tasks}, Duration={Duration}ms, Speedup={Speedup:F2}x, Efficiency={Efficiency:P2}",
                    _tasksCompleted, totalDuration.TotalMilliseconds, Statistics.Speedup, Statistics.Efficiency);

                _disposed = true;
            }
        }
    }
}
