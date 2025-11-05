using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for time-series data repository operations
/// </summary>
public interface ITimeSeriesRepository
{
    /// <summary>
    /// Store a single metric data point
    /// </summary>
    void StoreMetric(string metricName, double value, DateTime timestamp,
        double? movingAverage5 = null, double? movingAverage15 = null,
        TrendDirection trend = TrendDirection.Stable);

    /// <summary>
    /// Store multiple metrics at once
    /// </summary>
    void StoreBatch(Dictionary<string, double> metrics, DateTime timestamp,
        Dictionary<string, MovingAverageData>? movingAverages = null,
        Dictionary<string, TrendDirection>? trendDirections = null);

    /// <summary>
    /// Get historical data for a specific metric
    /// </summary>
    IEnumerable<MetricDataPoint> GetHistory(string metricName, TimeSpan? lookbackPeriod = null);

    /// <summary>
    /// Get most recent N metrics for a specific metric name
    /// </summary>
    List<MetricDataPoint> GetRecentMetrics(string metricName, int count);

    /// <summary>
    /// Clean up old data beyond retention period
    /// </summary>
    void CleanupOldData(TimeSpan retentionPeriod);

    /// <summary>
    /// Clear all stored time-series data
    /// </summary>
    void Clear();
}