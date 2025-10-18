using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Handles forecasting predictions using trained models
    /// </summary>
    internal sealed class ForecastingPredictor : IForecastingPredictor
    {
        private readonly ILogger<ForecastingPredictor> _logger;
        private readonly ITimeSeriesRepository _repository;
        private readonly IForecastingModelManager _modelManager;
        private readonly IForecastingTrainer _trainer;
        private readonly ForecastingConfiguration _config;
        private readonly MLContext _mlContext;

        public ForecastingPredictor(
            ILogger<ForecastingPredictor> logger,
            ITimeSeriesRepository repository,
            IForecastingModelManager modelManager,
            IForecastingTrainer trainer,
            ForecastingConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _trainer = trainer ?? throw new ArgumentNullException(nameof(trainer));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mlContext = new MLContext(seed: _config.MlContextSeed);
        }

        public MetricForecastResult? Predict(string metricName, int horizon = 12)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            if (horizon <= 0)
                throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon must be positive");

            try
            {
                if (!_modelManager.HasModel(metricName))
                {
                    if (_config.AutoTrainOnForecast)
                    {
                        _logger.LogDebug("No forecast model available for {MetricName}, training new model", metricName);
                        _trainer.TrainModel(metricName);

                        if (!_modelManager.HasModel(metricName))
                        {
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("No forecast model available for {MetricName}", metricName);
                        return null;
                    }
                }

                var model = _modelManager.GetModel(metricName);
                if (model == null)
                {
                    return null;
                }

                var history = _repository.GetHistory(metricName, TimeSpan.FromDays(_config.TrainingDataWindowDays)).ToList();
                if (history.Count == 0)
                {
                    _logger.LogWarning("No data available for forecasting {MetricName}", metricName);
                    return null;
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(history);

                // Transform the historical data to get forecasts
                var transformedData = model.Transform(dataView);
                var forecastRows = _mlContext.Data.CreateEnumerable<MetricForecastResult>(transformedData, reuseRowObject: false).ToList();

                // Use the forecast from the last data point (most recent)
                if (forecastRows.Count > 0)
                {
                    var forecast = forecastRows.Last();
                    _logger.LogInformation("Forecasted {Count} values for {MetricName} with horizon {Horizon}",
                        forecast.ForecastedValues.Length, metricName, horizon);
                    return forecast;
                }
                else
                {
                    _logger.LogWarning("No forecast data generated for {MetricName}", metricName);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forecasting for {MetricName}", metricName);
                return null;
            }
        }

        public async Task<MetricForecastResult?> PredictAsync(string metricName, int horizon = 12, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Predict(metricName, horizon);
            }, cancellationToken);
        }
    }
}