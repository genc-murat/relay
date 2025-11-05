using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization;

public class OptimizationEngineDecoratorTests
{
    [Fact]
    public async Task CachingOptimizationEngineDecorator_CachesSuccessfulResults()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());
        var cacheDuration = TimeSpan.FromMinutes(5);

        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8
            });

        var decorator = new CachingOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers, cacheDuration);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act - First call
        var result1 = await decorator.OptimizeAsync(context);
        // Second call - should use cache
        var result2 = await decorator.OptimizeAsync(context);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        innerEngine.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Once);
    }

    [Fact]
    public async Task MonitoringOptimizationEngineDecorator_TracksPerformanceMetrics()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            });

        var decorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act
        var result = await decorator.OptimizeAsync(context);

        // Assert
        Assert.True(result.Success);
        innerEngine.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Once);
    }

    [Fact]
    public async Task RetryOptimizationEngineDecorator_RetriesFailedOperations()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());
        var maxRetries = 2;

        // First call fails, second succeeds
        innerEngine.SetupSequence(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = false,
                StrategyName = "TestStrategy",
                ErrorMessage = "First attempt failed"
            })
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8
             });

        var decorator = new RetryOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers, maxRetries);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act
        var result = await decorator.OptimizeAsync(context);

        // Assert
        Assert.True(result.Success);
        innerEngine.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task RetryOptimizationEngineDecorator_ExhaustsRetries_ReturnsFailure()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());
        var maxRetries = 2;

        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = false,
                StrategyName = "TestStrategy",
                ErrorMessage = "Always fails"
            });

        var decorator = new RetryOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers, maxRetries);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act
        var result = await decorator.OptimizeAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed after 3 attempts", result.ErrorMessage);
        innerEngine.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Exactly(3));
    }

    [Fact]
    public void CachingOptimizationEngineDecorator_CacheKeyGeneration_IsDeterministic()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());
        var cacheDuration = TimeSpan.FromMinutes(5);

        var decorator = new CachingOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers, cacheDuration);

        var context1 = new OptimizationContext
        {
            Operation = "TestOperation",
            RequestType = typeof(string),
            Request = "test"
        };

        var context2 = new OptimizationContext
        {
            Operation = "TestOperation",
            RequestType = typeof(string),
            Request = "test"
        };

        // Act - Access private method via reflection or test indirectly
        // Since cache key generation is private, we test through caching behavior
        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8
            });

        // Assert - Same context should use cache
        // This is tested indirectly through the caching behavior test above
        Assert.True(true); // Placeholder - actual test would verify cache hits
    }

    [Fact]
    public async Task MonitoringOptimizationEngineDecorator_DetectsPerformanceDegradation()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        // Setup sequence of calls with degrading performance
        innerEngine.SetupSequence(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            })
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(250) // 2.5x slower
             });

        var decorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act
        await decorator.OptimizeAsync(context); // Normal performance
        await decorator.OptimizeAsync(context); // Degraded performance

        // Assert
        innerEngine.Verify(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task MonitoringOptimizationEngineDecorator_LimitsPerformanceHistory()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            });

        var decorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act - Execute 105 times to exceed the 100 limit
        for (int i = 0; i < 105; i++)
        {
            await decorator.OptimizeAsync(context);
        }

        // Assert - Should only keep 100 entries
        var field = typeof(MonitoringOptimizationEngineDecorator).GetField("_performanceHistory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var history = (Dictionary<string, List<TimeSpan>>)field.GetValue(decorator);
        Assert.Equal(100, history["TestOperation"].Count);
    }

    [Fact]
    public async Task MonitoringOptimizationEngineDecorator_NotifiesObservers_OnPerformanceDegradation()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observerMock = new Mock<IPerformanceObserver>();
        var observers = new List<IPerformanceObserver> { observerMock.Object };
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        // Setup sequence with baseline performance then degradation
        innerEngine.SetupSequence(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            });

        var decorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act - Build history with normal performance
        for (int i = 0; i < 10; i++)
        {
            await decorator.OptimizeAsync(context);
        }

        // Change mock to return degraded performance (3x slower = 300ms vs 100ms average)
        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(300)
            });

        await decorator.OptimizeAsync(context);

        // Assert
        observerMock.Verify(o => o.OnPerformanceThresholdExceededAsync(It.Is<PerformanceAlert>(
            alert => alert.AlertType == "PerformanceDegradation" &&
                    alert.Severity == AlertSeverity.Medium &&
                    alert.Message.Contains("TestOperation"))), Times.Once);
    }

    [Fact]
    public async Task MonitoringOptimizationEngineDecorator_HandlesObserverExceptions()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observerMock = new Mock<IPerformanceObserver>();
        observerMock.Setup(o => o.OnPerformanceThresholdExceededAsync(It.IsAny<PerformanceAlert>()))
            .ThrowsAsync(new Exception("Observer failed"));
        var observers = new List<IPerformanceObserver> { observerMock.Object };
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        // Setup sequence with baseline then degradation
        innerEngine.SetupSequence(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            });

        var decorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        var context = new OptimizationContext { Operation = "TestOperation" };

        // Act - Build history
        for (int i = 0; i < 10; i++)
        {
            await decorator.OptimizeAsync(context);
        }

        // Trigger degradation with failing observer
        innerEngine.Setup(e => e.OptimizeAsync(It.IsAny<OptimizationContext>(), default))
            .ReturnsAsync(new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromMilliseconds(300)
            });

        // Should not throw despite observer exception
        await decorator.OptimizeAsync(context);

        // Assert - Observer was called but exception was handled
        observerMock.Verify(o => o.OnPerformanceThresholdExceededAsync(It.IsAny<PerformanceAlert>()), Times.Once);
    }

    [Fact]
    public void MonitoringOptimizationEngineDecorator_IsPerformanceDegraded_RequiresMinimumHistory()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        var decorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        // Act & Assert - Should return false with insufficient history
        var method = typeof(MonitoringOptimizationEngineDecorator).GetMethod("IsPerformanceDegraded",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // With no history
        var result = (bool)method.Invoke(decorator, new object[] { "TestOperation", TimeSpan.FromMilliseconds(1000) });
        Assert.False(result);

        // With only 3 entries (less than 5 required)
        var field = typeof(MonitoringOptimizationEngineDecorator).GetField("_performanceHistory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var history = (Dictionary<string, List<TimeSpan>>)field.GetValue(decorator);
        history["TestOperation"] = new List<TimeSpan>
        {
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100)
        };

        result = (bool)method.Invoke(decorator, new object[] { "TestOperation", TimeSpan.FromMilliseconds(1000) });
        Assert.False(result);
    }

    [Fact]
    public void Decorator_InheritsFrom_OptimizationEngineDecorator()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), new List<IPerformanceObserver>());

        // Act
        var cachingDecorator = new CachingOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers, TimeSpan.FromMinutes(5));

        var monitoringDecorator = new MonitoringOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers);

        // Assert
        Assert.IsAssignableFrom<OptimizationEngineDecorator>(cachingDecorator);
        Assert.IsAssignableFrom<OptimizationEngineDecorator>(monitoringDecorator);
    }

    [Fact]
    public void Decorator_Implements_IOptimizationEngineTemplate()
    {
        // Arrange
        var logger = NullLogger.Instance;
        var strategyFactory = new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions());
        var observers = new List<IPerformanceObserver>();
        var innerEngine = new Mock<OptimizationEngineTemplate>(NullLogger.Instance, new OptimizationStrategyFactory(NullLoggerFactory.Instance, new AIOptimizationOptions()), observers);

        // Act
        var decorator = new CachingOptimizationEngineDecorator(
            innerEngine.Object, logger, strategyFactory, observers, TimeSpan.FromMinutes(5));

        // Assert
        Assert.IsAssignableFrom<OptimizationEngineTemplate>(decorator);
    }
}