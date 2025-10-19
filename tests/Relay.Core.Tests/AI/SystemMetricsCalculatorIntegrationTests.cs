using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorIntegrationTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

        #region Integration Tests

        [Fact]
        public async Task SystemMetricsCalculator_Should_Handle_Concurrent_Access()
        {
            // Arrange
            var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
            {
                var data = CreateAnalysisDataWithExecutions(100 * i);
                _requestAnalytics[Type.GetType($"Request{i}") ?? typeof(object)] = data;
            })).ToArray();

            // Act
            await Task.WhenAll(tasks);

            var throughput = _calculator.CalculateCurrentThroughput();
            var responseTime = _calculator.CalculateAverageResponseTime();
            var errorRate = _calculator.CalculateCurrentErrorRate();
            var activeCount = _calculator.GetActiveRequestCount();

            // Assert - All metrics should be valid
            Assert.True(throughput >= 0.0);
            Assert.True(responseTime >= TimeSpan.Zero);
            Assert.InRange(errorRate, 0.0, 1.0);
            Assert.True(activeCount >= 0);
        }

        [Fact]
        public async Task SystemMetricsCalculator_Should_Provide_Consistent_Metrics()
        {
            // Arrange
            var data1 = CreateAnalysisDataWithExecutions(1000);
            var data2 = CreateAnalysisDataWithExecutions(2000);
            _requestAnalytics[typeof(Request1)] = data1;
            _requestAnalytics[typeof(Request2)] = data2;

            // Act
            var cpuUsage = await _calculator.CalculateCpuUsageAsync(CancellationToken.None);
            var memoryUsage = _calculator.CalculateMemoryUsage();
            var throughput = _calculator.CalculateCurrentThroughput();
            var responseTime = _calculator.CalculateAverageResponseTime();
            var errorRate = _calculator.CalculateCurrentErrorRate();
            var threadPoolUtil = _calculator.GetThreadPoolUtilization();

            // Assert - All metrics should be in valid ranges
            Assert.InRange(cpuUsage, 0.0, 1.0);
            Assert.InRange(memoryUsage, 0.0, 1.0);
            Assert.True(throughput >= 0.0);
            Assert.True(responseTime >= TimeSpan.Zero);
            Assert.InRange(errorRate, 0.0, 1.0);
            Assert.InRange(threadPoolUtil, 0.0, 1.0);
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

        #endregion
    }
}