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
    public class DataCleanupManagerPredictionTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerPredictionTests()
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

        #region Prediction Results Tests

        [Fact]
        public void CleanupOldData_Should_Remove_Multiple_Old_Predictions()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add multiple old predictions
            for (int i = 0; i < 10; i++)
            {
                var oldPrediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = oldTime.AddMinutes(i),
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(oldPrediction);
            }

            // Add a recent one at the end
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(int),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(recentPrediction);

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Single(_recentPredictions); // Only recent one should remain
        }

        [Fact]
        public void CleanupOldData_Should_Stop_At_First_Recent_Prediction()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add a recent prediction first
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(int),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(recentPrediction);

            // Add old predictions after
            for (int i = 0; i < 5; i++)
            {
                var oldPrediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = oldTime,
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(oldPrediction);
            }

            var initialCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert - Should stop at first recent, keeping all after it
            Assert.Equal(initialCount, _recentPredictions.Count);
        }

        #endregion
    }
}