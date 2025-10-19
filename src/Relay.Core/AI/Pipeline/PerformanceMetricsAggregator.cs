using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Relay.Core.AI
{
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
}
