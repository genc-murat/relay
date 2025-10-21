using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Handles training of forecasting models
    /// </summary>
    internal sealed class ForecastingTrainer : IForecastingTrainer
    {
        private readonly ILogger<ForecastingTrainer> _logger;
        private readonly ITimeSeriesRepository _repository;
        private readonly IForecastingModelManager _modelManager;
        private readonly IForecastingMethodManager _methodManager;
        private readonly ForecastingConfiguration _config;
        private readonly MLContext _mlContext;

        public ForecastingTrainer(
            ILogger<ForecastingTrainer> logger,
            ITimeSeriesRepository repository,
            IForecastingModelManager modelManager,
            IForecastingMethodManager methodManager,
            ForecastingConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _methodManager = methodManager ?? throw new ArgumentNullException(nameof(methodManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mlContext = new MLContext(seed: _config.MlContextSeed);
        }

        public void TrainModel(string metricName, ForecastingMethod? method = null)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            var forecastingMethod = method ?? _methodManager.GetForecastingMethod(metricName);

            try
            {
                if (!HasSufficientData(metricName, out var dataCount))
                {
                    throw new InsufficientDataException(
                        $"Insufficient data for {forecastingMethod} forecasting in {metricName}: {dataCount} data points (minimum {_config.MinimumDataPoints} required)")
                    {
                        MetricName = metricName,
                        Operation = $"Train{forecastingMethod}Model",
                        MinimumRequired = _config.MinimumDataPoints,
                        ActualCount = dataCount
                    };
                }

                var history = _repository.GetHistory(metricName, TimeSpan.FromDays(_config.TrainingDataWindowDays)).ToList();

                _logger.LogInformation("Training {Method} forecast model for {MetricName} with {Count} data points",
                    forecastingMethod, metricName, history.Count);

                var strategy = _methodManager.GetStrategy(forecastingMethod);
                if (strategy == null)
                {
                    _logger.LogWarning("Unsupported forecasting method: {Method} for {MetricName}", forecastingMethod, metricName);
                    return;
                }

                var model = strategy.TrainModel(_mlContext, history, _config.DefaultForecastHorizon);

                _modelManager.StoreModel(metricName, model, forecastingMethod);

                _logger.LogInformation("{Method} forecast model trained for {MetricName} with horizon={Horizon}",
                    forecastingMethod, metricName, _config.DefaultForecastHorizon);
            }
            catch (InsufficientDataException)
            {
                // Re-throw InsufficientDataException to be handled by higher level
                throw;
            }
            catch (OperationCanceledException)
            {
                // Re-throw OperationCanceledException for proper cancellation handling
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training {Method} forecast model for {MetricName}", forecastingMethod, metricName);
                // Do not throw - handle gracefully
            }
        }

        public async Task TrainModelAsync(string metricName, ForecastingMethod? method = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                TrainModel(metricName, method);
            }, cancellationToken);
        }

        public bool HasSufficientData(string metricName, out int actualCount)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            var history = _repository.GetHistory(metricName, TimeSpan.FromDays(_config.TrainingDataWindowDays));
            actualCount = history.Count();
            return actualCount >= _config.MinimumDataPoints;
        }
    }
}