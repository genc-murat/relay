using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.AI.Metrics.Interfaces;

namespace Relay.Core.AI;

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
