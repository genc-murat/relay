using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.AI.Pipeline.Metrics;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.AI.Metrics.Interfaces;
using Relay.Core.AI;

namespace Relay.Core.AI.Pipeline.Behaviors;

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

        var requestType = typeof(TRequest);
        var monitoringConfig = GetMonitoringConfiguration(requestType);

        // Skip tracking if sampling rate doesn't include this request
        if (monitoringConfig.SamplingRate < 1.0 && Random.Shared.NextDouble() > monitoringConfig.SamplingRate)
        {
            return await next();
        }

        var stopwatch = Stopwatch.StartNew();
        var aggregator = _aggregators.GetOrAdd(requestType, _ => new PerformanceMetricsAggregator(_options));

        try
        {
            if (_options.EnableDetailedLogging && monitoringConfig.CollectDetailedMetrics)
            {
                _logger.LogDebug("AI Performance tracking started for request: {RequestType} with monitoring level: {Level}",
                    requestType.Name, monitoringConfig.Level);
            }

            var response = await next();

            stopwatch.Stop();

            if (_options.EnableDetailedLogging && monitoringConfig.CollectDetailedMetrics)
            {
                _logger.LogDebug("AI Performance tracking completed for request: {RequestType} in {ElapsedMs}ms",
                    requestType.Name, stopwatch.ElapsedMilliseconds);
            }

            // Track successful execution
            await TrackPerformanceMetricsAsync(requestType, aggregator, stopwatch.Elapsed, success: true, monitoringConfig, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (monitoringConfig.CollectDetailedMetrics)
            {
                _logger.LogWarning(ex, "AI Performance tracking detected error for request: {RequestType} after {ElapsedMs}ms",
                    requestType.Name, stopwatch.ElapsedMilliseconds);
            }

            // Track failed execution
            await TrackPerformanceMetricsAsync(requestType, aggregator, stopwatch.Elapsed, success: false, monitoringConfig, cancellationToken);

            throw;
        }
    }

    private async ValueTask TrackPerformanceMetricsAsync(
        Type requestType,
        PerformanceMetricsAggregator aggregator,
        TimeSpan elapsed,
        bool success,
        (MonitoringLevel Level, bool CollectDetailedMetrics, bool TrackAccessPatterns, double SamplingRate, string[] Tags) monitoringConfig,
        CancellationToken cancellationToken)
    {
        // Add to aggregator
        aggregator.AddMetric(elapsed, success);

        if (_options.EnableDetailedLogging && monitoringConfig.CollectDetailedMetrics)
        {
            var stats = aggregator.GetStatistics();
            _logger.LogTrace(
                "Performance metric tracked: {RequestType}, Duration: {Duration}ms, Success: {Success}, " +
                "Avg: {Avg}ms, P50: {P50}ms, P95: {P95}ms, P99: {P99}ms, ErrorRate: {ErrorRate:P2}, Level: {Level}",
                requestType.Name,
                elapsed.TotalMilliseconds,
                success,
                stats.AverageDuration.TotalMilliseconds,
                stats.P50.TotalMilliseconds,
                stats.P95.TotalMilliseconds,
                stats.P99.TotalMilliseconds,
                stats.ErrorRate,
                monitoringConfig.Level);
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

    private (MonitoringLevel Level, bool CollectDetailedMetrics, bool TrackAccessPatterns, double SamplingRate, string[] Tags) GetMonitoringConfiguration(Type requestType)
    {
        var attribute = requestType.GetCustomAttribute<AIMonitoredAttribute>();
        if (attribute != null)
        {
            return (attribute.Level, attribute.CollectDetailedMetrics, attribute.TrackAccessPatterns, attribute.SamplingRate, attribute.Tags);
        }

        // Default configuration
        return (MonitoringLevel.Standard, true, true, 1.0, Array.Empty<string>());
    }

    public void Dispose()
    {
        _exportTimer?.Dispose();
    }
}
