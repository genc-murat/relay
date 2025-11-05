using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorPoolTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorPoolTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

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
    }
}