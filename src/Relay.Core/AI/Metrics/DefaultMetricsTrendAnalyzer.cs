using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI;

/// <summary>
/// Default implementation of metrics trend analyzer.
/// </summary>
internal class DefaultMetricsTrendAnalyzer : IMetricsTrendAnalyzer
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, MetricTrend> _metricTrends = new();

    public DefaultMetricsTrendAnalyzer(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask AnalyzeTrendsAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default)
    {
        TrackMetricTrend("accuracy", statistics.AccuracyScore);
        TrackMetricTrend("precision", statistics.PrecisionScore);
        TrackMetricTrend("recall", statistics.RecallScore);
        TrackMetricTrend("f1_score", statistics.F1Score);
        TrackMetricTrend("confidence", statistics.ModelConfidence);
        TrackMetricTrend("prediction_time_ms", statistics.AveragePredictionTime.TotalMilliseconds);

        // Log trends if significant changes detected
        foreach (var trend in _metricTrends)
        {
            if (Math.Abs(trend.Value.PercentageChange) > 10.0) // 10% change threshold
            {
                var direction = trend.Value.PercentageChange > 0 ? "increased" : "decreased";
                _logger.LogInformation(
                    "Significant trend detected: {MetricName} has {Direction} by {Change:F1}% over the last {Count} exports",
                    trend.Key, direction, Math.Abs(trend.Value.PercentageChange), trend.Value.DataPoints.Count);
            }
        }

        return ValueTask.CompletedTask;
    }

    private void TrackMetricTrend(string metricName, double value)
    {
        var trend = _metricTrends.GetOrAdd(metricName, _ => new MetricTrend());

        trend.DataPoints.Enqueue(value);

        // Keep only last 20 data points
        while (trend.DataPoints.Count > 20)
        {
            trend.DataPoints.TryDequeue(out _);
        }

        // Calculate percentage change
        if (trend.DataPoints.Count >= 2)
        {
            var oldestValue = trend.DataPoints.First();
            var newestValue = trend.DataPoints.Last();

            if (oldestValue != 0)
            {
                trend.PercentageChange = ((newestValue - oldestValue) / oldestValue) * 100.0;
            }
        }
    }

    private class MetricTrend
    {
        public ConcurrentQueue<double> DataPoints { get; } = new();
        public double PercentageChange { get; set; }
    }
}