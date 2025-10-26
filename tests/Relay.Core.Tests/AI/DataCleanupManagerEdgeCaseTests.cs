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
    public class DataCleanupManagerEdgeCaseTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerEdgeCaseTests()
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

        #region Edge Case Tests

        [Fact]
        public void CleanupOldData_Should_Handle_Null_Property_Values()
        {
            // Arrange
            var manager = CreateManager();

            // Add data with default values (which shouldn't be null)
            _requestAnalytics.TryAdd(typeof(string), new RequestAnalysisData());
            _cachingAnalytics.TryAdd(typeof(int), new CachingAnalysisData());

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Boundary_Time()
        {
            // Arrange
            var manager = CreateManager();
            var boundaryTime = DateTime.UtcNow.AddHours(-24); // Exactly at cutoff

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, boundaryTime);
            _requestAnalytics.TryAdd(typeof(string), data);

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Future_Timestamps()
        {
            // Arrange
            var manager = CreateManager();
            var futureTime = DateTime.UtcNow.AddHours(24); // Future time

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, futureTime);
            _requestAnalytics.TryAdd(typeof(string), data);

            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert - Future data should be kept
            Assert.Equal(initialCount, _requestAnalytics.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_MinValue_Timestamps()
        {
            // Arrange
            var manager = CreateManager();

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, DateTime.MinValue);
            _requestAnalytics.TryAdd(typeof(string), data);

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
            Assert.Empty(_requestAnalytics); // MinValue should be cleaned up
        }

        [Fact]
        public void CleanupOldData_Should_Handle_MaxValue_Timestamps()
        {
            // Arrange
            var manager = CreateManager();

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, DateTime.MaxValue);
            _requestAnalytics.TryAdd(typeof(string), data);

            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert - MaxValue should be kept
            Assert.Equal(initialCount, _requestAnalytics.Count);
        }

        #endregion
    }
}