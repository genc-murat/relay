using System;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class PerformanceAnalyzerConstructorTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var options = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = true,
                MinConfidenceScore = 0.7,
                MinExecutionsForAnalysis = 5
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PerformanceAnalyzer(null!, options));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Arrange
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<PerformanceAnalyzer>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PerformanceAnalyzer(logger, null!));
        }

        #endregion
    }
}