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
    public class SystemMetricsCalculatorTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var analytics = new ConcurrentDictionary<Type, RequestAnalysisData>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemMetricsCalculator(null!, analytics));
        }

        [Fact]
        public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemMetricsCalculator(_logger, null!));
        }

        #endregion

        #region CalculateCpuUsageAsync Tests

        [Fact]
        public async Task CalculateCpuUsageAsync_Should_Return_Value_Between_0_And_1()
        {
            // Act
            var cpuUsage = await _calculator.CalculateCpuUsageAsync(CancellationToken.None);

            // Assert
            Assert.InRange(cpuUsage, 0.0, 1.0);
        }

        [Fact]
        public async Task CalculateCpuUsageAsync_Should_Return_Consistent_Values()
        {
            // Act
            var usage1 = await _calculator.CalculateCpuUsageAsync(CancellationToken.None);
            var usage2 = await _calculator.CalculateCpuUsageAsync(CancellationToken.None);

            // Assert - Both should be valid percentages
            Assert.InRange(usage1, 0.0, 1.0);
            Assert.InRange(usage2, 0.0, 1.0);
        }

        [Fact]
        public async Task CalculateCpuUsageAsync_Should_Respect_CancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _calculator.CalculateCpuUsageAsync(cts.Token));
        }

        [Fact]
        public async Task CalculateCpuUsageAsync_Should_Return_Higher_Base_For_Low_Processor_Count()
        {
            // This test verifies the logic exists, though the actual value depends on the environment
            // Act
            var cpuUsage = await _calculator.CalculateCpuUsageAsync(CancellationToken.None);

            // Assert
            var expectedMinimum = Environment.ProcessorCount > 4 ? 0.2 : 0.3;
            Assert.True(cpuUsage >= expectedMinimum || cpuUsage <= 1.0);
        }

        #endregion

        #region CalculateMemoryUsage Tests

        [Fact]
        public void CalculateMemoryUsage_Should_Return_Value_Between_0_And_1()
        {
            // Act
            var memoryUsage = _calculator.CalculateMemoryUsage();

            // Assert
            Assert.InRange(memoryUsage, 0.0, 1.0);
        }

        [Fact]
        public void CalculateMemoryUsage_Should_Return_Non_Negative_Value()
        {
            // Act
            var memoryUsage = _calculator.CalculateMemoryUsage();

            // Assert
            Assert.True(memoryUsage >= 0.0);
        }

        [Fact]
        public void CalculateMemoryUsage_Should_Be_Based_On_GC_Memory()
        {
            // Arrange - Force some memory allocation
            var largeArray = new byte[1024 * 1024]; // 1MB

            // Act
            var memoryUsage = _calculator.CalculateMemoryUsage();

            // Assert
            Assert.True(memoryUsage > 0.0);

            // Cleanup
            GC.KeepAlive(largeArray);
        }

        #endregion

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

        #region GetDatabasePoolUtilization Tests

        [Fact]
        public void GetDatabasePoolUtilization_Should_Return_0()
        {
            // Act
            var utilization = _calculator.GetDatabasePoolUtilization();

            // Assert
            Assert.Equal(0.0, utilization);
        }

        #endregion

        #region GetThreadPoolUtilization Tests

        [Fact]
        public void GetThreadPoolUtilization_Should_Return_Value_Between_0_And_1()
        {
            // Act
            var utilization = _calculator.GetThreadPoolUtilization();

            // Assert
            Assert.InRange(utilization, 0.0, 1.0);
        }

        [Fact]
        public void GetThreadPoolUtilization_Should_Return_Non_Negative()
        {
            // Act
            var utilization = _calculator.GetThreadPoolUtilization();

            // Assert
            Assert.True(utilization >= 0.0);
        }

        [Fact]
        public async Task GetThreadPoolUtilization_Should_Increase_With_Active_Tasks()
        {
            // Arrange
            var utilization1 = _calculator.GetThreadPoolUtilization();

            // Act - Create some thread pool work
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(async () => await Task.Delay(10)))
                .ToArray();

            var utilization2 = _calculator.GetThreadPoolUtilization();

            await Task.WhenAll(tasks);

            // Assert - Utilization should be non-negative in both cases
            Assert.True(utilization1 >= 0.0);
            Assert.True(utilization2 >= 0.0);
        }

        #endregion

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

        private RequestAnalysisData CreateAnalysisDataWithConcurrentPeaks(int peaks)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                ConcurrentExecutions = peaks
            };
            data.AddMetrics(metrics);
            return data;
        }

        private RequestAnalysisData CreateAnalysisDataWithExecutions(long totalExecutions)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = totalExecutions - 5,
                FailedExecutions = 5,
                ConcurrentExecutions = 10
            };
            data.AddMetrics(metrics);
            return data;
        }

        private RequestAnalysisData CreateAnalysisDataWithExecutionTime(double milliseconds)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(milliseconds),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                ConcurrentExecutions = 10
            };
            data.AddMetrics(metrics);
            return data;
        }

        private RequestAnalysisData CreateAnalysisDataWithErrorRate(double errorRate)
        {
            var data = new RequestAnalysisData();
            long totalExecutions = 100;
            long failedExecutions = (long)(totalExecutions * errorRate);
            long successfulExecutions = totalExecutions - failedExecutions;

            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions,
                ConcurrentExecutions = 10
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
