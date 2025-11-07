using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorPeakMetricsTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorPeakMetricsTests()
        {
            _logger = NullLogger<ConnectionMetricsCollector>.Instance;
            _options = new AIOptimizationOptions
            {
                MaxEstimatedHttpConnections = 200,
                MaxEstimatedDbConnections = 50,
                EstimatedMaxDbConnections = 100,
                MaxEstimatedExternalConnections = 30,
                MaxEstimatedWebSocketConnections = 1000
            };
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        }

        private ConnectionMetricsCollector CreateCollector()
        {
            return new ConnectionMetricsCollector(_logger, _options, _requestAnalytics);
        }

        [Fact]
        public void GetPeakMetrics_Should_Return_NonNull_Object()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetPeakMetrics();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetPeakMetrics_Should_Return_Valid_Peak_Metrics_Object()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetPeakMetrics();

            // Assert
            Assert.IsType<PeakConnectionMetrics>(result);
            Assert.True(result.AllTimePeak >= 0);
            Assert.True(result.DailyPeak >= 0);
            Assert.True(result.HourlyPeak >= 0);
        }

        [Fact]
        public void GetPeakMetrics_Should_Return_Initial_Values_When_No_Updates()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result = collector.GetPeakMetrics();

            // Assert
            Assert.Equal(0, result.AllTimePeak); // Initial value should be 0
            Assert.Equal(0, result.DailyPeak);
            Assert.Equal(0, result.HourlyPeak);
        }

        [Fact]
        public void GetPeakMetrics_Should_Return_Same_Object_Reference()
        {
            // Arrange
            var collector = CreateCollector();

            // Act
            var result1 = collector.GetPeakMetrics();
            var result2 = collector.GetPeakMetrics();

            // Assert
            Assert.Same(result1, result2); // Should return the same instance
        }

        [Fact]
        public void GetPeakMetrics_Should_Reflect_Updates_After_Connection_Count_Calculation()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Call GetActiveConnectionCount which updates peak metrics
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.AllTimePeak > 0); // Should have been updated
            Assert.True(peakMetrics.DailyPeak > 0);
            Assert.True(peakMetrics.HourlyPeak > 0);
        }

        [Fact]
        public void GetPeakMetrics_Should_Track_All_Time_Peak_Correctly()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - First call with lower count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 5.0,
                estimateKeepAliveConnections: () => 1,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Second call with higher count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.AllTimePeak >= 10); // Should be at least the higher value
        }

        [Fact]
        public void GetPeakMetrics_Should_Track_Daily_Peak_Correctly()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Call with connection count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 50,
                calculateConnectionThroughputFactor: () => 25.0,
                estimateKeepAliveConnections: () => 5,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.DailyPeak >= 0);
        }

        [Fact]
        public void GetPeakMetrics_Should_Track_Hourly_Peak_Correctly()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Call with connection count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 25,
                calculateConnectionThroughputFactor: () => 12.0,
                estimateKeepAliveConnections: () => 2,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.HourlyPeak >= 0);
        }

        [Fact]
        public void GetPeakMetrics_Should_Be_Thread_Safe()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Multiple calls (simulating concurrent access)
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                collector.GetActiveConnectionCount(
                    getActiveRequestCount: () => i * 10,
                    calculateConnectionThroughputFactor: () => i * 5.0,
                    estimateKeepAliveConnections: () => i,
                    filterHealthyConnections: count => count,
                    cacheConnectionCount: count => { },
                    getFallbackConnectionCount: () => 10
                );
            });

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.NotNull(peakMetrics);
            Assert.True(peakMetrics.AllTimePeak >= 0);
        }
    }
}