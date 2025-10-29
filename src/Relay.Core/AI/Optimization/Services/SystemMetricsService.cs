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
        private readonly ISystemAnalyzer _systemAnalyzer;
        private readonly IMetricsPublisher _publisher;
        private readonly MetricsCollectionOptions _options;
        private readonly HealthScoringOptions _healthOptions;

        // Legacy support - will be removed in future versions
        private readonly Dictionary<string, double> _latestMetrics = new();
        private Dictionary<string, double>? _testMetrics;

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
        /// Collect all system metrics asynchronously
        /// </summary>
        public async Task<Dictionary<string, IEnumerable<MetricValue>>> CollectAllMetricsAsync(CancellationToken cancellationToken = default)
        {
            return await _aggregator.CollectAllMetricsAsync(cancellationToken);
        }

        /// <summary>
        /// Collect metrics from a specific collector
        /// </summary>
        public async Task<IEnumerable<MetricValue>> CollectMetricsAsync(string collectorName, CancellationToken cancellationToken = default)
        {
            return await _aggregator.CollectMetricsAsync(collectorName, cancellationToken);
        }

        /// <summary>
        /// Calculate system health score
        /// </summary>
        public async Task<SystemHealthScore> CalculateSystemHealthScoreAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            // Collect current metrics
            var allMetrics = await _aggregator.CollectAllMetricsAsync(cancellationToken);

            // Convert to legacy format for health scoring
            var metricsDict = ConvertMetricsToDictionary(allMetrics);

            // Calculate health score
            var overallScore = await _healthScorer.CalculateScoreAsync(metricsDict, cancellationToken);
            var status = GetHealthStatus(overallScore);
            var criticalAreas = _healthScorer.GetCriticalAreas(metricsDict).ToList();

            var healthScore = new SystemHealthScore
            {
                Overall = overallScore,
                Performance = await CalculateAspectScoreAsync("PerformanceScorer", metricsDict, cancellationToken),
                Reliability = await CalculateAspectScoreAsync("ReliabilityScorer", metricsDict, cancellationToken),
                Scalability = await CalculateAspectScoreAsync("ScalabilityScorer", metricsDict, cancellationToken),
                Security = await CalculateAspectScoreAsync("SecurityScorer", metricsDict, cancellationToken),
                Maintainability = await CalculateAspectScoreAsync("MaintainabilityScorer", metricsDict, cancellationToken),
                Status = status,
                CriticalAreas = criticalAreas
            };

            var calculationDuration = DateTime.UtcNow - startTime;

            // Publish event
            await _publisher.PublishHealthScoreCalculatedAsync(new HealthScoreCalculatedEventArgs
            {
                HealthScore = healthScore,
                CalculationDuration = calculationDuration
            });

            return healthScore;
        }

        /// <summary>
        /// Analyze load patterns
        /// </summary>
        public async Task<LoadPatternData> AnalyzeLoadPatternsAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            // Collect current metrics
            var allMetrics = await _aggregator.CollectAllMetricsAsync(cancellationToken);
            var metricsDict = ConvertMetricsToDictionary(allMetrics);

            // Analyze load patterns
            var loadPatternData = await _systemAnalyzer.AnalyzeLoadPatternsAsync(metricsDict, cancellationToken);

            var analysisDuration = DateTime.UtcNow - startTime;

            // Publish event
            await _publisher.PublishLoadPatternAnalyzedAsync(new LoadPatternAnalyzedEventArgs
            {
                LoadPatternData = loadPatternData,
                AnalysisDuration = analysisDuration
            });

            return loadPatternData;
        }

        /// <summary>
        /// Generate optimization recommendations
        /// </summary>
        public IEnumerable<OptimizationRecommendation> GenerateRecommendations()
        {
            var metrics = GetLatestMetricsAsDictionary();
            return _systemAnalyzer.GenerateRecommendations(metrics);
        }

        /// <summary>
        /// Record a prediction outcome
        /// </summary>
        public void RecordPredictionOutcome(OptimizationStrategy strategy, TimeSpan predictedImprovement, TimeSpan actualImprovement, TimeSpan baselineExecutionTime)
        {
            _systemAnalyzer.RecordPredictionOutcome(strategy, predictedImprovement, actualImprovement, baselineExecutionTime);
        }







        /// <summary>
        /// Get strategy effectiveness data for a specific strategy
        /// </summary>
        public StrategyEffectivenessData GetStrategyEffectiveness(OptimizationStrategy strategy)
        {
            return _systemAnalyzer.GetStrategyEffectiveness(strategy);
        }

        /// <summary>
        /// Get all strategy effectiveness data
        /// </summary>
        public IEnumerable<StrategyEffectivenessData> GetAllStrategyEffectiveness()
        {
            return _systemAnalyzer.GetAllStrategyEffectiveness();
        }











        /// <summary>
        /// Get latest metrics as dictionary (for legacy compatibility)
        /// </summary>
        private Dictionary<string, double> GetLatestMetricsAsDictionary()
        {
            var allMetrics = _aggregator.GetLatestMetrics();
            return ConvertMetricsToDictionary(allMetrics);
        }

        /// <summary>
        /// Convert new metric format to legacy dictionary format
        /// </summary>
        private Dictionary<string, double> ConvertMetricsToDictionary(Dictionary<string, IEnumerable<MetricValue>> allMetrics)
        {
            var result = new Dictionary<string, double>();

            foreach (var collectorMetrics in allMetrics)
            {
                foreach (var metric in collectorMetrics.Value)
                {
                    result[metric.Name] = metric.Value;
                }
            }

            // Add test metrics if available
            if (_testMetrics != null)
            {
                foreach (var kvp in _testMetrics)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate score for a specific health aspect
        /// </summary>
        private async Task<double> CalculateAspectScoreAsync(string scorerName, Dictionary<string, double> metrics, CancellationToken cancellationToken)
        {
            // This is a simplified implementation - in a real system,
            // we'd have individual scorers for each aspect
            if (_healthScorer is CompositeHealthScorer compositeScorer)
            {
                // For composite scorer, we can't easily get individual scores
                // Return a default value based on the scorer name
                return scorerName switch
                {
                    "PerformanceScorer" => CalculatePerformanceScore(metrics),
                    "ReliabilityScorer" => CalculateReliabilityScore(metrics),
                    "ScalabilityScorer" => CalculateScalabilityScore(metrics),
                    "SecurityScorer" => CalculateSecurityScore(metrics),
                    "MaintainabilityScorer" => CalculateMaintainabilityScore(metrics),
                    _ => 0.5
                };
            }

            return await _healthScorer.CalculateScoreAsync(metrics, cancellationToken);
        }

        /// <summary>
        /// Legacy performance score calculation
        /// </summary>
        private double CalculatePerformanceScore(Dictionary<string, double> metrics)
        {
            var cpuScore = 1.0 - metrics.GetValueOrDefault("CpuUtilization", 0.5);
            var memoryScore = 1.0 - metrics.GetValueOrDefault("MemoryUtilization", 0.5);
            var throughputScore = Math.Min(metrics.GetValueOrDefault("ThroughputPerSecond", 100) / 1000.0, 1.0);
            return (cpuScore + memoryScore + throughputScore) / 3.0;
        }

        /// <summary>
        /// Legacy reliability score calculation
        /// </summary>
        private double CalculateReliabilityScore(Dictionary<string, double> metrics)
        {
            var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1);
            var reliabilityScore = 1.0 - Math.Min(errorRate, 1.0);
            var loadAverage = metrics.GetValueOrDefault("SystemLoadAverage", 1.0);
            var stabilityScore = Math.Max(0, 1.0 - loadAverage / 10.0);
            return (reliabilityScore + stabilityScore) / 2.0;
        }

        /// <summary>
        /// Legacy scalability score calculation
        /// </summary>
        private double CalculateScalabilityScore(Dictionary<string, double> metrics)
        {
            var threadCount = metrics.GetValueOrDefault("ThreadCount", 50);
            var handleCount = metrics.GetValueOrDefault("HandleCount", 1000);
            var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 100);
            var threadEfficiency = Math.Min(throughput / Math.Max(threadCount, 1), 10.0) / 10.0;
            var handleEfficiency = Math.Min(throughput / Math.Max(handleCount / 100.0, 1), 10.0) / 10.0;
            return (threadEfficiency + handleEfficiency) / 2.0;
        }

        /// <summary>
        /// Legacy security score calculation
        /// </summary>
        private double CalculateSecurityScore(Dictionary<string, double> metrics)
        {
            // Simplified security score
            var failedAuthAttempts = metrics.GetValueOrDefault("FailedAuthAttempts", 0);
            var knownVulnerabilities = metrics.GetValueOrDefault("KnownVulnerabilities", 0);
            var dataEncryptionEnabled = metrics.GetValueOrDefault("DataEncryptionEnabled", 1);

            var authScore = Math.Max(0, 1.0 - (failedAuthAttempts / 100.0));
            var vulnScore = Math.Max(0, 1.0 - (knownVulnerabilities / 10.0));

            return (authScore + vulnScore + dataEncryptionEnabled) / 3.0;
        }

        /// <summary>
        /// Legacy maintainability score calculation
        /// </summary>
        private double CalculateMaintainabilityScore(Dictionary<string, double> metrics)
        {
            var errorRate = metrics.GetValueOrDefault("ErrorRate", 0.1);
            var maintainabilityScore = 1.0 - Math.Min(errorRate * 2, 1.0);
            var cpuUtil = metrics.GetValueOrDefault("CpuUtilization", 0.5);
            var memoryUtil = metrics.GetValueOrDefault("MemoryUtilization", 0.5);
            var resourceScore = 1.0 - Math.Max(cpuUtil, memoryUtil);
            return (maintainabilityScore + resourceScore) / 2.0;
        }

        /// <summary>
        /// Get health status from score
        /// </summary>
        private string GetHealthStatus(double overallScore)
        {
            if (overallScore > _healthOptions.Thresholds.Excellent) return "Excellent";
            if (overallScore > _healthOptions.Thresholds.Good) return "Good";
            if (overallScore > _healthOptions.Thresholds.Fair) return "Fair";
            if (overallScore > _healthOptions.Thresholds.Poor) return "Poor";
            return "Critical";
        }

        /// <summary>
        /// Event handler for metrics collected
        /// </summary>
        private void OnMetricsCollected(object? sender, MetricsCollectedEventArgs e)
        {
            _logger.LogDebug("Metrics collected from {Collector}: {Count} metrics in {Duration}ms",
                e.CollectorName, e.Metrics.Count(), e.CollectionDuration.TotalMilliseconds);
        }

        /// <summary>
        /// Event handler for health score calculated
        /// </summary>
        private void OnHealthScoreCalculated(object? sender, HealthScoreCalculatedEventArgs e)
        {
            _logger.LogInformation("Health score calculated: {Score:F2} ({Status}) in {Duration}ms",
                e.HealthScore.Overall, e.HealthScore.Status, e.CalculationDuration.TotalMilliseconds);
        }

        /// <summary>
        /// Event handler for load pattern analyzed
        /// </summary>
        private void OnLoadPatternAnalyzed(object? sender, LoadPatternAnalyzedEventArgs e)
        {
            _logger.LogDebug("Load pattern analyzed: Level={Level}, SuccessRate={Rate:F2} in {Duration}ms",
                e.LoadPatternData.Level, e.LoadPatternData.SuccessRate, e.AnalysisDuration.TotalMilliseconds);
        }

        /// <summary>
        /// Set test metrics for testing purposes (legacy compatibility)
        /// </summary>
        public void SetTestMetrics(Dictionary<string, double> testMetrics)
        {
            _testMetrics = testMetrics;
        }

        /// <summary>
        /// Get current metrics as dictionary (including test metrics for testing)
        /// </summary>
        public Dictionary<string, double> GetCurrentMetricsAsDictionary()
        {
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