using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization;

/// <summary>
/// Base decorator for optimization engines.
/// </summary>
public abstract class OptimizationEngineDecorator : OptimizationEngineTemplate
{
    protected readonly OptimizationEngineTemplate _innerEngine;

    protected OptimizationEngineDecorator(
        OptimizationEngineTemplate innerEngine,
        ILogger logger,
        OptimizationStrategyFactory strategyFactory,
        IEnumerable<IPerformanceObserver> observers)
        : base(logger, strategyFactory, observers)
    {
        _innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
    }
}