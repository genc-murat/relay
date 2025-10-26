using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class DataCleanupManagerPerformanceTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerPerformanceTests()
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

        #region Performance Tests

        [Fact]
        public void CleanupOldData_Should_Complete_Quickly_With_Many_Items()
        {
            // Arrange
            var manager = CreateManager();

            // Add many items
            for (int i = 0; i < 100; i++)
            {
                _requestAnalytics.TryAdd(Type.GetType($"System.Type{i}") ?? typeof(object), new RequestAnalysisData());
                _cachingAnalytics.TryAdd(Type.GetType($"System.Cache{i}") ?? typeof(object), new CachingAnalysisData());

                var prediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(prediction);
            }

            var startTime = DateTime.UtcNow;

            // Act
            manager.CleanupOldData();

            var duration = DateTime.UtcNow - startTime;

            // Assert - Should complete in reasonable time (< 1 second)
            Assert.True(duration.TotalSeconds < 1);
        }

        #endregion
    }
}