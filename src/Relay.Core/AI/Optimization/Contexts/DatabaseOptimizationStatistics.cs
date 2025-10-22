using System;

namespace Relay.Core.AI.Optimization.Contexts;

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
