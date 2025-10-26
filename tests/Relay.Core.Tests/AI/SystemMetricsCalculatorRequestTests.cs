using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorRequestTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorRequestTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

        #region GetActiveRequestCount Tests

        [Fact]
        public void GetActiveRequestCount_Should_Return_0_When_No_Analytics()
        {
            // Act
            var count = _calculator.GetActiveRequestCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetActiveRequestCount_Should_Return_Average_Of_Concurrent_Peaks()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithConcurrentPeaks(10);
            var data2 = CreateAnalysisDataWithConcurrentPeaks(20);
            var data3 = CreateAnalysisDataWithConcurrentPeaks(30);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;
            _requestAnalytics[typeof(Request3)] = data3;

            // Act
            var count = _calculator.GetActiveRequestCount();

            // Assert
            // (10 + 20 + 30) / 3 = 20
            Assert.Equal(20, count);
        }

        [Fact]
        public void GetActiveRequestCount_Should_Handle_Single_Request_Type()
        {
            // Arrange
            var data = CreateAnalysisDataWithConcurrentPeaks(15);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var count = _calculator.GetActiveRequestCount();

            // Assert
            Assert.Equal(15, count);
        }

        [Fact]
        public void GetActiveRequestCount_Should_Return_Integer_Average()
        {
            // Arrange - Add data that doesn't divide evenly
            var data1 = CreateAnalysisDataWithConcurrentPeaks(10);
            var data2 = CreateAnalysisDataWithConcurrentPeaks(11);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;

            // Act
            var count = _calculator.GetActiveRequestCount();

            // Assert
            // (10 + 11) / 2 = 10 (integer division)
            Assert.Equal(10, count);
        }

        #endregion

        #region GetQueuedRequestCount Tests

        [Fact]
        public void GetQueuedRequestCount_Should_Return_0_When_Active_Is_Below_Threshold()
        {
            // Arrange
            var processorThreshold = Environment.ProcessorCount * 2;
            var data = CreateAnalysisDataWithConcurrentPeaks(processorThreshold - 1);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var count = _calculator.GetQueuedRequestCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetQueuedRequestCount_Should_Return_Difference_When_Active_Exceeds_Threshold()
        {
            // Arrange
            var processorThreshold = Environment.ProcessorCount * 2;
            var activeCount = processorThreshold + 10;
            var data = CreateAnalysisDataWithConcurrentPeaks(activeCount);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var count = _calculator.GetQueuedRequestCount();

            // Assert
            Assert.Equal(10, count);
        }

        [Fact]
        public void GetQueuedRequestCount_Should_Never_Return_Negative()
        {
            // Arrange - Empty analytics
            // Act
            var count = _calculator.GetQueuedRequestCount();

            // Assert
            Assert.True(count >= 0);
        }

        [Fact]
        public void GetQueuedRequestCount_Should_Be_Based_On_Active_Request_Count()
        {
            // Arrange
            var processorThreshold = Environment.ProcessorCount * 2;
            var data1 = CreateAnalysisDataWithConcurrentPeaks(processorThreshold + 5);
            var data2 = CreateAnalysisDataWithConcurrentPeaks(processorThreshold + 15);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;

            // Act
            var activeCount = _calculator.GetActiveRequestCount();
            var queuedCount = _calculator.GetQueuedRequestCount();

            // Assert
            var expectedQueued = Math.Max(0, activeCount - processorThreshold);
            Assert.Equal(expectedQueued, queuedCount);
        }

        #endregion

        #region Helper Methods

        private RequestAnalysisData CreateAnalysisDataWithConcurrentPeaks(int concurrentPeaks)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                ConcurrentExecutions = concurrentPeaks
            };
            data.AddMetrics(metrics);
            return data;
        }

        #endregion

        #region Test Types

        private class Request1 { }
        private class Request2 { }
        private class Request3 { }

        #endregion
    }
}