using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Interface for aggregating metrics from multiple collectors
    /// </summary>
    public interface IMetricsAggregator
    {
        /// <summary>
        /// Register a metrics collector
        /// </summary>
        void RegisterCollector(IMetricsCollector collector);

        /// <summary>
        /// Unregister a metrics collector
        /// </summary>
        void UnregisterCollector(string collectorName);

        /// <summary>
        /// Get all registered collectors
        /// </summary>
        IReadOnlyCollection<IMetricsCollector> GetCollectors();

        /// <summary>
        /// Collect metrics from all registered collectors
        /// </summary>
        Task<Dictionary<string, IEnumerable<MetricValue>>> CollectAllMetricsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Collect metrics from a specific collector
        /// </summary>
        Task<IEnumerable<MetricValue>> CollectMetricsAsync(string collectorName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the latest metrics for all collectors
        /// </summary>
        Dictionary<string, IEnumerable<MetricValue>> GetLatestMetrics();

        /// <summary>
        /// Get historical metrics for a time range
        /// </summary>
        Task<Dictionary<string, IEnumerable<MetricValue>>> GetHistoricalMetricsAsync(
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Start automatic metrics collection
        /// </summary>
        Task StartCollectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop automatic metrics collection
        /// </summary>
        Task StopCollectionAsync();

        /// <summary>
        /// Check if collection is currently running
        /// </summary>
        bool IsCollectionRunning { get; }
    }

    /// <summary>
    /// Default implementation of metrics aggregator
    /// </summary>
    public class DefaultMetricsAggregator : IMetricsAggregator
    {
        private readonly ILogger _logger;
        private readonly IMetricsPublisher _publisher;
        private readonly MetricsCollectionOptions _options;
        private readonly Dictionary<string, IMetricsCollector> _collectors = new();
        private readonly Dictionary<string, List<MetricValue>> _metricsHistory = new();
        private readonly object _lock = new();
        private CancellationTokenSource? _collectionCts;
        private Task? _collectionTask;

        public DefaultMetricsAggregator(
            ILogger logger,
            IMetricsPublisher publisher,
            MetricsCollectionOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void RegisterCollector(IMetricsCollector collector)
        {
            if (collector == null) throw new ArgumentNullException(nameof(collector));

            lock (_lock)
            {
                if (_collectors.ContainsKey(collector.Name))
                {
                    _logger.LogWarning("Collector {CollectorName} is already registered", collector.Name);
                    return;
                }

                if (!collector.IsAvailable())
                {
                    _logger.LogWarning("Collector {CollectorName} is not available on this system", collector.Name);
                    return;
                }

                _collectors[collector.Name] = collector;
                _metricsHistory[collector.Name] = new List<MetricValue>();
                _logger.LogInformation("Registered metrics collector: {CollectorName}", collector.Name);
            }
        }

        public void UnregisterCollector(string collectorName)
        {
            if (string.IsNullOrEmpty(collectorName)) throw new ArgumentNullException(nameof(collectorName));

            lock (_lock)
            {
                if (_collectors.Remove(collectorName))
                {
                    _metricsHistory.Remove(collectorName);
                    _logger.LogInformation("Unregistered metrics collector: {CollectorName}", collectorName);
                }
            }
        }

        public IReadOnlyCollection<IMetricsCollector> GetCollectors()
        {
            lock (_lock)
            {
                return _collectors.Values.ToArray();
            }
        }

        public async Task<Dictionary<string, IEnumerable<MetricValue>>> CollectAllMetricsAsync(CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, IEnumerable<MetricValue>>();
            var tasks = new List<Task>();

            lock (_lock)
            {
                foreach (var collector in _collectors.Values.Where(c => _options.EnabledCollectors.Contains(c.Name)))
                {
                    tasks.Add(CollectFromCollectorAsync(collector, results, cancellationToken));
                }
            }

            await Task.WhenAll(tasks);
            return results;
        }

        public async Task<IEnumerable<MetricValue>> CollectMetricsAsync(string collectorName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(collectorName)) throw new ArgumentNullException(nameof(collectorName));

            IMetricsCollector? collector;
            lock (_lock)
            {
                _collectors.TryGetValue(collectorName, out collector);
            }

            if (collector == null)
            {
                throw new InvalidOperationException($"Collector '{collectorName}' is not registered");
            }

            var startTime = DateTime.UtcNow;
            var metrics = await collector.CollectMetricsAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            // Store in history
            lock (_lock)
            {
                if (_metricsHistory.TryGetValue(collectorName, out var history))
                {
                    history.AddRange(metrics);
                    // Maintain max history size
                    while (history.Count > _options.MaxHistorySize)
                    {
                        history.RemoveAt(0);
                    }
                }
            }

            // Publish event
            if (_options.EnableRealTimePublishing)
            {
                await _publisher.PublishMetricsCollectedAsync(new MetricsCollectedEventArgs
                {
                    CollectorName = collectorName,
                    Metrics = metrics.ToArray(),
                    CollectionDuration = duration
                });
            }

            return metrics;
        }

        public Dictionary<string, IEnumerable<MetricValue>> GetLatestMetrics()
        {
            lock (_lock)
            {
                return _metricsHistory.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IEnumerable<MetricValue>)kvp.Value.AsReadOnly());
            }
        }

        public async Task<Dictionary<string, IEnumerable<MetricValue>>> GetHistoricalMetricsAsync(
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Allow async context switch

            lock (_lock)
            {
                return _metricsHistory.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IEnumerable<MetricValue>)kvp.Value
                        .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                        .ToArray());
            }
        }

        public async Task StartCollectionAsync(CancellationToken cancellationToken = default)
        {
            if (_collectionTask != null && !_collectionTask.IsCompleted)
            {
                _logger.LogWarning("Metrics collection is already running");
                return;
            }

            _collectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _collectionTask = RunCollectionLoopAsync(_collectionCts.Token);

            _logger.LogInformation("Started automatic metrics collection");
        }

        public async Task StopCollectionAsync()
        {
            if (_collectionCts != null)
            {
                _collectionCts.Cancel();
                _collectionCts.Dispose();
                _collectionCts = null;
            }

            if (_collectionTask != null)
            {
                await _collectionTask;
                _collectionTask = null;
            }

            _logger.LogInformation("Stopped automatic metrics collection");
        }

        public bool IsCollectionRunning => _collectionTask != null && !_collectionTask.IsCompleted;

        private async Task CollectFromCollectorAsync(
            IMetricsCollector collector,
            Dictionary<string, IEnumerable<MetricValue>> results,
            CancellationToken cancellationToken)
        {
            try
            {
                var metrics = await CollectMetricsAsync(collector.Name, cancellationToken);
                results[collector.Name] = metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics from {CollectorName}", collector.Name);
                results[collector.Name] = Array.Empty<MetricValue>();
            }
        }

        private async Task RunCollectionLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CollectAllMetricsAsync(cancellationToken);

                    // Wait for next collection interval
                    await Task.Delay(_options.DefaultCollectionInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in metrics collection loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // Back off on error
                }
            }
        }
    }
}