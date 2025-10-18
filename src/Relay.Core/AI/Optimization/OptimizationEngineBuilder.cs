using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Builder for creating optimization engines with complex configurations.
    /// </summary>
    public class OptimizationEngineBuilder
    {
        private ILoggerFactory? _loggerFactory;
        private AIOptimizationOptions? _options;
        private List<IOptimizationStrategy>? _strategies;
        private List<IPerformanceObserver>? _observers;
        private bool _enableCaching = true;
        private bool _enableLearning = true;
        private bool _enableSystemInsights = true;
        private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        public OptimizationEngineBuilder()
        {
        }

        /// <summary>
        /// Sets the logger factory.
        /// </summary>
        public OptimizationEngineBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            return this;
        }

        /// <summary>
        /// Sets the optimization options.
        /// </summary>
        public OptimizationEngineBuilder WithOptions(AIOptimizationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Adds a custom strategy.
        /// </summary>
        public OptimizationEngineBuilder AddStrategy(IOptimizationStrategy strategy)
        {
            _strategies ??= new List<IOptimizationStrategy>();
            _strategies.Add(strategy ?? throw new ArgumentNullException(nameof(strategy)));
            return this;
        }

        /// <summary>
        /// Adds multiple strategies.
        /// </summary>
        public OptimizationEngineBuilder AddStrategies(IEnumerable<IOptimizationStrategy> strategies)
        {
            _strategies ??= new List<IOptimizationStrategy>();
            _strategies.AddRange(strategies ?? throw new ArgumentNullException(nameof(strategies)));
            return this;
        }

        /// <summary>
        /// Adds a performance observer.
        /// </summary>
        public OptimizationEngineBuilder AddObserver(IPerformanceObserver observer)
        {
            _observers ??= new List<IPerformanceObserver>();
            _observers.Add(observer ?? throw new ArgumentNullException(nameof(observer)));
            return this;
        }

        /// <summary>
        /// Adds multiple observers.
        /// </summary>
        public OptimizationEngineBuilder AddObservers(IEnumerable<IPerformanceObserver> observers)
        {
            _observers ??= new List<IPerformanceObserver>();
            _observers.AddRange(observers ?? throw new ArgumentNullException(nameof(observers)));
            return this;
        }

        /// <summary>
        /// Enables or disables caching optimizations.
        /// </summary>
        public OptimizationEngineBuilder WithCaching(bool enabled)
        {
            _enableCaching = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables learning optimizations.
        /// </summary>
        public OptimizationEngineBuilder WithLearning(bool enabled)
        {
            _enableLearning = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables system insights.
        /// </summary>
        public OptimizationEngineBuilder WithSystemInsights(bool enabled)
        {
            _enableSystemInsights = enabled;
            return this;
        }

        /// <summary>
        /// Sets the default timeout for operations.
        /// </summary>
        public OptimizationEngineBuilder WithDefaultTimeout(TimeSpan timeout)
        {
            _defaultTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Builds the optimization engine.
        /// </summary>
        public OptimizationEngine Build()
        {
            if (_loggerFactory == null)
                throw new InvalidOperationException("Logger factory must be set");

            if (_options == null)
                throw new InvalidOperationException("Options must be set");

            // Create strategy factory
            var strategyFactory = new OptimizationStrategyFactory(_loggerFactory, _options);

            // Get default strategies
            var strategies = _strategies ?? new List<IOptimizationStrategy>();
            if (strategies.Count == 0)
            {
                // Add default strategies based on configuration
                var defaultStrategies = new List<IOptimizationStrategy>();

                foreach (var strategy in strategyFactory.CreateAllStrategies())
                {
                    if (ShouldIncludeStrategy(strategy))
                    {
                        defaultStrategies.Add(strategy);
                    }
                }

                strategies = defaultStrategies;
            }

            // Create observers
            var observers = _observers ?? new List<IPerformanceObserver>();
            if (observers.Count == 0)
            {
                // Add default observer
                var logger = _loggerFactory.CreateLogger<PerformanceAlertObserver>();
                observers.Add(new PerformanceAlertObserver(logger));
            }

            return new OptimizationEngine(
                _loggerFactory.CreateLogger<OptimizationEngine>(),
                strategyFactory,
                strategies,
                observers,
                _defaultTimeout);
        }

        private bool ShouldIncludeStrategy(IOptimizationStrategy strategy)
        {
            return strategy.Name switch
            {
                "Caching" => _enableCaching,
                "Learning" => _enableLearning,
                "SystemInsights" => _enableSystemInsights,
                _ => true // Include other strategies by default
            };
        }
    }

    /// <summary>
    /// The built optimization engine.
    /// </summary>
    public class OptimizationEngine : OptimizationEngineTemplate
    {
        private readonly IEnumerable<IOptimizationStrategy> _strategies;
        private readonly TimeSpan _defaultTimeout;

        public OptimizationEngine(
            ILogger logger,
            OptimizationStrategyFactory strategyFactory,
            IEnumerable<IOptimizationStrategy> strategies,
            IEnumerable<IPerformanceObserver> observers,
            TimeSpan defaultTimeout)
            : base(logger, strategyFactory, observers)
        {
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
            _defaultTimeout = defaultTimeout;
        }

        protected override bool ValidateContext(OptimizationContext context)
        {
            return !string.IsNullOrEmpty(context.Operation);
        }

        protected override IEnumerable<IOptimizationStrategy> SelectStrategies(OptimizationContext context)
        {
            // Use strategies that can handle the operation
            return _strategies.Where(s => s.CanHandle(context.Operation));
        }

        protected override async ValueTask<StrategyExecutionResult> ExecuteOptimizationAsync(
            OptimizationContext context,
            IEnumerable<IOptimizationStrategy> strategies,
            CancellationToken cancellationToken)
        {
            var strategyList = strategies.OrderByDescending(s => s.Priority).ToList();

            if (strategyList.Count == 0)
            {
                return new StrategyExecutionResult
                {
                    Success = false,
                    StrategyName = "Engine",
                    ErrorMessage = $"No strategies available for operation: {context.Operation}",
                    ExecutionTime = TimeSpan.Zero
                };
            }

            // Execute the highest priority strategy
            var primaryStrategy = strategyList[0];
            _logger.LogDebug("Executing primary strategy: {Strategy} for operation: {Operation}",
                primaryStrategy.Name, context.Operation);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_defaultTimeout);

            var result = await primaryStrategy.ExecuteAsync(context, cts.Token);

            // If primary strategy fails, try alternatives
            if (!result.Success && strategyList.Count > 1)
            {
                _logger.LogDebug("Primary strategy failed, trying alternatives");

                foreach (var alternative in strategyList.Skip(1))
                {
                    _logger.LogDebug("Trying alternative strategy: {Strategy}", alternative.Name);

                    var altResult = await alternative.ExecuteAsync(context, cts.Token);
                    if (altResult.Success)
                    {
                        return altResult;
                    }
                }
            }

            return result;
        }
    }
}