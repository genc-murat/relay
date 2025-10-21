using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Service for calculating time-series statistics
/// </summary>
internal class TimeSeriesStatisticsService : ITimeSeriesStatisticsService
{
    private readonly ILogger<TimeSeriesStatisticsService> _logger;
    private readonly ITimeSeriesRepository _repository;

    public TimeSeriesStatisticsService(
        ILogger<TimeSeriesStatisticsService> logger,
        ITimeSeriesRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc/>
    public MetricStatistics? GetStatistics(string metricName, TimeSpan? period = null)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        try
        {
            var history = _repository.GetHistory(metricName, period).ToList();

            if (history.Count == 0)
            {
                _logger.LogWarning("No data available for statistics calculation for {MetricName}", metricName);
                return null;
            }

            var values = history.Select(h => h.Value).ToArray();

            var statistics = new MetricStatistics
            {
                MetricName = metricName,
                Count = values.Length,
                Mean = values.Average(),
                Min = values.Min(),
                Max = values.Max(),
                StdDev = TimeSeriesStatistics.CalculateStdDev(values),
                Median = TimeSeriesStatistics.CalculateMedian(values),
                P95 = TimeSeriesStatistics.CalculatePercentile(values, 0.95),
                P99 = TimeSeriesStatistics.CalculatePercentile(values, 0.99)
            };

            _logger.LogTrace("Calculated statistics for {MetricName}: count={Count}, mean={Mean:F2}",
                metricName, statistics.Count, statistics.Mean);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for {MetricName}", metricName);
            throw new TimeSeriesException($"Failed to calculate statistics for {metricName}", ex);
        }
    }
}