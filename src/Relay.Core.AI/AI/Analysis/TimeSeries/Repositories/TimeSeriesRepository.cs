using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Repository for time-series data storage and retrieval
/// </summary>
internal class TimeSeriesRepository : ITimeSeriesRepository
{
    private readonly ILogger<TimeSeriesRepository> _logger;
    private readonly ConcurrentDictionary<string, CircularBuffer<MetricDataPoint>> _metricHistories;
    private readonly int _maxHistorySize;

    public TimeSeriesRepository(ILogger<TimeSeriesRepository> logger, int maxHistorySize = 10000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxHistorySize = maxHistorySize;
        _metricHistories = new ConcurrentDictionary<string, CircularBuffer<MetricDataPoint>>();

        _logger.LogInformation("Time-series repository initialized with max history size: {MaxSize}", _maxHistorySize);
    }

    /// <inheritdoc/>
    public void StoreMetric(string metricName, double value, DateTime timestamp,
        double? movingAverage5 = null, double? movingAverage15 = null,
        TrendDirection trend = TrendDirection.Stable)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        try
        {
            var dataPoint = new MetricDataPoint
            {
                MetricName = metricName,
                Timestamp = timestamp,
                Value = (float)value,
                MA5 = movingAverage5.HasValue ? (float)movingAverage5.Value : (float)value,
                MA15 = movingAverage15.HasValue ? (float)movingAverage15.Value : (float)value,
                Trend = (int)trend,
                HourOfDay = timestamp.Hour,
                DayOfWeek = (int)timestamp.DayOfWeek
            };

            var history = _metricHistories.GetOrAdd(metricName,
                _ => new CircularBuffer<MetricDataPoint>(_maxHistorySize));

            history.Add(dataPoint);

            _logger.LogTrace("Stored metric {MetricName} with value {Value} at {Timestamp}",
                metricName, value, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error storing metric {MetricName}", metricName);
            throw new TimeSeriesException($"Failed to store metric {metricName}", ex);
        }
    }

    /// <inheritdoc/>
    public void StoreBatch(Dictionary<string, double> metrics, DateTime timestamp,
        Dictionary<string, MovingAverageData>? movingAverages = null,
        Dictionary<string, TrendDirection>? trendDirections = null)
    {
        if (metrics == null || metrics.Count == 0)
            return;

        foreach (var metric in metrics)
        {
            var ma = movingAverages?.GetValueOrDefault(metric.Key);
            var trend = trendDirections?.GetValueOrDefault(metric.Key, TrendDirection.Stable) ?? TrendDirection.Stable;

            StoreMetric(metric.Key, metric.Value, timestamp, ma?.MA5, ma?.MA15, trend);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<MetricDataPoint> GetHistory(string metricName, TimeSpan? lookbackPeriod = null)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        if (!_metricHistories.TryGetValue(metricName, out var history))
        {
            return Enumerable.Empty<MetricDataPoint>();
        }

        var allData = history.AsEnumerable();

        if (lookbackPeriod.HasValue)
        {
            var cutoffTime = DateTime.UtcNow - lookbackPeriod.Value;
            return allData.Where(d => d.Timestamp >= cutoffTime).OrderBy(d => d.Timestamp);
        }

        return allData.OrderBy(d => d.Timestamp);
    }

    /// <inheritdoc/>
    public List<MetricDataPoint> GetRecentMetrics(string metricName, int count)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        if (count <= 0)
            return new List<MetricDataPoint>();

        if (!_metricHistories.TryGetValue(metricName, out var history))
        {
            return new List<MetricDataPoint>();
        }

        return history
            .OrderByDescending(d => d.Timestamp)
            .Take(count)
            .OrderBy(d => d.Timestamp) // Re-order chronologically
            .ToList();
    }

    /// <inheritdoc/>
    public void CleanupOldData(TimeSpan retentionPeriod)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - retentionPeriod;
            var totalRemoved = 0;

            foreach (var kvp in _metricHistories)
            {
                var history = kvp.Value;
                var originalCount = history.Count;

                // Remove old data points efficiently
                var oldCount = history.Where(d => d.Timestamp < cutoffTime).Count();
                history.RemoveFront(oldCount);

                totalRemoved += originalCount - history.Count;
            }

            if (totalRemoved > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old time-series data points", totalRemoved);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old time-series data");
            throw new TimeSeriesException("Failed to cleanup old data", ex);
        }
    }

    /// <summary>
    /// Clear all stored time-series data
    /// </summary>
    public void Clear()
    {
        _metricHistories.Clear();
        _logger.LogInformation("Cleared all time-series data");
    }
}