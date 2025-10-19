using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorResponseTimeTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorResponseTimeTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

        #region CalculateAverageResponseTime Tests

        [Fact]
        public void CalculateAverageResponseTime_Should_Return_Default_When_No_Analytics()
        {
            // Act
            var responseTime = _calculator.CalculateAverageResponseTime();

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(100), responseTime);
        }

        [Fact]
        public void CalculateAverageResponseTime_Should_Return_Average_Of_All_Requests()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithExecutionTime(100);
            var data2 = CreateAnalysisDataWithExecutionTime(200);
            var data3 = CreateAnalysisDataWithExecutionTime(300);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;
            _requestAnalytics[typeof(Request3)] = data3;

            // Act
            var responseTime = _calculator.CalculateAverageResponseTime();

            // Assert
            // (100 + 200 + 300) / 3 = 200ms
            Assert.Equal(200.0, responseTime.TotalMilliseconds, 1);
        }

        [Fact]
        public void CalculateAverageResponseTime_Should_Handle_Single_Request_Type()
        {
            // Arrange
            var data = CreateAnalysisDataWithExecutionTime(150);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var responseTime = _calculator.CalculateAverageResponseTime();

            // Assert
            Assert.Equal(150.0, responseTime.TotalMilliseconds, 1);
        }

        [Fact]
        public void CalculateAverageResponseTime_Should_Return_Positive_Value()
        {
            // Arrange
            var data = CreateAnalysisDataWithExecutionTime(50);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var responseTime = _calculator.CalculateAverageResponseTime();

            // Assert
            Assert.True(responseTime > TimeSpan.Zero);
        }

        #endregion

        #region Helper Methods

        private RequestAnalysisData CreateAnalysisDataWithExecutionTime(double executionTimeMs)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(executionTimeMs),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5
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