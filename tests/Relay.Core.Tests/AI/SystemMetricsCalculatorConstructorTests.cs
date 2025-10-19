using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class SystemMetricsCalculatorConstructorTests
    {
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
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<SystemMetricsCalculator>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SystemMetricsCalculator(logger, null!));
        }

        #endregion
    }
}