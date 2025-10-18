using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using Relay.Core.AI.Optimization.Strategies;
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
            strategies.Should().HaveCount(5);
            strategies.Should().Contain(s => s.Name == "RequestAnalysis");
            strategies.Should().Contain(s => s.Name == "BatchSizePrediction");
            strategies.Should().Contain(s => s.Name == "Caching");
            strategies.Should().Contain(s => s.Name == "Learning");
            strategies.Should().Contain(s => s.Name == "SystemInsights");
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldCreateStrategyByName()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategy = factory.CreateStrategy("RequestAnalysis");

            // Assert
            strategy.Should().NotBeNull();
            strategy.Should().BeOfType<RequestAnalysisStrategy>();
            strategy!.Name.Should().Be("RequestAnalysis");
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldReturnNullForUnknownStrategy()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategy = factory.CreateStrategy("UnknownStrategy");

            // Assert
            strategy.Should().BeNull();
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldCreateStrategiesForOperation()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategies = factory.CreateStrategiesForOperation("AnalyzeRequest").ToList();

            // Assert
            strategies.Should().HaveCount(1);
            strategies[0].Should().BeOfType<RequestAnalysisStrategy>();
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldReturnEmptyForUnknownOperation()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var strategies = factory.CreateStrategiesForOperation("UnknownOperation").ToList();

            // Assert
            strategies.Should().BeEmpty();
        }

        [Fact]
        public void OptimizationStrategyFactory_ShouldReturnAllStrategyNames()
        {
            // Arrange
            var factory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Act
            var names = factory.GetAvailableStrategyNames().ToList();

            // Assert
            names.Should().HaveCount(5);
            names.Should().Contain("RequestAnalysis");
            names.Should().Contain("BatchSizePrediction");
            names.Should().Contain("Caching");
            names.Should().Contain("Learning");
            names.Should().Contain("SystemInsights");
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
                strategy.Should().NotBeNull();
                strategy.Name.Should().NotBeNullOrEmpty();
                strategy.Priority.Should().BeGreaterThanOrEqualTo(0);
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
                strategies.Should().HaveCount(1);
                strategies[0].CanHandle(operation).Should().BeTrue();
            }
        }
    }
}