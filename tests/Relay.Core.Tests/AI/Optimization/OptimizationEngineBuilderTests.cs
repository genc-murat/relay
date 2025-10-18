using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationEngineBuilderTests
    {
        [Fact]
        public void OptimizationEngineBuilder_ShouldBuildWithDefaults()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .Build();

            // Assert
            engine.Should().NotBeNull();
            engine.Should().BeOfType<OptimizationEngine>();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldThrowWithoutLoggerFactory()
        {
            // Arrange
            var options = new AIOptimizationOptions();

            // Act
            Action act = () => new OptimizationEngineBuilder()
                .WithOptions(options)
                .Build();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Logger factory must be set");
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldThrowWithoutOptions()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;

            // Act
            Action act = () => new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .Build();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Options must be set");
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldAddCustomStrategy()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();
            var mockStrategy = new Mock<IOptimizationStrategy>();
            mockStrategy.Setup(s => s.Name).Returns("CustomStrategy");

            // Act
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .AddStrategy(mockStrategy.Object)
                .Build();

            // Assert
            engine.Should().NotBeNull();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldAddMultipleStrategies()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();
            var strategies = new[]
            {
                Mock.Of<IOptimizationStrategy>(s => s.Name == "Strategy1"),
                Mock.Of<IOptimizationStrategy>(s => s.Name == "Strategy2")
            };

            // Act
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .AddStrategies(strategies)
                .Build();

            // Assert
            engine.Should().NotBeNull();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldAddObserver()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();
            var observer = new Mock<IPerformanceObserver>();

            // Act
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .AddObserver(observer.Object)
                .Build();

            // Assert
            engine.Should().NotBeNull();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldConfigureCaching()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act
            var engineWithCaching = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithCaching(true)
                .Build();

            var engineWithoutCaching = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithCaching(false)
                .Build();

            // Assert
            engineWithCaching.Should().NotBeNull();
            engineWithoutCaching.Should().NotBeNull();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldConfigureLearning()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act
            var engineWithLearning = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithLearning(true)
                .Build();

            var engineWithoutLearning = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithLearning(false)
                .Build();

            // Assert
            engineWithLearning.Should().NotBeNull();
            engineWithoutLearning.Should().NotBeNull();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldConfigureSystemInsights()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act
            var engineWithInsights = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithSystemInsights(true)
                .Build();

            var engineWithoutInsights = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithSystemInsights(false)
                .Build();

            // Assert
            engineWithInsights.Should().NotBeNull();
            engineWithoutInsights.Should().NotBeNull();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldSetDefaultTimeout()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();
            var customTimeout = TimeSpan.FromMinutes(5);

            // Act
            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithDefaultTimeout(customTimeout)
                .Build();

            // Assert
            engine.Should().NotBeNull();
        }

        [Fact]
        public async Task OptimizationEngineBuilder_ShouldCreateFunctionalEngine()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            var engine = new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .Build();

            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 10,
                    SuccessfulExecutions = 9,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100)
                }
            };

            // Act
            var result = await engine.OptimizeAsync(context);

            // Assert
            result.Should().NotBeNull();
            result.StrategyName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldBeFluent()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act
            var builder = new OptimizationEngineBuilder();
            var result = builder
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .WithCaching(true)
                .WithLearning(true)
                .WithSystemInsights(true)
                .WithDefaultTimeout(TimeSpan.FromSeconds(30));

            // Assert
            result.Should().Be(builder);
        }

        [Fact]
        public void OptimizationEngineBuilder_ShouldHandleNullCollections()
        {
            // Arrange
            var loggerFactory = NullLoggerFactory.Instance;
            var options = new AIOptimizationOptions();

            // Act & Assert
            Action act = () => new OptimizationEngineBuilder()
                .WithLoggerFactory(loggerFactory)
                .WithOptions(options)
                .AddStrategies(null!)
                .Build();

            act.Should().Throw<ArgumentNullException>();
        }
    }
}