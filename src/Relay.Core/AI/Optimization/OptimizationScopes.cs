using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Context for custom optimization operations.
    /// </summary>
    public sealed class CustomOptimizationContext
    {
        public Type? RequestType { get; init; }
        public string OptimizationType { get; init; } = "General";
        public int OptimizationLevel { get; init; }
        public bool EnableProfiling { get; init; }
        public bool EnableTracing { get; init; }
        public Dictionary<string, object> CustomParameters { get; init; } = new();
        public OptimizationRecommendation? Recommendation { get; init; }
    }

    /// <summary>
    /// Statistics for custom optimization operations.
    /// </summary>
    public sealed class CustomOptimizationStatistics
    {
        public int OptimizationActionsApplied { get; set; }
        public int ActionsSucceeded { get; set; }
        public int ActionsFailed { get; set; }
        public List<OptimizationAction> Actions { get; set; } = new();
        public double OverallEffectiveness { get; set; }
        public double SuccessRate => OptimizationActionsApplied > 0 ? (double)ActionsSucceeded / OptimizationActionsApplied : 0.0;
    }

    /// <summary>
    /// Represents an optimization action.
    /// </summary>
    public sealed class OptimizationAction
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

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

    /// <summary>
    /// Context for memory pooling operations.
    /// </summary>
    public sealed class MemoryPoolingContext
    {
        public bool EnableObjectPooling { get; init; }
        public bool EnableBufferPooling { get; init; }
        public int EstimatedBufferSize { get; init; } = 4096;
    }

    /// <summary>
    /// Helper class providing a scope for memory pooling operations.
    /// </summary>
    public sealed class MemoryPoolScope : IDisposable
    {
        private readonly int _bufferSize;
        private readonly ILogger? _logger;
        private bool _disposed = false;

        public MemoryPoolStatistics Statistics { get; } = new();

        private MemoryPoolScope(MemoryPoolingContext context, ILogger? logger)
        {
            _bufferSize = context.EstimatedBufferSize;
            _logger = logger;
        }

        public static MemoryPoolScope Create(int bufferSize, ILogger? logger)
        {
            return new MemoryPoolScope(
                new MemoryPoolingContext { EstimatedBufferSize = bufferSize },
                logger);
        }

        public static MemoryPoolScope Create(MemoryPoolingContext context, ILogger? logger)
        {
            return new MemoryPoolScope(context, logger);
        }

        /// <summary>
        /// Rents a buffer from the array pool.
        /// </summary>
        public byte[] RentBuffer(int minimumSize)
        {
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(minimumSize);
            Statistics.BuffersRented++;
            Statistics.TotalBytesAllocated += buffer.Length;
            return buffer;
        }

        /// <summary>
        /// Returns a buffer to the array pool.
        /// </summary>
        public void ReturnBuffer(byte[] buffer, bool clearArray = false)
        {
            if (buffer == null) return;

            System.Buffers.ArrayPool<byte>.Shared.Return(buffer, clearArray);
            Statistics.BuffersReturned++;
        }

        /// <summary>
        /// Gets current statistics.
        /// </summary>
        public MemoryPoolStatistics GetStatistics()
        {
            return Statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var efficiency = Statistics.PoolEfficiency;
                _logger?.LogDebug(
                    "Memory pool scope disposed: Rented={Rented}, Returned={Returned}, Efficiency={Efficiency:P2}, Bytes={TotalBytes}",
                    Statistics.BuffersRented, Statistics.BuffersReturned, efficiency, Statistics.TotalBytesAllocated);

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Statistics for memory pool operations.
    /// </summary>
    public sealed class MemoryPoolStatistics
    {
        public int BuffersRented { get; set; }
        public int BuffersReturned { get; set; }
        public int PoolHits { get; set; }
        public int PoolMisses { get; set; }
        public long TotalBytesAllocated { get; set; }
        public long EstimatedSavings { get; set; }
        public double Efficiency => PoolHits + PoolMisses > 0 ? (double)PoolHits / (PoolHits + PoolMisses) : 0.0;
        public double PoolEfficiency => BuffersRented > 0 ? (double)BuffersReturned / BuffersRented : 0.0;
    }

    /// <summary>
    /// Context for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingContext
    {
        public int MaxDegreeOfParallelism { get; init; }
        public bool EnableWorkStealing { get; init; }
        public int MinItemsForParallel { get; init; }
        public double CpuUtilization { get; init; }
        public int AvailableProcessors { get; init; }
    }

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

    /// <summary>
    /// Statistics for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingStatistics
    {
        public int TasksStarted { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksExecuted { get; set; }
        public int TasksFailed { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageTaskDuration { get; set; }
        public double Speedup { get; set; }
        public double Efficiency { get; set; }
        public double ActualParallelism { get; set; }
    }

    /// <summary>
    /// Context for database optimization operations.
    /// </summary>
    public sealed class DatabaseOptimizationContext
    {
        public bool EnableQueryOptimization { get; init; }
        public bool EnableConnectionPooling { get; init; }
        public bool EnableReadOnlyHint { get; init; }
        public bool EnableBatchingHint { get; init; }
        public bool EnableNoTracking { get; init; }
        public int MaxRetries { get; init; }
        public int RetryDelayMs { get; init; }
        public int QueryTimeoutSeconds { get; init; }
        public Type? RequestType { get; init; }
    }

    /// <summary>
    /// Helper class providing a scope for database optimization operations.
    /// </summary>
    public sealed class DatabaseOptimizationScope : IDisposable
    {
        private readonly ILogger? _logger;
        private bool _disposed = false;
        private int _queriesExecuted = 0;
        private int _queriesRetried = 0;
        private int _connectionPoolHits = 0;
        private int _connectionPoolMisses = 0;
        private long _slowestQueryMs = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        public DatabaseOptimizationStatistics Statistics { get; } = new();

        private DatabaseOptimizationScope(DatabaseOptimizationContext context, ILogger? logger)
        {
            _logger = logger;
        }

        public static DatabaseOptimizationScope Create(ILogger? logger)
        {
            return new DatabaseOptimizationScope(new DatabaseOptimizationContext(), logger);
        }

        public static DatabaseOptimizationScope Create(DatabaseOptimizationContext context, ILogger? logger)
        {
            return new DatabaseOptimizationScope(context, logger);
        }

        /// <summary>
        /// Records a query execution.
        /// </summary>
        public void RecordQueryExecution(TimeSpan duration, bool wasRetried = false)
        {
            System.Threading.Interlocked.Increment(ref _queriesExecuted);
            if (wasRetried)
                System.Threading.Interlocked.Increment(ref _queriesRetried);

            // Update slowest query (lock-free maximum)
            var durationMs = (long)duration.TotalMilliseconds;
            long current;
            do
            {
                current = System.Threading.Interlocked.Read(ref _slowestQueryMs);
                if (durationMs <= current) break;
            }
            while (System.Threading.Interlocked.CompareExchange(ref _slowestQueryMs, durationMs, current) != current);
        }

        /// <summary>
        /// Records connection pool usage.
        /// </summary>
        public void RecordConnectionPoolUsage(bool hit)
        {
            if (hit)
                System.Threading.Interlocked.Increment(ref _connectionPoolHits);
            else
                System.Threading.Interlocked.Increment(ref _connectionPoolMisses);
        }

        /// <summary>
        /// Records a retry attempt.
        /// </summary>
        public void RecordRetry()
        {
            System.Threading.Interlocked.Increment(ref _queriesRetried);
        }

        /// <summary>
        /// Gets current statistics.
        /// </summary>
        public DatabaseOptimizationStatistics GetStatistics()
        {
            Statistics.QueriesExecuted = _queriesExecuted;
            Statistics.QueriesRetried = _queriesRetried;
            Statistics.ConnectionPoolHits = _connectionPoolHits;
            Statistics.ConnectionPoolMisses = _connectionPoolMisses;
            Statistics.SlowestQueryDuration = TimeSpan.FromMilliseconds(_slowestQueryMs);
            Statistics.TotalDuration = DateTime.UtcNow - _startTime;

            var totalConnections = _connectionPoolHits + _connectionPoolMisses;
            Statistics.ConnectionPoolEfficiency = totalConnections > 0
                ? (double)_connectionPoolHits / totalConnections
                : 0.0;

            return Statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var totalDuration = DateTime.UtcNow - _startTime;

                Statistics.QueriesExecuted = _queriesExecuted;
                Statistics.QueriesRetried = _queriesRetried;
                Statistics.ConnectionPoolHits = _connectionPoolHits;
                Statistics.ConnectionPoolMisses = _connectionPoolMisses;
                Statistics.SlowestQueryDuration = TimeSpan.FromMilliseconds(_slowestQueryMs);
                Statistics.TotalDuration = totalDuration;

                var totalConnections = _connectionPoolHits + _connectionPoolMisses;
                Statistics.ConnectionPoolEfficiency = totalConnections > 0
                    ? (double)_connectionPoolHits / totalConnections
                    : 0.0;

                _logger?.LogDebug(
                    "Database optimization scope disposed: Queries={Queries}, Retries={Retries}, Pool Efficiency={PoolEfficiency:P2}, Slowest={Slowest}ms",
                    _queriesExecuted, _queriesRetried, Statistics.ConnectionPoolEfficiency, _slowestQueryMs);

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Statistics for database optimization operations.
    /// </summary>
    public sealed class DatabaseOptimizationStatistics
    {
        public int QueriesExecuted { get; set; }
        public int QueriesRetried { get; set; }
        public int ConnectionPoolHits { get; set; }
        public int ConnectionPoolMisses { get; set; }
        public int ConnectionsOpened { get; set; }
        public int ConnectionsReused { get; set; }
        public TimeSpan TotalQueryTime { get; set; }
        public TimeSpan SlowestQueryTime { get; set; }
        public TimeSpan SlowestQueryDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan AverageQueryTime => QueriesExecuted > 0 ? TimeSpan.FromTicks(TotalQueryTime.Ticks / QueriesExecuted) : TimeSpan.Zero;
        public double QueryEfficiency { get; set; }
        public double ConnectionPoolEfficiency { get; set; }
    }

    /// <summary>
    /// Context for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationContext
    {
        public bool EnableVectorization { get; init; }
        public int VectorSize { get; init; }
        public bool EnableUnrolling { get; init; }
        public int UnrollFactor { get; init; }
        public int MinDataSize { get; init; }
        public bool IsHardwareAccelerated { get; init; }
        public string[] SupportedVectorTypes { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Helper class providing a scope for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationScope : IDisposable
    {
        private readonly ILogger? _logger;
        private bool _disposed = false;
        private int _vectorOperations = 0;
        private int _scalarOperations = 0;
        private int _totalElementsProcessed = 0;

        public SIMDOptimizationStatistics Statistics { get; } = new();

        private SIMDOptimizationScope(SIMDOptimizationContext context, ILogger? logger)
        {
            _logger = logger;
        }

        public static SIMDOptimizationScope Create(ILogger? logger)
        {
            return new SIMDOptimizationScope(new SIMDOptimizationContext(), logger);
        }

        public static SIMDOptimizationScope Create(SIMDOptimizationContext context, ILogger? logger)
        {
            return new SIMDOptimizationScope(context, logger);
        }

        /// <summary>
        /// Processes data using SIMD when possible, falling back to scalar operations.
        /// </summary>
        public void ProcessData<T>(ReadOnlySpan<T> data, Action<System.Numerics.Vector<T>> vectorAction, Action<T> scalarAction)
            where T : struct
        {
            var vectorSize = System.Numerics.Vector<T>.Count;
            var vectorCount = data.Length / vectorSize;

            // Process vector-aligned data
            for (int i = 0; i < vectorCount; i++)
            {
                var vector = new System.Numerics.Vector<T>(data.Slice(i * vectorSize, vectorSize));
                vectorAction(vector);
                RecordVectorOperation(vectorSize);
            }

            // Process remaining scalar elements
            for (int i = vectorCount * vectorSize; i < data.Length; i++)
            {
                scalarAction(data[i]);
                RecordScalarOperation(1);
            }
        }

        public void RecordVectorOperation(int elementsProcessed)
        {
            System.Threading.Interlocked.Increment(ref _vectorOperations);
            System.Threading.Interlocked.Add(ref _totalElementsProcessed, elementsProcessed);
        }

        public void RecordScalarOperation(int elementsProcessed)
        {
            System.Threading.Interlocked.Increment(ref _scalarOperations);
            System.Threading.Interlocked.Add(ref _totalElementsProcessed, elementsProcessed);
        }

        /// <summary>
        /// Gets current statistics.
        /// </summary>
        public SIMDOptimizationStatistics GetStatistics()
        {
            Statistics.VectorOperations = _vectorOperations;
            Statistics.ScalarOperations = _scalarOperations;
            Statistics.TotalElementsProcessed = _totalElementsProcessed;

            var totalOps = _vectorOperations + _scalarOperations;
            Statistics.VectorizationRatio = totalOps > 0 ? (double)_vectorOperations / totalOps : 0.0;

            // Estimate speedup based on vectorization ratio
            var vectorSize = System.Numerics.Vector<int>.Count;
            Statistics.EstimatedSpeedup = _totalElementsProcessed > 0
                ? (_vectorOperations * vectorSize + _scalarOperations) / (double)_totalElementsProcessed
                : 1.0;

            return Statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Statistics.VectorOperations = _vectorOperations;
                Statistics.ScalarOperations = _scalarOperations;
                Statistics.TotalElementsProcessed = _totalElementsProcessed;

                var totalOps = _vectorOperations + _scalarOperations;
                Statistics.VectorizationRatio = totalOps > 0 ? (double)_vectorOperations / totalOps : 0.0;

                // Estimate speedup based on vectorization ratio
                var vectorSize = System.Numerics.Vector<int>.Count; // Assume typical vector size
                Statistics.EstimatedSpeedup = _totalElementsProcessed > 0
                    ? (_vectorOperations * vectorSize + _scalarOperations) / (double)_totalElementsProcessed
                    : 0.0;

                _logger?.LogDebug(
                    "SIMD optimization scope disposed: Vector Ops={VectorOps}, Scalar Ops={ScalarOps}, Vectorization={Vectorization:P2}, Speedup={Speedup:F2}x",
                    _vectorOperations, _scalarOperations, Statistics.VectorizationRatio, Statistics.EstimatedSpeedup);

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Statistics for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationStatistics
    {
        public int VectorOperations { get; set; }
        public int ScalarOperations { get; set; }
        public int TotalElementsProcessed { get; set; }
        public long DataProcessed { get; set; }
        public double VectorizationRatio { get; set; }
        public double VectorizedDataPercentage { get; set; }
        public double EstimatedSpeedup { get; set; }
    }
}
