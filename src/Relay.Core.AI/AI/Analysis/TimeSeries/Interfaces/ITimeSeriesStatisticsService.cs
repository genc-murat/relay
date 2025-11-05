using System;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for time-series statistics operations
/// </summary>
public interface ITimeSeriesStatisticsService
{
    /// <summary>
    /// Calculate statistics for a metric
    /// </summary>
    MetricStatistics? GetStatistics(string metricName, TimeSpan? period = null);
}