using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorMemoryTests
    {
        private readonly ILogger<SystemMetricsCalculator> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly SystemMetricsCalculator _calculator;

        public SystemMetricsCalculatorMemoryTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _calculator = new SystemMetricsCalculator(_logger, _requestAnalytics);
        }

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
    }
}