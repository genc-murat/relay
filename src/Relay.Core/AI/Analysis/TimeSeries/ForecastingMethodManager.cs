using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Analysis.TimeSeries.ForecastingStrategies;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Manages forecasting methods and strategies
    /// </summary>
    internal sealed class ForecastingMethodManager : IForecastingMethodManager
    {
        private readonly ILogger<ForecastingMethodManager> _logger;
        private readonly ConcurrentDictionary<string, ForecastingMethod> _metricMethods;
        private readonly ConcurrentDictionary<ForecastingMethod, IForecastingStrategy> _strategies;
        private readonly ForecastingMethod _defaultMethod;

        public ForecastingMethodManager(
            ILogger<ForecastingMethodManager> logger,
            ForecastingConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _defaultMethod = config?.DefaultForecastingMethod ?? ForecastingMethod.SSA;
            _metricMethods = new ConcurrentDictionary<string, ForecastingMethod>();
            _strategies = new ConcurrentDictionary<ForecastingMethod, IForecastingStrategy>();

            // Register default strategies
            RegisterStrategy(new SsaForecastingStrategy());
            RegisterStrategy(new ExponentialSmoothingForecastingStrategy());
            RegisterStrategy(new MovingAverageForecastingStrategy());
            RegisterStrategy(new EnsembleForecastingStrategy());

            _logger.LogInformation("Forecasting method manager initialized with default method: {Method}", _defaultMethod);
        }

        public ForecastingMethod GetForecastingMethod(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            return _metricMethods.GetValueOrDefault(metricName, _defaultMethod);
        }

        public void SetForecastingMethod(string metricName, ForecastingMethod method)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            _metricMethods[metricName] = method;
            _logger.LogInformation("Forecasting method for {MetricName} set to {Method}", metricName, method);
        }

        public IEnumerable<IForecastingStrategy> GetAvailableStrategies()
        {
            return _strategies.Values;
        }

        public IForecastingStrategy? GetStrategy(ForecastingMethod method)
        {
            return _strategies.TryGetValue(method, out var strategy) ? strategy : null;
        }

        private void RegisterStrategy(IForecastingStrategy strategy)
        {
            _strategies[strategy.Method] = strategy;
            _logger.LogDebug("Registered forecasting strategy: {Method}", strategy.Method);
        }
    }
}