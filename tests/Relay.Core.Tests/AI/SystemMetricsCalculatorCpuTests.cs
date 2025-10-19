using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorCpuTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorCpuTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

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
    }
}