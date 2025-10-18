using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Manages forecasting models for different metrics
    /// </summary>
    internal sealed class ForecastingModelManager : IForecastingModelManager
    {
        private readonly ILogger<ForecastingModelManager> _logger;
        private readonly ConcurrentDictionary<string, ITransformer> _models;
        private readonly ConcurrentDictionary<string, ForecastingMethod> _methods;

        public ForecastingModelManager(ILogger<ForecastingModelManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _models = new ConcurrentDictionary<string, ITransformer>();
            _methods = new ConcurrentDictionary<string, ForecastingMethod>();
        }

        public void StoreModel(string metricName, ITransformer model, ForecastingMethod method)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            _models[metricName] = model;
            _methods[metricName] = method;

            _logger.LogDebug("Stored {Method} model for metric {MetricName}", method, metricName);
        }

        public ITransformer? GetModel(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            return _models.TryGetValue(metricName, out var model) ? model : null;
        }

        public bool HasModel(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            return _models.ContainsKey(metricName);
        }

        public IEnumerable<string> GetAvailableMetrics()
        {
            return _models.Keys.ToList();
        }

        public void RemoveModel(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            _models.TryRemove(metricName, out _);
            _methods.TryRemove(metricName, out _);

            _logger.LogDebug("Removed model for metric {MetricName}", metricName);
        }
    }
}