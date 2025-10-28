using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for OptimizationStrategyFactory implementation.
/// </summary>
public class OptimizationStrategyFactoryTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;
    private readonly Mock<IAIOptimizationEngine> _aiEngineMock;
    private readonly AIOptimizationOptions _options;

    public OptimizationStrategyFactoryTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
        _aiEngineMock = new Mock<IAIOptimizationEngine>();

        _options = new AIOptimizationOptions
        {
            Enabled = true,
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.5
        };
    }

    [Fact]
    public void OptimizationStrategyFactory_CreateStrategy_ReturnsCorrectStrategyType()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        var factory = new OptimizationStrategyFactory<TestRequest, TestResponse>(
            loggerFactoryMock.Object,
            null, // memory cache
            null, // distributed cache
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        // Act & Assert
        var cachingStrategy = factory.CreateStrategy(OptimizationStrategy.EnableCaching);
        Assert.IsType<CachingOptimizationStrategy<TestRequest, TestResponse>>(cachingStrategy);

        var batchingStrategy = factory.CreateStrategy(OptimizationStrategy.BatchProcessing);
        Assert.IsType<BatchingOptimizationStrategy<TestRequest, TestResponse>>(batchingStrategy);

        var memoryStrategy = factory.CreateStrategy(OptimizationStrategy.MemoryPooling);
        Assert.IsType<MemoryPoolingOptimizationStrategy<TestRequest, TestResponse>>(memoryStrategy);

        var parallelStrategy = factory.CreateStrategy(OptimizationStrategy.ParallelProcessing);
        Assert.IsType<ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>>(parallelStrategy);

        var circuitBreakerStrategy = factory.CreateStrategy(OptimizationStrategy.CircuitBreaker);
        Assert.IsType<CircuitBreakerOptimizationStrategy<TestRequest, TestResponse>>(circuitBreakerStrategy);

        var databaseStrategy = factory.CreateStrategy(OptimizationStrategy.DatabaseOptimization);
        Assert.IsType<DatabaseOptimizationStrategy<TestRequest, TestResponse>>(databaseStrategy);

        var simdStrategy = factory.CreateStrategy(OptimizationStrategy.SIMDAcceleration);
        Assert.IsType<SIMDOptimizationStrategy<TestRequest, TestResponse>>(simdStrategy);

        var customStrategy = factory.CreateStrategy(OptimizationStrategy.Custom);
        Assert.IsType<CustomOptimizationStrategy<TestRequest, TestResponse>>(customStrategy);
    }

    [Fact]
    public void OptimizationStrategyFactory_GetSupportedStrategies_ReturnsAllStrategies()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var factory = new OptimizationStrategyFactory<TestRequest, TestResponse>(
            loggerFactoryMock.Object,
            null, // memory cache
            null, // distributed cache
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        // Act
        var strategies = factory.GetSupportedStrategies();

        // Assert
        Assert.Contains(OptimizationStrategy.EnableCaching, strategies);
        Assert.Contains(OptimizationStrategy.BatchProcessing, strategies);
        Assert.Contains(OptimizationStrategy.MemoryPooling, strategies);
        Assert.Contains(OptimizationStrategy.ParallelProcessing, strategies);
        Assert.Contains(OptimizationStrategy.CircuitBreaker, strategies);
        Assert.Contains(OptimizationStrategy.DatabaseOptimization, strategies);
        Assert.Contains(OptimizationStrategy.SIMDAcceleration, strategies);
        Assert.Contains(OptimizationStrategy.Custom, strategies);
    }

    [Fact]
    public void OptimizationStrategyFactory_Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OptimizationStrategyFactory<TestRequest, TestResponse>(
                null!, null, null, _aiEngineMock.Object, _options, _metricsProviderMock.Object));
    }

    [Fact]
    public void OptimizationStrategyFactory_Constructor_WithNullAIEngine_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OptimizationStrategyFactory<TestRequest, TestResponse>(
                loggerFactoryMock.Object, null, null, null!, _options, _metricsProviderMock.Object));
    }

    [Fact]
    public void OptimizationStrategyFactory_Constructor_WithNullOptions_WorksCorrectly()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        // Act & Assert - Should not throw
        Assert.Null(Record.Exception(() =>
            new OptimizationStrategyFactory<TestRequest, TestResponse>(
                loggerFactoryMock.Object, null, null, _aiEngineMock.Object, null, _metricsProviderMock.Object)));
    }

    [Fact]
    public void OptimizationStrategyFactory_Constructor_WithNullMetricsProvider_WorksCorrectly()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        // Act & Assert - Should not throw
        Assert.Null(Record.Exception(() =>
            new OptimizationStrategyFactory<TestRequest, TestResponse>(
                loggerFactoryMock.Object, null, null, _aiEngineMock.Object, _options, null)));
    }

    [Fact]
    public void OptimizationStrategyFactory_Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        // Act
        var factory = new OptimizationStrategyFactory<TestRequest, TestResponse>(
            loggerFactoryMock.Object,
            null, // memory cache
            null, // distributed cache
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void OptimizationStrategyFactory_CreateStrategy_WithCachingDependencies_CreatesCachingStrategy()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        var memoryCacheMock = new Mock<IMemoryCache>();
        var distributedCacheMock = new Mock<IDistributedCache>();

        var factory = new OptimizationStrategyFactory<TestRequest, TestResponse>(
            loggerFactoryMock.Object,
            memoryCacheMock.Object,
            distributedCacheMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        // Act
        var strategy = factory.CreateStrategy(OptimizationStrategy.EnableCaching);

        // Assert
        Assert.IsType<CachingOptimizationStrategy<TestRequest, TestResponse>>(strategy);
    }

    [Fact]
    public void OptimizationStrategyFactory_GetSupportedStrategies_ReturnsExpectedCount()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var factory = new OptimizationStrategyFactory<TestRequest, TestResponse>(
            loggerFactoryMock.Object,
            null, // memory cache
            null, // distributed cache
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        // Act
        var strategies = factory.GetSupportedStrategies();

        // Assert - Should return 8 strategies
        Assert.Equal(8, strategies.Count());
    }
}