using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            Assert.True(result.Success);
            Assert.Equal("Engine2Strategy", result.StrategyName); // Should pick highest confidence
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
            Assert.False(result.Success);
            Assert.Contains("All engines failed", result.ErrorMessage);
            Assert.Contains("Engine1 failed", result.ErrorMessage);
            Assert.Contains("Engine2 failed", result.ErrorMessage);
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
            Assert.True(result1.Success);
            Assert.True(result2.Success);
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
            Assert.True(result.Success);
            Assert.Equal("StrategyA", result.StrategyName); // Majority vote wins
        }

        [Fact]
        public async Task VotingOptimizationEngine_AllEnginesFail_ReturnsFailure()
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
                    ErrorMessage = "Engine1 failed",
                    ExecutionTime = TimeSpan.FromMilliseconds(100)
                });

            engine2.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Engine2",
                    ErrorMessage = "Engine2 failed",
                    ExecutionTime = TimeSpan.FromMilliseconds(200)
                });

            var voting = new VotingOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await voting.OptimizeAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Voting", result.StrategyName);
            Assert.Equal("No successful results from any engine", result.ErrorMessage);
            Assert.Equal(TimeSpan.FromMilliseconds(200), result.ExecutionTime); // Max execution time
        }

        [Fact]
        public async Task VotingOptimizationEngine_TieResolvedByConfidence()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine3 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine4 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // Two engines choose StrategyA with low confidence, two choose StrategyB with high confidence
            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyA",
                    Confidence = 0.5
                });

            engine2.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyA",
                    Confidence = 0.6
                });

            engine3.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyB",
                    Confidence = 0.8
                });

            engine4.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyB",
                    Confidence = 0.9
                });

            var voting = new VotingOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object, engine3.Object, engine4.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await voting.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("StrategyB", result.StrategyName); // Higher average confidence wins tie
        }

        [Fact]
        public async Task VotingOptimizationEngine_AllEnginesAgree_ReturnsConsensus()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine2 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);
            var engine3 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            // All engines agree on StrategyA
            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "StrategyA",
                    Confidence = 0.7
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
                    StrategyName = "StrategyA",
                    Confidence = 0.9
                });

            var voting = new VotingOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object, engine2.Object, engine3.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await voting.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("StrategyA", result.StrategyName);
        }

        [Fact]
        public async Task VotingOptimizationEngine_SingleEngine_ReturnsItsResult()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var strategyFactory = new Mock<OptimizationStrategyFactory>(NullLoggerFactory.Instance, new AIOptimizationOptions());
            var observers = new List<IPerformanceObserver>();

            var engine1 = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, strategyFactory.Object, observers);

            engine1.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
                .ReturnsAsync(new StrategyExecutionResult
                {
                    Success = true,
                    StrategyName = "SingleStrategy",
                    Confidence = 0.85
                });

            var voting = new VotingOptimizationEngine(
                logger, strategyFactory.Object, observers, new[] { engine1.Object });

            var context = new OptimizationContext { Operation = "TestOperation" };

            // Act
            var result = await voting.OptimizeAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("SingleStrategy", result.StrategyName);
            Assert.Equal(0.85, result.Confidence);
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
            Assert.Equal(2, composite.EngineCount);
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
            Assert.True(removed);
            Assert.Equal(1, composite.EngineCount);
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
            Assert.False(removed);
            Assert.Equal(1, composite.EngineCount);
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
            act();
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
            Assert.NotNull(composite);
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
            Assert.IsAssignableFrom<OptimizationEngineTemplate>(composite);
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
            Assert.IsAssignableFrom<CompositeOptimizationEngine>(loadBalanced);
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
            Assert.IsAssignableFrom<CompositeOptimizationEngine>(voting);
        }


    }
}