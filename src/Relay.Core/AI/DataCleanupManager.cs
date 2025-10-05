using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Manages cleanup of old data to prevent memory leaks.
    /// </summary>
    internal class DataCleanupManager
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManager(
            ILogger<DataCleanupManager> logger,
            ConcurrentDictionary<Type, RequestAnalysisData> requestAnalytics,
            ConcurrentDictionary<Type, CachingAnalysisData> cachingAnalytics,
            ConcurrentQueue<PredictionResult> recentPredictions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requestAnalytics = requestAnalytics ?? throw new ArgumentNullException(nameof(requestAnalytics));
            _cachingAnalytics = cachingAnalytics ?? throw new ArgumentNullException(nameof(cachingAnalytics));
            _recentPredictions = recentPredictions ?? throw new ArgumentNullException(nameof(recentPredictions));
        }

        public void CleanupOldData()
        {
            try
            {
                var stats = new DataCleanupStatistics
                {
                    StartTime = DateTime.UtcNow,
                    CutoffTime = DateTime.UtcNow.AddHours(-24)
                };

                var cutoffTime = stats.CutoffTime;

                CleanupRequestAnalyticsData(cutoffTime, stats);
                CleanupCachingAnalyticsData(cutoffTime, stats);
                CleanupPredictionResults(cutoffTime, stats);
                TrimExecutionTimeCollections(stats);
                CleanupOptimizationResults(cutoffTime, stats);

                stats.EndTime = DateTime.UtcNow;
                stats.Duration = stats.EndTime - stats.StartTime;

                LogDetailedCleanupStatistics(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data cleanup operation");
            }
        }

        private void CleanupRequestAnalyticsData(DateTime cutoffTime, DataCleanupStatistics stats)
        {
            var keysToRemove = _requestAnalytics
                .Where(kvp => kvp.Value.LastActivityTime < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToArray();

            foreach (var key in keysToRemove)
            {
                if (_requestAnalytics.TryRemove(key, out var removed))
                {
                    stats.RequestAnalyticsRemoved++;
                    stats.InternalDataItemsRemoved += removed.ExecutionTimesCount;
                    stats.EstimatedMemoryFreed += EstimateRequestAnalyticsMemoryUsage(removed);
                }
            }

            foreach (var kvp in _requestAnalytics)
            {
                var itemsRemoved = kvp.Value.CleanupOldData(cutoffTime);
                if (itemsRemoved > 0)
                {
                    stats.InternalDataItemsRemoved += itemsRemoved;
                    _logger.LogTrace("Cleaned up {ItemsRemoved} old metrics from {RequestType}", itemsRemoved, kvp.Key.Name);
                }
            }
        }

        private void CleanupCachingAnalyticsData(DateTime cutoffTime, DataCleanupStatistics stats)
        {
            var keysToRemove = _cachingAnalytics
                .Where(kvp => kvp.Value.LastAccessTime < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToArray();

            foreach (var key in keysToRemove)
            {
                if (_cachingAnalytics.TryRemove(key, out var removed))
                {
                    stats.CachingAnalyticsRemoved++;
                    stats.CachingDataItemsRemoved += removed.AccessPatternsCount;
                    stats.EstimatedMemoryFreed += EstimateCachingAnalyticsMemoryUsage(removed);
                }
            }

            foreach (var kvp in _cachingAnalytics)
            {
                var itemsRemoved = kvp.Value.CleanupOldAccessPatterns(cutoffTime);
                if (itemsRemoved > 0)
                {
                    stats.CachingDataItemsRemoved += itemsRemoved;
                    _logger.LogTrace("Cleaned up {ItemsRemoved} old access patterns from {RequestType}", itemsRemoved, kvp.Key.Name);
                }
            }
        }

        private void CleanupPredictionResults(DateTime cutoffTime, DataCleanupStatistics stats)
        {
            var removed = 0;
            while (_recentPredictions.TryPeek(out var result))
            {
                if (result.Timestamp < cutoffTime)
                {
                    if (_recentPredictions.TryDequeue(out _))
                    {
                        removed++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            stats.PredictionResultsRemoved = removed;
            if (removed > 0)
            {
                stats.EstimatedMemoryFreed += removed * 256;
                _logger.LogTrace("Cleaned up {Count} old prediction results", removed);
            }
        }

        private void TrimExecutionTimeCollections(DataCleanupStatistics stats)
        {
            const int maxExecutionTimes = 1000;

            foreach (var kvp in _requestAnalytics)
            {
                var itemsRemoved = kvp.Value.TrimExecutionTimes(maxExecutionTimes);
                if (itemsRemoved > 0)
                {
                    stats.ExecutionTimesRemoved += itemsRemoved;
                    stats.EstimatedMemoryFreed += itemsRemoved * 16;
                    _logger.LogTrace("Trimmed {ItemsRemoved} execution times from {RequestType}", itemsRemoved, kvp.Key.Name);
                }
            }
        }

        private void CleanupOptimizationResults(DateTime cutoffTime, DataCleanupStatistics stats)
        {
            foreach (var kvp in _requestAnalytics)
            {
                var itemsRemoved = kvp.Value.CleanupOptimizationResults(cutoffTime);
                if (itemsRemoved > 0)
                {
                    stats.OptimizationResultsRemoved += itemsRemoved;
                    stats.EstimatedMemoryFreed += itemsRemoved * 128;
                    _logger.LogTrace("Cleaned up {ItemsRemoved} old optimization results from {RequestType}", itemsRemoved, kvp.Key.Name);
                }
            }
        }

        private long EstimateRequestAnalyticsMemoryUsage(RequestAnalysisData analysisData)
        {
            long baseSize = 256;
            long executionTimeSize = analysisData.ExecutionTimesCount * 16;
            long optimizationResultSize = analysisData.OptimizationResultsCount * 128;
            long historicalMetricsSize = analysisData.HistoricalMetricsCount * 256;
            return baseSize + executionTimeSize + optimizationResultSize + historicalMetricsSize;
        }

        private long EstimateCachingAnalyticsMemoryUsage(CachingAnalysisData cachingData)
        {
            long baseSize = 128;
            long accessPatternSize = cachingData.AccessPatternsCount * 256;
            return baseSize + accessPatternSize;
        }

        private void LogDetailedCleanupStatistics(DataCleanupStatistics stats)
        {
            _logger.LogInformation(
                "Data cleanup completed in {Duration}ms. Removed {TotalItems} items: " +
                "RequestAnalytics={RequestAnalytics}, CachingAnalytics={CachingAnalytics}, " +
                "PredictionResults={PredictionResults}, ExecutionTimes={ExecutionTimes}, " +
                "OptimizationResults={OptimizationResults}. Estimated memory freed: {MemoryFreed} bytes",
                stats.Duration.TotalMilliseconds,
                stats.TotalItemsRemoved,
                stats.RequestAnalyticsRemoved,
                stats.CachingAnalyticsRemoved,
                stats.PredictionResultsRemoved,
                stats.ExecutionTimesRemoved,
                stats.OptimizationResultsRemoved,
                stats.EstimatedMemoryFreed);
        }
    }
}
