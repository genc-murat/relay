using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Modern, modular service for collecting and analyzing system metrics
    /// </summary>
    public class SystemMetricsService : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMetricsAggregator _aggregator;
        private readonly IHealthScorer _healthScorer;
        private ISystemAnalyzer _systemAnalyzer; // Made non-readonly for testing
        private readonly IMetricsPublisher _publisher;
        private readonly MetricsCollectionOptions _options;
        private readonly HealthScoringOptions _healthOptions;

        // Legacy support - will be removed in future versions
        private readonly Dictionary<string, double> _latestMetrics = new();
        private Dictionary<string, double>? _testMetrics;

        /// <summary>
        /// Sets test metrics for testing purposes
        /// </summary>
        public void SetTestMetrics(Dictionary<string, double> metrics)
        {
            _testMetrics = metrics;
        }

        /// <summary>
        /// Records prediction outcome for testing
        /// </summary>
        public void RecordPredictionOutcome(OptimizationStrategy strategy, TimeSpan predictedImprovement, TimeSpan actualImprovement, TimeSpan baselineExecutionTime)
        {
            // For testing purposes - do nothing
        }

        public SystemMetricsService(
            ILogger<SystemMetricsService> logger,
            IMetricsAggregator aggregator,
            IHealthScorer healthScorer,
            ISystemAnalyzer systemAnalyzer,
            IMetricsPublisher publisher,
            MetricsCollectionOptions options,
            HealthScoringOptions healthOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
            _healthScorer = healthScorer ?? throw new ArgumentNullException(nameof(healthScorer));
            _systemAnalyzer = systemAnalyzer ?? throw new ArgumentNullException(nameof(systemAnalyzer));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _healthOptions = healthOptions ?? throw new ArgumentNullException(nameof(healthOptions));

            // Subscribe to events
            _publisher.MetricsCollected += OnMetricsCollected;
            _publisher.HealthScoreCalculated += OnHealthScoreCalculated;
            _publisher.LoadPatternAnalyzed += OnLoadPatternAnalyzed;
        }

        /// <summary>
        /// Initialize the metrics service with default collectors
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing SystemMetricsService");

            // Register default collectors
            RegisterDefaultCollectors();

            // Start automatic collection if enabled
            if (_options.EnableRealTimePublishing)
            {
                await _aggregator.StartCollectionAsync(cancellationToken);
            }

            _logger.LogInformation("SystemMetricsService initialized successfully");
        }

        /// <summary>
        /// Register default metrics collectors
        /// </summary>
        private void RegisterDefaultCollectors()
        {
            var collectors = new IMetricsCollector[]
            {
                new CpuMetricsCollector(_logger),
                new MemoryMetricsCollector(_logger),
                new ThroughputMetricsCollector(_logger),
                new ErrorMetricsCollector(_logger),
                new NetworkMetricsCollector(_logger),
                new DiskMetricsCollector(_logger),
                new SystemLoadMetricsCollector(_logger)
            };

            foreach (var collector in collectors)
            {
                if (_options.EnabledCollectors.Contains(collector.Name))
                {
                    _aggregator.RegisterCollector(collector);
                }
            }
        }

        /// <summary>
        /// Collects all current metrics from registered collectors
        /// </summary>
        public async Task<Dictionary<string, IEnumerable<MetricValue>>> CollectAllMetricsAsync(CancellationToken cancellationToken = default)
        {
            return await _aggregator.CollectAllMetricsAsync(cancellationToken);
        }

        /// <summary>
        /// Calculates the current system health score
        /// </summary>
        public async Task<SystemHealthScore> CalculateSystemHealthScoreAsync(CancellationToken cancellationToken = default)
        {
            var metrics = GetCurrentMetricsAsDictionary();

            // Calculate individual scores
            var overall = await _healthScorer.CalculateScoreAsync(metrics, cancellationToken);
            var performance = overall; // For now, use overall as performance
            var reliability = overall;
            var scalability = overall;
            var security = overall;
            var maintainability = overall;

            var criticalAreas = _healthScorer.GetCriticalAreas(metrics).ToList();

            return new SystemHealthScore
            {
                Overall = overall,
                Performance = performance,
                Reliability = reliability,
                Scalability = scalability,
                Security = security,
                Maintainability = maintainability,
                Status = overall >= 0.8 ? "Healthy" : overall >= 0.6 ? "Warning" : "Critical",
                CriticalAreas = criticalAreas
            };
        }

        /// <summary>
        /// Analyzes current load patterns
        /// </summary>
        public async Task<LoadPatternData> AnalyzeLoadPatternsAsync(CancellationToken cancellationToken = default)
        {
            var metrics = GetCurrentMetricsAsDictionary();
            return await _systemAnalyzer.AnalyzeLoadPatternsAsync(metrics, cancellationToken);
        }

        /// <summary>
        /// Gets the current system metrics as a dictionary for analysis
        /// </summary>
        public virtual Dictionary<string, double> GetCurrentMetricsAsDictionary()
        {
            // For testing purposes, return test metrics if set
            if (_testMetrics != null)
            {
                return _testMetrics;
            }

            var allMetrics = _aggregator.GetLatestMetrics();
            return ConvertMetricsToDictionary(allMetrics);
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Converts metrics to dictionary format
        /// </summary>
        private Dictionary<string, double> ConvertMetricsToDictionary(Dictionary<string, IEnumerable<MetricValue>> allMetrics)
        {
            var result = new Dictionary<string, double>();

            foreach (var kvp in allMetrics)
            {
                var latestMetric = kvp.Value.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                if (latestMetric != null)
                {
                    result[kvp.Key] = latestMetric.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Handles metrics collected events
        /// </summary>
        private void OnMetricsCollected(object? sender, MetricsCollectedEventArgs e)
        {
            // Update latest metrics
            foreach (var metric in e.Metrics)
            {
                _latestMetrics[metric.Name] = metric.Value;
            }
        }

        /// <summary>
        /// Handles health score calculated events
        /// </summary>
        private void OnHealthScoreCalculated(object? sender, HealthScoreCalculatedEventArgs e)
        {
            // Could cache health score if needed
        }

        /// <summary>
        /// Handles load pattern analyzed events
        /// </summary>
        private void OnLoadPatternAnalyzed(object? sender, LoadPatternAnalyzedEventArgs e)
        {
            // Could cache load pattern data if needed
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop collection
                Task.Run(() => _aggregator.StopCollectionAsync()).GetAwaiter().GetResult();

                // Unsubscribe from events
                _publisher.MetricsCollected -= OnMetricsCollected;
                _publisher.HealthScoreCalculated -= OnHealthScoreCalculated;
                _publisher.LoadPatternAnalyzed -= OnLoadPatternAnalyzed;
            }
        }


    }
}