using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Relay.Core.AI.Optimization.Data;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Event arguments for metrics collection events
    /// </summary>
    public class MetricsCollectedEventArgs : EventArgs
    {
        public string CollectorName { get; set; } = string.Empty;
        public IReadOnlyCollection<MetricValue> Metrics { get; set; } = Array.Empty<MetricValue>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan CollectionDuration { get; set; }
    }

    /// <summary>
    /// Event arguments for health score calculation events
    /// </summary>
    public class HealthScoreCalculatedEventArgs : EventArgs
    {
        public SystemHealthScore HealthScore { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan CalculationDuration { get; set; }
    }

    /// <summary>
    /// Event arguments for load pattern analysis events
    /// </summary>
    public class LoadPatternAnalyzedEventArgs : EventArgs
    {
        public LoadPatternData LoadPatternData { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan AnalysisDuration { get; set; }
    }

    /// <summary>
    /// Interface for publishing metrics events
    /// </summary>
    public interface IMetricsPublisher
    {
        /// <summary>
        /// Event raised when metrics are collected
        /// </summary>
        event EventHandler<MetricsCollectedEventArgs>? MetricsCollected;

        /// <summary>
        /// Event raised when health score is calculated
        /// </summary>
        event EventHandler<HealthScoreCalculatedEventArgs>? HealthScoreCalculated;

        /// <summary>
        /// Event raised when load pattern analysis is completed
        /// </summary>
        event EventHandler<LoadPatternAnalyzedEventArgs>? LoadPatternAnalyzed;

        /// <summary>
        /// Publish metrics collection event
        /// </summary>
        Task PublishMetricsCollectedAsync(MetricsCollectedEventArgs args);

        /// <summary>
        /// Publish health score calculation event
        /// </summary>
        Task PublishHealthScoreCalculatedAsync(HealthScoreCalculatedEventArgs args);

        /// <summary>
        /// Publish load pattern analysis event
        /// </summary>
        Task PublishLoadPatternAnalyzedAsync(LoadPatternAnalyzedEventArgs args);
    }

    /// <summary>
    /// Default implementation of metrics publisher
    /// </summary>
    public class DefaultMetricsPublisher : IMetricsPublisher
    {
        public event EventHandler<MetricsCollectedEventArgs>? MetricsCollected;
        public event EventHandler<HealthScoreCalculatedEventArgs>? HealthScoreCalculated;
        public event EventHandler<LoadPatternAnalyzedEventArgs>? LoadPatternAnalyzed;

        public async Task PublishMetricsCollectedAsync(MetricsCollectedEventArgs args)
        {
            await Task.Yield(); // Allow async context switch
            MetricsCollected?.Invoke(this, args);
        }

        public async Task PublishHealthScoreCalculatedAsync(HealthScoreCalculatedEventArgs args)
        {
            await Task.Yield(); // Allow async context switch
            HealthScoreCalculated?.Invoke(this, args);
        }

        public async Task PublishLoadPatternAnalyzedAsync(LoadPatternAnalyzedEventArgs args)
        {
            await Task.Yield(); // Allow async context switch
            LoadPatternAnalyzed?.Invoke(this, args);
        }
    }
}