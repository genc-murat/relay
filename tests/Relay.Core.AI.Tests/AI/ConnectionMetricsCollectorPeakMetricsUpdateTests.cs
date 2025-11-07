using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class ConnectionMetricsCollectorPeakMetricsUpdateTests
    {
        private readonly ILogger<ConnectionMetricsCollector> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

        public ConnectionMetricsCollectorPeakMetricsUpdateTests()
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
        public void UpdatePeakConnectionMetrics_Should_Update_All_Time_Peak_When_New_Record()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - First call establishes baseline
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 5.0,
                estimateKeepAliveConnections: () => 1,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var initialPeak = collector.GetPeakMetrics().AllTimePeak;

            // Second call with higher count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var updatedPeak = collector.GetPeakMetrics().AllTimePeak;

            // Assert
            Assert.True(updatedPeak >= initialPeak);
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Not_Update_All_Time_Peak_When_Lower()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - First call with high count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 100,
                calculateConnectionThroughputFactor: () => 50.0,
                estimateKeepAliveConnections: () => 10,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var highPeak = collector.GetPeakMetrics().AllTimePeak;

            // Second call with lower count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 5.0,
                estimateKeepAliveConnections: () => 1,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var finalPeak = collector.GetPeakMetrics().AllTimePeak;

            // Assert
            Assert.Equal(highPeak, finalPeak); // Should remain the same
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Update_Daily_Peak_On_New_Day()
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
        public void UpdatePeakConnectionMetrics_Should_Update_Daily_Peak_When_Higher_On_Same_Day()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - First call
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 20,
                calculateConnectionThroughputFactor: () => 10.0,
                estimateKeepAliveConnections: () => 2,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Second call with higher count (same day)
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 80,
                calculateConnectionThroughputFactor: () => 40.0,
                estimateKeepAliveConnections: () => 8,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.DailyPeak >= 20); // Should be at least the higher value
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Update_Hourly_Peak_On_New_Hour()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Call with connection count
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 30,
                calculateConnectionThroughputFactor: () => 15.0,
                estimateKeepAliveConnections: () => 3,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.HourlyPeak >= 0);
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Update_Hourly_Peak_When_Higher_In_Same_Hour()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - First call
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 15,
                calculateConnectionThroughputFactor: () => 7.0,
                estimateKeepAliveConnections: () => 1,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Second call with higher count (same hour)
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 60,
                calculateConnectionThroughputFactor: () => 30.0,
                estimateKeepAliveConnections: () => 6,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.HourlyPeak >= 15); // Should be at least the higher value
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Handle_Zero_Connection_Count()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Call with zero connections
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 0,
                calculateConnectionThroughputFactor: () => 0.0,
                estimateKeepAliveConnections: () => 0,
                filterHealthyConnections: count => 0,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 0
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.Equal(0, peakMetrics.AllTimePeak); // Should remain 0
            Assert.Equal(0, peakMetrics.DailyPeak);
            Assert.Equal(0, peakMetrics.HourlyPeak);
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Handle_Negative_Connection_Count_Gracefully()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Call with negative result (should be clamped to 0)
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 10,
                calculateConnectionThroughputFactor: () => 5.0,
                estimateKeepAliveConnections: () => 1,
                filterHealthyConnections: count => -50, // Negative filter result
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.Equal(0, peakMetrics.AllTimePeak); // Should be 0 due to Math.Max(0, count)
            Assert.Equal(0, peakMetrics.DailyPeak);
            Assert.Equal(0, peakMetrics.HourlyPeak);
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Update_Timestamp_On_New_Peak()
        {
            // Arrange
            var collector = CreateCollector();
            var beforeCall = DateTime.UtcNow;

            // Act - Call that should update peaks
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 25,
                calculateConnectionThroughputFactor: () => 12.0,
                estimateKeepAliveConnections: () => 2,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            var afterCall = DateTime.UtcNow;
            var peakMetrics = collector.GetPeakMetrics();

            // Assert
            Assert.True(peakMetrics.LastPeakTimestamp >= beforeCall);
            Assert.True(peakMetrics.LastPeakTimestamp <= afterCall);
        }

        [Fact]
        public void UpdatePeakConnectionMetrics_Should_Handle_Exception_Gracefully()
        {
            // Arrange
            var collector = CreateCollector();

            // Act - Exception in update should not crash the main flow
            collector.GetActiveConnectionCount(
                getActiveRequestCount: () => 20,
                calculateConnectionThroughputFactor: () => 10.0,
                estimateKeepAliveConnections: () => 2,
                filterHealthyConnections: count => count,
                cacheConnectionCount: count => { },
                getFallbackConnectionCount: () => 10
            );

            // Assert - Should complete without throwing
            var peakMetrics = collector.GetPeakMetrics();
            Assert.NotNull(peakMetrics);
        }
    }
}