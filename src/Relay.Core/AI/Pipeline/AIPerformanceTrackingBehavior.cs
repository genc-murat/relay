using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.AI
{
    /// <summary>
    /// Pipeline behavior for tracking AI-related performance metrics.
    /// </summary>
    internal class AIPerformanceTrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<AIPerformanceTrackingBehavior<TRequest, TResponse>> _logger;
        private readonly IAIMetricsExporter? _metricsExporter;
        private readonly AIPerformanceTrackingOptions _options;
        private readonly ConcurrentDictionary<Type, PerformanceMetricsAggregator> _aggregators;
        private readonly Timer? _exportTimer;

        public AIPerformanceTrackingBehavior(
            ILogger<AIPerformanceTrackingBehavior<TRequest, TResponse>> logger,
            IAIMetricsExporter? metricsExporter = null,
            AIPerformanceTrackingOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsExporter = metricsExporter;
            _options = options ?? new AIPerformanceTrackingOptions();
            _aggregators = new ConcurrentDictionary<Type, PerformanceMetricsAggregator>();

            if (_options.EnablePeriodicExport && _metricsExporter != null)
            {
                _exportTimer = new Timer(
                    ExportMetricsCallback,
                    null,
                    _options.ExportInterval,
                    _options.ExportInterval);
            }
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_options.EnableTracking)
            {
                return await next();
            }

            var stopwatch = Stopwatch.StartNew();
            var requestType = typeof(TRequest);
            var aggregator = _aggregators.GetOrAdd(requestType, _ => new PerformanceMetricsAggregator(_options));

            try
            {
                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("AI Performance tracking started for request: {RequestType}", requestType.Name);
                }

                var response = await next();

                stopwatch.Stop();

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogDebug("AI Performance tracking completed for request: {RequestType} in {ElapsedMs}ms",
                        requestType.Name, stopwatch.ElapsedMilliseconds);
                }

                // Track successful execution
                await TrackPerformanceMetricsAsync(requestType, aggregator, stopwatch.Elapsed, success: true, cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogWarning(ex, "AI Performance tracking detected error for request: {RequestType} after {ElapsedMs}ms",
                    requestType.Name, stopwatch.ElapsedMilliseconds);

                // Track failed execution
                await TrackPerformanceMetricsAsync(requestType, aggregator, stopwatch.Elapsed, success: false, cancellationToken);

                throw;
            }
        }

        private async ValueTask TrackPerformanceMetricsAsync(
            Type requestType,
            PerformanceMetricsAggregator aggregator,
            TimeSpan elapsed,
            bool success,
            CancellationToken cancellationToken)
        {
            // Add to aggregator
            aggregator.AddMetric(elapsed, success);

            if (_options.EnableDetailedLogging)
            {
                var stats = aggregator.GetStatistics();
                _logger.LogTrace(
                    "Performance metric tracked: {RequestType}, Duration: {Duration}ms, Success: {Success}, " +
                    "Avg: {Avg}ms, P50: {P50}ms, P95: {P95}ms, P99: {P99}ms, ErrorRate: {ErrorRate:P2}",
                    requestType.Name,
                    elapsed.TotalMilliseconds,
                    success,
                    stats.AverageDuration.TotalMilliseconds,
                    stats.P50.TotalMilliseconds,
                    stats.P95.TotalMilliseconds,
                    stats.P99.TotalMilliseconds,
                    stats.ErrorRate);
            }

            // Export immediately if threshold reached
            if (_options.EnableImmediateExport &&
                _metricsExporter != null &&
                aggregator.ShouldExport())
            {
                await ExportMetricsAsync(requestType, aggregator, cancellationToken);
            }
        }

        private async void ExportMetricsCallback(object? state)
        {
            if (_metricsExporter == null)
                return;

            try
            {
                foreach (var kvp in _aggregators)
                {
                    await ExportMetricsAsync(kvp.Key, kvp.Value, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting performance metrics");
            }
        }

        private async ValueTask ExportMetricsAsync(
            Type requestType,
            PerformanceMetricsAggregator aggregator,
            CancellationToken cancellationToken)
        {
            try
            {
                var stats = aggregator.GetStatistics();

                var modelStats = new AIModelStatistics
                {
                    ModelVersion = _options.ModelVersion,
                    ModelTrainingDate = DateTime.UtcNow,
                    LastRetraining = DateTime.UtcNow,
                    TotalPredictions = stats.TotalCount,
                    AccuracyScore = stats.SuccessRate,
                    AveragePredictionTime = stats.AverageDuration,
                    ModelConfidence = stats.SuccessRate,
                    TrainingDataPoints = stats.TotalCount,
                    PrecisionScore = stats.SuccessRate,
                    RecallScore = stats.SuccessRate,
                    F1Score = stats.SuccessRate
                };

                await _metricsExporter!.ExportMetricsAsync(modelStats, cancellationToken);

                _logger.LogInformation(
                    "Exported performance metrics for {RequestType}: Count={Count}, Avg={Avg}ms, ErrorRate={ErrorRate:P2}",
                    requestType.Name,
                    stats.TotalCount,
                    stats.AverageDuration.TotalMilliseconds,
                    stats.ErrorRate);

                // Reset aggregator if configured
                if (_options.ResetAfterExport)
                {
                    aggregator.Reset();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export metrics for {RequestType}", requestType.Name);
            }
        }

        public void Dispose()
        {
            _exportTimer?.Dispose();
        }
    }

    /// <summary>
    /// Configuration options for AIPerformanceTrackingBehavior.
    /// </summary>
    public class AIPerformanceTrackingOptions
    {
        /// <summary>
        /// Gets or sets whether performance tracking is enabled.
        /// </summary>
        public bool EnableTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether detailed logging is enabled.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Gets or sets whether periodic metrics export is enabled.
        /// </summary>
        public bool EnablePeriodicExport { get; set; } = true;

        /// <summary>
        /// Gets or sets whether immediate export on threshold is enabled.
        /// </summary>
        public bool EnableImmediateExport { get; set; } = false;

        /// <summary>
        /// Gets or sets the interval for periodic metrics export.
        /// </summary>
        public TimeSpan ExportInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the threshold for immediate export (number of metrics).
        /// </summary>
        public int ImmediateExportThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to reset metrics after export.
        /// </summary>
        public bool ResetAfterExport { get; set; } = true;

        /// <summary>
        /// Gets or sets the sliding window size for metrics aggregation.
        /// </summary>
        public int SlidingWindowSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the model version identifier.
        /// </summary>
        public string ModelVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets whether to track percentiles (P50, P95, P99).
        /// </summary>
        public bool TrackPercentiles { get; set; } = true;
    }

    /// <summary>
    /// Aggregates performance metrics with sliding window support.
    /// </summary>
    internal class PerformanceMetricsAggregator
    {
        private readonly AIPerformanceTrackingOptions _options;
        private readonly Queue<MetricEntry> _metrics;
        private readonly object _lock = new object();
        private long _totalCount;
        private long _successCount;
        private long _errorCount;

        public PerformanceMetricsAggregator(AIPerformanceTrackingOptions options)
        {
            _options = options;
            _metrics = new Queue<MetricEntry>();
        }

        public void AddMetric(TimeSpan duration, bool success)
        {
            lock (_lock)
            {
                _metrics.Enqueue(new MetricEntry
                {
                    Duration = duration,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                });

                // Maintain sliding window
                while (_metrics.Count > _options.SlidingWindowSize)
                {
                    _metrics.Dequeue();
                }

                Interlocked.Increment(ref _totalCount);
                if (success)
                {
                    Interlocked.Increment(ref _successCount);
                }
                else
                {
                    Interlocked.Increment(ref _errorCount);
                }
            }
        }

        public bool ShouldExport()
        {
            return _metrics.Count >= _options.ImmediateExportThreshold;
        }

        public PerformanceStatistics GetStatistics()
        {
            lock (_lock)
            {
                if (_metrics.Count == 0)
                {
                    return new PerformanceStatistics();
                }

                var durations = _metrics.Select(m => m.Duration.TotalMilliseconds).OrderBy(d => d).ToList();
                var successfulMetrics = _metrics.Where(m => m.Success).ToList();

                var stats = new PerformanceStatistics
                {
                    TotalCount = _totalCount,
                    SuccessCount = _successCount,
                    ErrorCount = _errorCount,
                    SuccessRate = _totalCount > 0 ? (double)_successCount / _totalCount : 0.0,
                    ErrorRate = _totalCount > 0 ? (double)_errorCount / _totalCount : 0.0,
                    AverageDuration = TimeSpan.FromMilliseconds(durations.Average()),
                    MinDuration = TimeSpan.FromMilliseconds(durations.First()),
                    MaxDuration = TimeSpan.FromMilliseconds(durations.Last())
                };

                if (_options.TrackPercentiles && durations.Count > 0)
                {
                    stats.P50 = TimeSpan.FromMilliseconds(GetPercentile(durations, 0.50));
                    stats.P95 = TimeSpan.FromMilliseconds(GetPercentile(durations, 0.95));
                    stats.P99 = TimeSpan.FromMilliseconds(GetPercentile(durations, 0.99));
                }

                return stats;
            }
        }

        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0)
                return 0;

            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
            return sortedValues[index];
        }

        public void Reset()
        {
            lock (_lock)
            {
                _metrics.Clear();
                Interlocked.Exchange(ref _totalCount, 0);
                Interlocked.Exchange(ref _successCount, 0);
                Interlocked.Exchange(ref _errorCount, 0);
            }
        }

        private class MetricEntry
        {
            public TimeSpan Duration { get; set; }
            public bool Success { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    /// <summary>
    /// Performance statistics for a request type.
    /// </summary>
    public class PerformanceStatistics
    {
        public long TotalCount { get; set; }
        public long SuccessCount { get; set; }
        public long ErrorCount { get; set; }
        public double SuccessRate { get; set; }
        public double ErrorRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan P50 { get; set; }
        public TimeSpan P95 { get; set; }
        public TimeSpan P99 { get; set; }
    }
}
