using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorErrorRateTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorErrorRateTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

        #region CalculateCurrentErrorRate Tests

        [Fact]
        public void CalculateCurrentErrorRate_Should_Return_0_When_No_Analytics()
        {
            // Act
            var errorRate = _calculator.CalculateCurrentErrorRate();

            // Assert
            Assert.Equal(0.0, errorRate);
        }

        [Fact]
        public void CalculateCurrentErrorRate_Should_Return_Average_Error_Rate()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithErrorRate(0.05); // 5%
            var data2 = CreateAnalysisDataWithErrorRate(0.10); // 10%
            var data3 = CreateAnalysisDataWithErrorRate(0.15); // 15%

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;
            _requestAnalytics[typeof(Request3)] = data3;

            // Act
            var errorRate = _calculator.CalculateCurrentErrorRate();

            // Assert
            // (0.05 + 0.10 + 0.15) / 3 = 0.1
            Assert.Equal(0.1, errorRate, 3);
        }

        [Fact]
        public void CalculateCurrentErrorRate_Should_Return_0_When_All_Requests_Succeed()
        {
            // Arrange
            var data = CreateAnalysisDataWithErrorRate(0.0);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var errorRate = _calculator.CalculateCurrentErrorRate();

            // Assert
            Assert.Equal(0.0, errorRate);
        }

        [Fact]
        public void CalculateCurrentErrorRate_Should_Handle_100_Percent_Error_Rate()
        {
            // Arrange
            var data = CreateAnalysisDataWithErrorRate(1.0);
            _requestAnalytics[typeof(Request1)] = data;

            // Act
            var errorRate = _calculator.CalculateCurrentErrorRate();

            // Assert
            Assert.Equal(1.0, errorRate);
        }

        [Fact]
        public void CalculateCurrentErrorRate_Should_Be_Between_0_And_1()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithErrorRate(0.25);
            var data2 = CreateAnalysisDataWithErrorRate(0.50);

            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;

            // Act
            var errorRate = _calculator.CalculateCurrentErrorRate();

            // Assert
            Assert.InRange(errorRate, 0.0, 1.0);
        }

        #endregion

        #region Helper Methods

        private RequestAnalysisData CreateAnalysisDataWithErrorRate(double errorRate)
        {
            var data = new RequestAnalysisData();
            var totalExecutions = 100;
            var failedExecutions = (int)(totalExecutions * errorRate);
            var successfulExecutions = totalExecutions - failedExecutions;

            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions
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