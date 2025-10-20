using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using Relay.Core.AI.Optimization.Strategies;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationStrategyFactoryTests
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly AIOptimizationOptions _options = new();

        [Fact]
        public void OptimizationStrategyFactory_ShouldCreateAllStrategies()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategies = factory.CreateAllStrategies().ToList();

            // Assert
            Assert.Equal(5, strategies.Count);
            Assert.Contains(strategies, s => s.Name == "RequestAnalysis");
            Assert.Contains(strategies, s => s.Name == "BatchSizePrediction");
            Assert.Contains(strategies, s => s.Name == "Caching");
            Assert.Contains(strategies, s => s.Name == "Learning");
            Assert.Contains(strategies, s => s.Name == "SystemInsights");
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldCreateStrategyByName()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategy = factory.CreateStrategy("RequestAnalysis");

            // Assert
            Assert.NotNull(strategy);
            Assert.IsType<RequestAnalysisStrategy>(strategy);
            Assert.Equal("RequestAnalysis", strategy.Name);
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldReturnNullForUnknownStrategy()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategy = factory.CreateStrategy("UnknownStrategy");

            // Assert
            Assert.Null(strategy);
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldCreateStrategiesForOperation()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategies = factory.CreateStrategiesForOperation("AnalyzeRequest").ToList();

            // Assert
            Assert.Single(strategies);
            Assert.IsType<RequestAnalysisStrategy>(strategies[0]);
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldReturnEmptyForUnknownOperation()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategies = factory.CreateStrategiesForOperation("UnknownOperation").ToList();

            // Assert
            Assert.Empty(strategies);
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldReturnAllStrategyNames()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var names = factory.GetAvailableStrategyNames().ToList();

            // Assert
            Assert.Equal(5, names.Count);
            Assert.Contains("RequestAnalysis", names);
            Assert.Contains("BatchSizePrediction", names);
            Assert.Contains("Caching", names);
            Assert.Contains("Learning", names);
            Assert.Contains("SystemInsights", names);
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldCreateStrategiesWithCorrectDependencies()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategies = factory.CreateAllStrategies().ToList();

            // Assert
            foreach (var strategy in strategies)
            {
                Assert.NotNull(strategy);
                Assert.NotNull(strategy.Name);
                Assert.NotEmpty(strategy.Name);
                Assert.True(strategy.Priority >= 0);
            }
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldHandleMultipleOperations()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);
            var operations = new[] { "AnalyzeRequest", "PredictBatchSize", "OptimizeCaching", "LearnFromResults", "AnalyzeSystemInsights" };

            // Act & Assert
            foreach (var operation in operations)
            {
                var strategies = factory.CreateStrategiesForOperation(operation).ToList();
                Assert.Single(strategies);
                Assert.True(strategies[0].CanHandle(operation));
            }
        }
    }
}