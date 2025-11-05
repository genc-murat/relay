using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorThroughputTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorThroughputTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

        #region CalculateCurrentThroughput Tests

        [Fact]
        public void CalculateCurrentThroughput_Should_Return_0_When_No_Analytics()
        {
            // Act
            var throughput = _calculator.CalculateCurrentThroughput();

            // Assert
            Assert.Equal(0.0, throughput);
        }

        [Fact]
        public void CalculateCurrentThroughput_Should_Calculate_Executions_Per_Second()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithExecutions(1000);
            var data2 = CreateAnalysisDataWithExecutions(2000);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;

            // Act
            var throughput = _calculator.CalculateCurrentThroughput();

            // Assert
            // (1000 + 2000) / 300 seconds (5 minutes) = 10 requests/second
            Assert.Equal(10.0, throughput, 2);
        }

        [Fact]
        public void CalculateCurrentThroughput_Should_Be_Non_Negative()
        {
            // Arrange
            var data = CreateAnalysisDataWithExecutions(100);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var throughput = _calculator.CalculateCurrentThroughput();

            // Assert
            Assert.True(throughput >= 0.0);
        }

        [Fact]
        public void CalculateCurrentThroughput_Should_Sum_All_Request_Types()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithExecutions(300);
            var data2 = CreateAnalysisDataWithExecutions(600);
            var data3 = CreateAnalysisDataWithExecutions(900);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;
            _requestAnalytics[typeof(Request3)] = data3;

            // Act
            var throughput = _calculator.CalculateCurrentThroughput();

            // Assert
            // (300 + 600 + 900) / 300 = 6.0
            Assert.Equal(6.0, throughput, 2);
        }

        #endregion

        #region Helper Methods

        private RequestAnalysisData CreateAnalysisDataWithExecutions(int totalExecutions)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = totalExecutions - 1,
                FailedExecutions = 1
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