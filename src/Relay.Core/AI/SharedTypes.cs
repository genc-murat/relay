using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI
{
    /// <summary>
    /// Request analysis data for tracking execution metrics and patterns
    /// </summary>
    internal class RequestAnalysisData
    {
        public long TotalExecutions { get; private set; }
        public long SuccessfulExecutions { get; private set; }
        public long FailedExecutions { get; private set; }
        public TimeSpan AverageExecutionTime { get; private set; }
        public long RepeatRequestCount { get; private set; }
        public int ConcurrentExecutionPeaks { get; private set; }
        public double ErrorRate => TotalExecutions > 0 ? (double)FailedExecutions / TotalExecutions : 0;
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;
        public DateTime LastActivityTime { get; private set; } = DateTime.UtcNow;

        public int ExecutionTimesCount => _executionTimes.Count;
        public int OptimizationResultsCount => _optimizationResults.Count;
        public int HistoricalMetricsCount => _historicalMetrics.Count;

        // Additional metrics for ML.NET
        public double DatabaseCalls { get; set; }
        public double ExternalApiCalls { get; set; }
        public double CacheHitRatio { get; set; }
        public double RepeatRequestRate { get; set; }

        private readonly List<TimeSpan> _executionTimes = new();
        private readonly List<OptimizationResult> _optimizationResults = new();
        private readonly Dictionary<DateTime, RequestExecutionMetrics> _historicalMetrics = new();

        public void AddMetrics(RequestExecutionMetrics metrics)
        {
            TotalExecutions += metrics.TotalExecutions;
            SuccessfulExecutions += metrics.SuccessfulExecutions;
            FailedExecutions += metrics.FailedExecutions;
            LastActivityTime = DateTime.UtcNow;

            _executionTimes.Add(metrics.AverageExecutionTime);
            AverageExecutionTime = _executionTimes.Count > 0
                ? TimeSpan.FromMilliseconds(_executionTimes.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;

            ConcurrentExecutionPeaks = Math.Max(ConcurrentExecutionPeaks, metrics.ConcurrentExecutions);
            _historicalMetrics[DateTime.UtcNow] = metrics;

            UpdateRepeatRequestCount(metrics);
        }

        public void AddOptimizationResult(OptimizationResult result)
        {
            _optimizationResults.Add(result);
            LastActivityTime = DateTime.UtcNow;
        }

        public double CalculateExecutionVariance()
        {
            if (_executionTimes.Count < 2) return 0;

            var avg = _executionTimes.Average(t => t.TotalMilliseconds);
            var variance = _executionTimes.Sum(t => Math.Pow(t.TotalMilliseconds - avg, 2)) / _executionTimes.Count;
            return Math.Sqrt(variance) / avg;
        }

        public double CalculatePerformanceTrend()
        {
            if (_historicalMetrics.Count < 2) return 0;

            var sortedMetrics = _historicalMetrics.OrderBy(kvp => kvp.Key).ToArray();
            var oldAvg = sortedMetrics.Take(sortedMetrics.Length / 2)
                .Average(kvp => kvp.Value.AverageExecutionTime.TotalMilliseconds);
            var newAvg = sortedMetrics.Skip(sortedMetrics.Length / 2)
                .Average(kvp => kvp.Value.AverageExecutionTime.TotalMilliseconds);

            return (newAvg - oldAvg) / oldAvg;
        }

        public OptimizationStrategy[] GetMostEffectiveStrategies()
        {
            if (_optimizationResults.Count == 0) return Array.Empty<OptimizationStrategy>();

            return _optimizationResults
                .GroupBy(r => r.Strategy)
                .OrderByDescending(g => g.Average(r => r.ActualMetrics.SuccessRate))
                .Select(g => g.Key)
                .Take(3)
                .ToArray();
        }

        public int CleanupOldData(DateTime cutoffTime)
        {
            var itemsRemoved = 0;
            var metricsKeysToRemove = _historicalMetrics.Keys.Where(k => k < cutoffTime).ToArray();
            foreach (var key in metricsKeysToRemove)
            {
                if (_historicalMetrics.Remove(key))
                    itemsRemoved++;
            }
            return itemsRemoved;
        }

        public int TrimExecutionTimes(int maxCount)
        {
            if (_executionTimes.Count <= maxCount) return 0;

            var itemsToRemove = _executionTimes.Count - maxCount;
            _executionTimes.RemoveRange(0, itemsToRemove);

            AverageExecutionTime = _executionTimes.Count > 0
                ? TimeSpan.FromMilliseconds(_executionTimes.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;

            return itemsToRemove;
        }

        public int CleanupOptimizationResults(DateTime cutoffTime)
        {
            var initialCount = _optimizationResults.Count;

            for (int i = _optimizationResults.Count - 1; i >= 0; i--)
            {
                if (_optimizationResults[i].Timestamp < cutoffTime)
                {
                    _optimizationResults.RemoveAt(i);
                }
            }

            return initialCount - _optimizationResults.Count;
        }

        private void UpdateRepeatRequestCount(RequestExecutionMetrics metrics)
        {
            if (metrics.AverageExecutionTime.TotalMilliseconds < 10)
            {
                RepeatRequestCount++;
            }
        }
    }

    /// <summary>
    /// Optimization result tracking
    /// </summary>
    internal class OptimizationResult
    {
        public OptimizationStrategy Strategy { get; init; }
        public RequestExecutionMetrics ActualMetrics { get; init; } = null!;
        public DateTime Timestamp { get; init; }
    }

    /// <summary>
    /// Prediction result for ML model training and evaluation
    /// </summary>
    internal class PredictionResult
    {
        public Type RequestType { get; init; } = null!;
        public OptimizationStrategy[] PredictedStrategies { get; init; } = Array.Empty<OptimizationStrategy>();
        public TimeSpan ActualImprovement { get; init; }
        public DateTime Timestamp { get; init; }
        public RequestExecutionMetrics Metrics { get; init; } = null!;
    }

    /// <summary>
    /// Model statistics for monitoring ML performance
    /// </summary>
    internal class ModelStatistics
    {
        public double AccuracyScore { get; set; }
        public double PrecisionScore { get; set; }
        public double RecallScore { get; set; }
        public double F1Score { get; set; }
        public double ModelConfidence { get; set; }
        public long TotalPredictions { get; set; }
        public long CorrectPredictions { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Caching analysis data for tracking cache performance
    /// </summary>
    internal class CachingAnalysisData
    {
        public double CacheHitRate { get; private set; }
        public long TotalAccesses { get; private set; }
        public long CacheHits { get; private set; }
        public DateTime LastAccessTime { get; private set; } = DateTime.UtcNow;

        public int AccessPatternsCount => _accessPatterns.Count;

        private readonly List<AccessPattern> _accessPatterns = new();

        public void AddAccessPatterns(AccessPattern[] patterns)
        {
            _accessPatterns.AddRange(patterns);
            TotalAccesses += patterns.Length;
            CacheHits += patterns.Count(p => p.WasCacheHit);
            CacheHitRate = TotalAccesses > 0 ? (double)CacheHits / TotalAccesses : 0;
            LastAccessTime = DateTime.UtcNow;
        }

        public int CleanupOldAccessPatterns(DateTime cutoffTime)
        {
            var initialCount = _accessPatterns.Count;

            for (int i = _accessPatterns.Count - 1; i >= 0; i--)
            {
                if (_accessPatterns[i].Timestamp < cutoffTime)
                {
                    var pattern = _accessPatterns[i];
                    _accessPatterns.RemoveAt(i);

                    TotalAccesses--;
                    if (pattern.WasCacheHit)
                        CacheHits--;
                }
            }

            CacheHitRate = TotalAccesses > 0 ? (double)CacheHits / TotalAccesses : 0;
            return initialCount - _accessPatterns.Count;
        }
    }
}
