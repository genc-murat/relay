using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class DataCleanupManagerCleanupTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerCleanupTests()
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

        [Fact]
        public void CleanupOldData_Should_Execute_Successfully_With_Empty_Collections()
        {
            // Arrange
            var manager = CreateManager();

            // Act - Should not throw
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Request_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var oldData = new RequestAnalysisData();
            var oldTime = DateTime.UtcNow.AddHours(-48); // Older than 24 hours

            // Use reflection to set LastActivityTime
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(oldData, oldTime);

            _requestAnalytics.TryAdd(typeof(string), oldData);
            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_requestAnalytics.Count < initialCount || _requestAnalytics.Count == 0);
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Request_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var recentData = new RequestAnalysisData();

            // LastActivityTime defaults to DateTime.UtcNow in constructor, so it's recent
            _requestAnalytics.TryAdd(typeof(int), recentData);
            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Equal(initialCount, _requestAnalytics.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Caching_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var oldData = new CachingAnalysisData();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Use reflection to set LastAccessTime
            var property = typeof(CachingAnalysisData).GetProperty("LastAccessTime");
            property?.SetValue(oldData, oldTime);

            _cachingAnalytics.TryAdd(typeof(string), oldData);
            var initialCount = _cachingAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_cachingAnalytics.Count < initialCount || _cachingAnalytics.Count == 0);
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Caching_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var recentData = new CachingAnalysisData();

            // LastAccessTime defaults to DateTime.UtcNow in constructor
            _cachingAnalytics.TryAdd(typeof(int), recentData);
            var initialCount = _cachingAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Equal(initialCount, _cachingAnalytics.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Prediction_Results()
        {
            // Arrange
            var manager = CreateManager();
            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = new[] { OptimizationStrategy.EnableCaching },
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow.AddHours(-48),
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    TotalExecutions = 10,
                    SuccessfulExecutions = 9,
                    FailedExecutions = 1
                }
            };

            _recentPredictions.Enqueue(oldPrediction);
            var initialCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_recentPredictions.Count < initialCount);
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Prediction_Results()
        {
            // Arrange
            var manager = CreateManager();
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = new[] { OptimizationStrategy.EnableCaching },
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    TotalExecutions = 10,
                    SuccessfulExecutions = 9,
                    FailedExecutions = 1
                }
            };

            _recentPredictions.Enqueue(recentPrediction);
            var initialCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Equal(initialCount, _recentPredictions.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Mixed_Old_And_Recent_Data()
        {
            // Arrange
            var manager = CreateManager();

            // Add old request analytics
            var oldRequestData = new RequestAnalysisData();
            var oldRequestTime = DateTime.UtcNow.AddHours(-48);
            var requestProperty = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            requestProperty?.SetValue(oldRequestData, oldRequestTime);
            _requestAnalytics.TryAdd(typeof(string), oldRequestData);

            // Add recent request analytics
            var recentRequestData = new RequestAnalysisData();
            _requestAnalytics.TryAdd(typeof(int), recentRequestData);

            // Add old predictions
            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow.AddHours(-48),
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(oldPrediction);

            // Add recent predictions
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
            Assert.True(_requestAnalytics.Count > 0); // Recent data should remain
            Assert.True(_recentPredictions.Count > 0); // Recent predictions should remain
        }

        [Fact]
        public void CleanupOldData_Should_Be_Callable_Multiple_Times()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    manager.CleanupOldData();
                }
            });

            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Large_Collections()
        {
            // Arrange
            var manager = CreateManager();

            // Add many items
            for (int i = 0; i < 1000; i++)
            {
                var data = new RequestAnalysisData();
                _requestAnalytics.TryAdd(Type.GetType($"System.Int{i}") ?? typeof(int), data);
            }

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Empty_Predictions_Queue()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
            Assert.Empty(_recentPredictions);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_All_Old_Data()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add all old data
            var oldRequestData = new RequestAnalysisData();
            var requestProperty = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            requestProperty?.SetValue(oldRequestData, oldTime);
            _requestAnalytics.TryAdd(typeof(string), oldRequestData);

            var oldCachingData = new CachingAnalysisData();
            var cachingProperty = typeof(CachingAnalysisData).GetProperty("LastAccessTime");
            cachingProperty?.SetValue(oldCachingData, oldTime);
            _cachingAnalytics.TryAdd(typeof(string), oldCachingData);

            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = oldTime,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(oldPrediction);

            // Act
            manager.CleanupOldData();

            // Assert - All old data should be removed
            Assert.True(_requestAnalytics.Count == 0);
            Assert.True(_cachingAnalytics.Count == 0);
            Assert.True(_recentPredictions.Count == 0);
        }
    }
}