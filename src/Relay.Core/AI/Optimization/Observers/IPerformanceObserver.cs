using System;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Interface for observing performance-related events and alerts.
    /// </summary>
    public interface IPerformanceObserver
    {
        /// <summary>
        /// Called when an optimization completes.
        /// </summary>
        ValueTask OnOptimizationCompletedAsync(StrategyExecutionResult result);

        /// <summary>
        /// Called when a performance threshold is exceeded.
        /// </summary>
        ValueTask OnPerformanceThresholdExceededAsync(PerformanceAlert alert);

        /// <summary>
        /// Called when system load changes significantly.
        /// </summary>
        ValueTask OnSystemLoadChangedAsync(SystemLoadMetrics loadMetrics);
    }

    /// <summary>
    /// Represents a performance alert.
    /// </summary>
    public class PerformanceAlert
    {
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Alert severity levels.
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}