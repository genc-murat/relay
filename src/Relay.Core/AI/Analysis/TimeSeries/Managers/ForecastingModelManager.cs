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
                throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (!Enum.IsDefined(typeof(ForecastingMethod), method))
                throw new ArgumentException("Invalid forecasting method", nameof(method));

            _models[metricName] = model;
            _methods[metricName] = method;

            _logger.LogDebug("Stored {Method} model for metric {MetricName}", method, metricName);
        }

        public ITransformer? GetModel(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

            return _models.TryGetValue(metricName, out var model) ? model : null;
        }

        public ForecastingMethod? GetMethod(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

            return _methods.TryGetValue(metricName, out var method) ? method : null;
        }

        public bool HasModel(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

            return _models.ContainsKey(metricName);
        }

        public IEnumerable<string> GetAvailableMetrics()
        {
            return _models.Keys.ToList();
        }

        public int GetModelCount()
        {
            return _models.Count;
        }

        public void StoreModels(IDictionary<string, (ITransformer Model, ForecastingMethod Method)> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            foreach (var kvp in models)
            {
                var (model, method) = kvp.Value;
                StoreModel(kvp.Key, model, method);
            }

            _logger.LogInformation("Stored {Count} models in batch", models.Count);
        }

        public void RemoveModels(IEnumerable<string> metricNames)
        {
            if (metricNames == null)
                throw new ArgumentNullException(nameof(metricNames));

            var removedCount = 0;
            foreach (var metricName in metricNames)
            {
                if (_models.ContainsKey(metricName))
                {
                    RemoveModel(metricName);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("Removed {Count} models in batch", removedCount);
            }
        }

        public void RemoveModel(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

            var removed = _models.TryRemove(metricName, out _);
            _methods.TryRemove(metricName, out _);

            if (removed)
            {
                _logger.LogDebug("Removed model for metric {MetricName}", metricName);
            }
        }

        public void ClearAll()
        {
            var count = _models.Count;
            _models.Clear();
            _methods.Clear();

            if (count > 0)
            {
                _logger.LogInformation("Cleared all {Count} models", count);
            }
        }
    }
}