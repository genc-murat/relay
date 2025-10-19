using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class DataCleanupManagerIntegrationTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerIntegrationTests()
        {
            _logger = NullLogger<DataCleanupManager>.Instance;
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _cachingAnalytics = new ConcurrentDictionary<Type, CachingAnalysisData>();
            _recentPredictions = new ConcurrentQueue<PredictionResult>();
        }

        private DataCleanupManager CreateManager()
        {
            return new DataCleanupManager(_logger, _requestAnalytics, _cachingAnalytics, _recentPredictions);
        }

        #region Integration Tests

        [Fact]
        public void CleanupOldData_Should_Clean_All_Collection_Types()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add old data to all collections
            var oldRequestData = new RequestAnalysisData();
            var requestProperty = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            requestProperty?.SetValue(oldRequestData, oldTime);
            _requestAnalytics.TryAdd(typeof(string), oldRequestData);

            var oldCachingData = new CachingAnalysisData();
            var cachingProperty = typeof(CachingAnalysisData).GetProperty("LastAccessTime");
            cachingProperty?.SetValue(oldCachingData, oldTime);
            _cachingAnalytics.TryAdd(typeof(double), oldCachingData);

            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(bool),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = oldTime,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(oldPrediction);

            // Act
            manager.CleanupOldData();

            // Assert - All should be cleaned
            Assert.Empty(_requestAnalytics);
            Assert.Empty(_cachingAnalytics);
            Assert.Empty(_recentPredictions);
        }

        [Fact]
        public void CleanupOldData_Should_Preserve_All_Recent_Data()
        {
            // Arrange
            var manager = CreateManager();

            // Add recent data to all collections
            _requestAnalytics.TryAdd(typeof(string), new RequestAnalysisData());
            _requestAnalytics.TryAdd(typeof(int), new RequestAnalysisData());
            _cachingAnalytics.TryAdd(typeof(double), new CachingAnalysisData());
            _cachingAnalytics.TryAdd(typeof(bool), new CachingAnalysisData());

            for (int i = 0; i < 5; i++)
            {
                var prediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = DateTime.UtcNow,
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(prediction);
            }

            var requestCount = _requestAnalytics.Count;
            var cachingCount = _cachingAnalytics.Count;
            var predictionCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert - All should be preserved
            Assert.Equal(requestCount, _requestAnalytics.Count);
            Assert.Equal(cachingCount, _cachingAnalytics.Count);
            Assert.Equal(predictionCount, _recentPredictions.Count);
        }

        #endregion
    }
}