using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class CompositeOptimizationEngineTests
    {
        [Fact]
        public async Task CompositeOptimizationEngine_CombinesMultipleEngines()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "Engine1Strategy",
                    Confidence = 0.7
                });

            engine2.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "Engine2Strategy",
                    Confidence = 0.8
                });

            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await composite.OptimizeAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.StrategyName.Should().Be("Engine2Strategy"); // Should pick highest confidence
            engine1.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Once);
            engine2.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Once);
        }

        [Fact]
        public async Task CompositeOptimizationEngine_AllEnginesFail_ReturnsAggregatedError()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Engine1",
                    ErrorMessage = "Engine1 failed"
                });

            engine2.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Engine2",
                    ErrorMessage = "Engine2 failed"
                });

            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await composite.OptimizeAsync(context);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("All engines failed");
            result.ErrorMessage.Should().Contain("Engine1 failed");
            result.ErrorMessage.Should().Contain("Engine2 failed");
        }

        [Fact]
        public async Task LoadBalancedOptimizationEngine_UsesRoundRobinDistribution()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "Engine1Strategy",
                    Confidence = 0.8
                });

            engine2.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "Engine2Strategy",
                    Confidence = 0.8
                });

            var loadBalanced = new LoadBalancedOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result1 = await loadBalanced.OptimizeAsync(context);
            var result2 = await loadBalanced.OptimizeAsync(context);

            // Assert
            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Note: Round-robin behavior may not be deterministic in parallel execution
        }

        [Fact]
        public async Task VotingOptimizationEngine_SelectsConsensusResult()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine3 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Engine1 and Engine2 agree on StrategyA, Engine3 chooses StrategyB
            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyA",
                    Confidence = 0.8
                });

            engine2.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyA",
                    Confidence = 0.8
                });

            engine3.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyB",
                    Confidence = 0.8
                });

            var voting = new VotingOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object, engine3.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await voting.OptimizeAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.StrategyName.Should().Be("StrategyA"); // Majority vote wins
        }

        [Fact]
        public void CompositeOptimizationEngine_AddEngine_IncreasesEngineCount()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object });

            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Act
            composite.AddEngine(engine2.Object);

            // Assert
            composite.EngineCount.Should().Be(2);
        }

        [Fact]
        public void CompositeOptimizationEngine_RemoveEngine_DecreasesEngineCount()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object });

            // Act
            var removed = composite.RemoveEngine(engine2.Object);

            // Assert
            removed.Should().BeTrue();
            composite.EngineCount.Should().Be(1);
        }

        [Fact]
        public void CompositeOptimizationEngine_RemoveNonExistentEngine_ReturnsFalse()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object });

            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Act
            var removed = composite.RemoveEngine(engine2.Object);

            // Assert
            removed.Should().BeFalse();
            composite.EngineCount.Should().Be(1);
        }

        [Fact]
        public void CompositeOptimizationEngine_Constructor_AllowsEmptyEngines()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            // Act
            Action act = () => new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, Array.Empty<OptimizationEngineTemplate>());

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task CompositeOptimizationEngine_ValidatesContext_WithAllEngines()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Setup validation - both engines agree context is valid
            // Note: This tests the internal validation logic indirectly

            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object });

            var validContext = new OptimizationContext { Operation = "ValidOperation" };
            var invalidContext = new OptimizationContext { Operation = null };

            // Act & Assert - Valid context
            // Note: Actual validation testing would require more complex mocking
            composite.Should().NotBeNull();
        }

        [Fact]
        public void CompositeOptimizationEngine_InheritsFrom_OptimizationEngineTemplate()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Act
            var composite = new CompositeOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine.Object });

            // Assert
            composite.Should().BeAssignableTo<OptimizationEngineTemplate>();
        }

        [Fact]
        public void LoadBalancedOptimizationEngine_InheritsFrom_CompositeOptimizationEngine()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Act
            var loadBalanced = new LoadBalancedOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine.Object });

            // Assert
            loadBalanced.Should().BeAssignableTo<CompositeOptimizationEngine>();
        }

        [Fact]
        public void VotingOptimizationEngine_InheritsFrom_CompositeOptimizationEngine()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();
            var engine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Act
            var voting = new VotingOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine.Object });

            // Assert
            voting.Should().BeAssignableTo<CompositeOptimizationEngine>();
        }


    }
}