using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class DataCleanupManagerConstructorTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerConstructorTests()
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
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var manager = CreateManager();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(null!, _requestAnalytics, _cachingAnalytics, _recentPredictions));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(_logger, null!, _cachingAnalytics, _recentPredictions));

            Assert.Equal("requestAnalytics", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_CachingAnalytics_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(_logger, _requestAnalytics, null!, _recentPredictions));

            Assert.Equal("cachingAnalytics", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RecentPredictions_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(_logger, _requestAnalytics, _cachingAnalytics, null!));

            Assert.Equal("recentPredictions", exception.ParamName);
        }
    }
}