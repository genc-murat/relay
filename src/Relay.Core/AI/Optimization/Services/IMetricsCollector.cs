using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Represents a single metric measurement
    /// </summary>
    public class MetricValue
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Unit { get; set; } = string.Empty;
        public MetricType Type { get; set; }
    }

    /// <summary>
    /// Types of metrics that can be collected
    /// </summary>
    public enum MetricType
    {
        Gauge,      // Point-in-time value (CPU %, Memory MB)
        Counter,    // Monotonically increasing value (requests processed, errors)
        Histogram,  // Distribution of values (response times)
        Summary     // Statistical summary
    }

    /// <summary>
    /// Interface for collecting system metrics
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Name of this collector
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Types of metrics this collector provides
        /// </summary>
        IReadOnlyCollection<MetricType> SupportedTypes { get; }

        /// <summary>
        /// Collect metrics synchronously
        /// </summary>
        IEnumerable<MetricValue> CollectMetrics();

        /// <summary>
        /// Collect metrics asynchronously
        /// </summary>
        Task<IEnumerable<MetricValue>> CollectMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if this collector is available on the current system
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Get the collection interval for this collector
        /// </summary>
        TimeSpan CollectionInterval { get; }
    }

    /// <summary>
    /// Base class for metrics collectors
    /// </summary>
    public abstract class MetricsCollectorBase : IMetricsCollector
    {
        protected readonly ILogger _logger;

        protected MetricsCollectorBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public abstract string Name { get; }
        public abstract IReadOnlyCollection<MetricType> SupportedTypes { get; }
        public abstract TimeSpan CollectionInterval { get; }

        public virtual IEnumerable<MetricValue> CollectMetrics()
        {
            try
            {
                return CollectMetricsCore();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error collecting metrics from {CollectorName}", Name);
                return Array.Empty<MetricValue>();
            }
        }

        public virtual async Task<IEnumerable<MetricValue>> CollectMetricsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await CollectMetricsCoreAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error collecting metrics asynchronously from {CollectorName}", Name);
                return Array.Empty<MetricValue>();
            }
        }

        protected abstract IEnumerable<MetricValue> CollectMetricsCore();
        protected virtual Task<IEnumerable<MetricValue>> CollectMetricsCoreAsync(CancellationToken cancellationToken = default)
        {
            // Default implementation falls back to sync collection
            return Task.FromResult(CollectMetricsCore());
        }

        public virtual bool IsAvailable() => true;
    }
}