using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public void AllStrategies_ShouldHaveUniqueNames()
        {
            // Arrange
            var strategies = new List<IOptimizationStrategy>
            {
                new RequestAnalysisStrategy(_logger),
                new BatchSizePredictionStrategy(_logger, new AIOptimizationOptions()),
                new CachingStrategy(_logger),
                new LearningStrategy(_logger),
                new SystemInsightsStrategy(_logger)
            };

            // Act
            var names = strategies.Select(s => s.Name).ToList();

            // Assert
            Assert.Equal(5, names.Distinct().Count());
            Assert.Equal(5, names.Count);
        }

        [Fact]
        public void AllStrategies_ShouldHaveValidPriorities()
        {
            // Arrange
            var strategies = new List<IOptimizationStrategy>
            {
                new RequestAnalysisStrategy(_logger),
                new BatchSizePredictionStrategy(_logger, new AIOptimizationOptions()),
                new CachingStrategy(_logger),
                new LearningStrategy(_logger),
                new SystemInsightsStrategy(_logger)
            };

            // Act & Assert
            foreach (var strategy in strategies)
            {
                Assert.True(strategy.Priority >= 0);
            }
        }
    }
}