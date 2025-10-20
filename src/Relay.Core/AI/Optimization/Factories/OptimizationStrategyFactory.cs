using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Strategies;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Factory for creating optimization strategies.
    /// </summary>
    public class OptimizationStrategyFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AIOptimizationOptions _options;

        public OptimizationStrategyFactory(
            ILoggerFactory loggerFactory,
            AIOptimizationOptions options)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates all available optimization strategies.
        /// </summary>
        public IEnumerable<IOptimizationStrategy> CreateAllStrategies()
        {
            var logger = _loggerFactory.CreateLogger("OptimizationStrategies");

            yield return new RequestAnalysisStrategy(logger);
            yield return new BatchSizePredictionStrategy(logger, _options);
            yield return new CachingStrategy(logger);
            yield return new LearningStrategy(logger);
            yield return new SystemInsightsStrategy(logger);
        }

        /// <summary>
        /// Creates a strategy by name.
        /// </summary>
        public IOptimizationStrategy? CreateStrategy(string strategyName)
        {
            var logger = _loggerFactory.CreateLogger("OptimizationStrategies");

            return strategyName switch
            {
                "RequestAnalysis" => new RequestAnalysisStrategy(logger),
                "BatchSizePrediction" => new BatchSizePredictionStrategy(logger, _options),
                "Caching" => new CachingStrategy(logger),
                "Learning" => new LearningStrategy(logger),
                "SystemInsights" => new SystemInsightsStrategy(logger),
                _ => null
            };
        }

        /// <summary>
        /// Creates strategies that can handle the specified operation.
        /// </summary>
        public IEnumerable<IOptimizationStrategy> CreateStrategiesForOperation(string operation)
        {
            foreach (var strategy in CreateAllStrategies())
            {
                if (strategy.CanHandle(operation))
                {
                    yield return strategy;
                }
            }
        }

        /// <summary>
        /// Gets all available strategy names.
        /// </summary>
        public IEnumerable<string> GetAvailableStrategyNames()
        {
            yield return "RequestAnalysis";
            yield return "BatchSizePrediction";
            yield return "Caching";
            yield return "Learning";
            yield return "SystemInsights";
        }
    }
}