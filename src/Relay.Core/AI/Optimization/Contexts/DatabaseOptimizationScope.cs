using System;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
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
}
