using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Pipeline.Behaviors.Strategies;

/// <summary>
/// Factory for creating optimization strategy instances.
/// </summary>
public class OptimizationStrategyFactory<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IAIOptimizationEngine _aiEngine;
    private readonly AIOptimizationOptions _options;
    private readonly IMetricsProvider? _metricsProvider;

    public OptimizationStrategyFactory(
        ILoggerFactory loggerFactory,
        IMemoryCache? memoryCache = null,
        IDistributedCache? distributedCache = null,
        IAIOptimizationEngine? aiEngine = null,
        AIOptimizationOptions? options = null,
        IMetricsProvider? metricsProvider = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _aiEngine = aiEngine ?? throw new ArgumentNullException(nameof(aiEngine));
        _options = options ?? new AIOptimizationOptions();
        _metricsProvider = metricsProvider;
    }

    /// <summary>
    /// Creates an optimization strategy instance for the specified strategy type.
    /// </summary>
    public IOptimizationStrategy<TRequest, TResponse> CreateStrategy(OptimizationStrategy strategyType)
    {
        var logger = _loggerFactory.CreateLogger($"OptimizationStrategy.{strategyType}");

        return strategyType switch
        {
            OptimizationStrategy.EnableCaching or OptimizationStrategy.Caching =>
                new CachingOptimizationStrategy<TRequest, TResponse>(
                    logger, _memoryCache, _distributedCache, _aiEngine, _options, _metricsProvider),

            OptimizationStrategy.BatchProcessing or OptimizationStrategy.Batching =>
                new BatchingOptimizationStrategy<TRequest, TResponse>(
                    logger, _aiEngine, _options, _metricsProvider),

            OptimizationStrategy.MemoryPooling =>
                new MemoryPoolingOptimizationStrategy<TRequest, TResponse>(
                    logger, _metricsProvider),

            OptimizationStrategy.ParallelProcessing or OptimizationStrategy.Parallelization =>
                new ParallelProcessingOptimizationStrategy<TRequest, TResponse>(
                    logger, _metricsProvider),

            OptimizationStrategy.CircuitBreaker =>
                new CircuitBreakerOptimizationStrategy<TRequest, TResponse>(
                    logger, _metricsProvider),

            OptimizationStrategy.DatabaseOptimization =>
                new DatabaseOptimizationStrategy<TRequest, TResponse>(
                    logger, _aiEngine, _options, _metricsProvider),

            OptimizationStrategy.SIMDAcceleration =>
                new SIMDOptimizationStrategy<TRequest, TResponse>(
                    logger, _metricsProvider),

            OptimizationStrategy.Custom =>
                new CustomOptimizationStrategy<TRequest, TResponse>(
                    logger, _aiEngine, _options, _metricsProvider),

            _ => throw new NotSupportedException($"Optimization strategy '{strategyType}' is not supported.")
        };
    }

    /// <summary>
    /// Gets all supported optimization strategies.
    /// </summary>
    public IEnumerable<OptimizationStrategy> GetSupportedStrategies()
    {
        return new[]
        {
            OptimizationStrategy.EnableCaching,
            OptimizationStrategy.BatchProcessing,
            OptimizationStrategy.MemoryPooling,
            OptimizationStrategy.ParallelProcessing,
            OptimizationStrategy.CircuitBreaker,
            OptimizationStrategy.DatabaseOptimization,
            OptimizationStrategy.SIMDAcceleration,
            OptimizationStrategy.Custom
        };
    }
}