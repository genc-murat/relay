using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries;

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
    private readonly ConcurrentDictionary<string, PredictionMetadata> _predictionMetadata;

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
        _predictionMetadata = new ConcurrentDictionary<string, PredictionMetadata>();
    }

    public MetricForecastResult? Predict(string metricName, int horizon = 12)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

        if (horizon <= 0)
            throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon must be positive");

        var metadata = new PredictionMetadata
        {
            MetricName = metricName,
            Horizon = horizon,
            PredictedAt = DateTime.UtcNow
        };

        try
        {
            if (!_modelManager.HasModel(metricName))
            {
                if (_config.AutoTrainOnForecast)
                {
                    _logger.LogDebug("No forecast model available for {MetricName}, training new model", metricName);
                    _trainer.TrainModel(metricName);
                    metadata.AutoTrained = true;

                    if (!_modelManager.HasModel(metricName))
                    {
                        metadata.Success = false;
                        metadata.ErrorMessage = "Failed to train model";
                        _predictionMetadata[metricName] = metadata;
                        return null;
                    }
                }
                else
                {
                    _logger.LogDebug("No forecast model available for {MetricName}", metricName);
                    metadata.Success = false;
                    metadata.ErrorMessage = "No model available and auto-training disabled";
                    _predictionMetadata[metricName] = metadata;
                    return null;
                }
            }

            var model = _modelManager.GetModel(metricName);
            if (model == null)
            {
                metadata.Success = false;
                metadata.ErrorMessage = "Model not found";
                _predictionMetadata[metricName] = metadata;
                return null;
            }

            var method = _modelManager.GetMethod(metricName);
            metadata.Method = method;

            var history = _repository.GetHistory(metricName, TimeSpan.FromDays(_config.TrainingDataWindowDays)).ToList();
            metadata.TrainingDataPoints = history.Count;

            if (history.Count == 0)
            {
                _logger.LogWarning("No data available for forecasting {MetricName}", metricName);
                metadata.Success = false;
                metadata.ErrorMessage = "No historical data available";
                _predictionMetadata[metricName] = metadata;
                return null;
            }

            if (history.Count < _config.MinimumDataPoints)
            {
                _logger.LogWarning("Insufficient data for forecasting {MetricName}: {Count} points, minimum {Minimum}",
                    metricName, history.Count, _config.MinimumDataPoints);
                metadata.Success = false;
                metadata.ErrorMessage = $"Insufficient data: {history.Count} points, minimum {_config.MinimumDataPoints}";
                _predictionMetadata[metricName] = metadata;
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

                metadata.Success = true;
                _predictionMetadata[metricName] = metadata;
                return forecast;
            }
            else
            {
                _logger.LogWarning("No forecast data generated for {MetricName}", metricName);
                metadata.Success = false;
                metadata.ErrorMessage = "No forecast data generated";
                _predictionMetadata[metricName] = metadata;
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forecasting for {MetricName}", metricName);
            metadata.Success = false;
            metadata.ErrorMessage = ex.Message;
            _predictionMetadata[metricName] = metadata;
            return null;
        }
    }

    public async Task<MetricForecastResult?> PredictAsync(string metricName, int horizon = 12, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // For CPU-bound operations, use Task.Run to avoid blocking the thread pool
        return await Task.Run(() => Predict(metricName, horizon), cancellationToken);
    }

    public IDictionary<string, MetricForecastResult?> PredictBatch(IEnumerable<string> metricNames, int horizon = 12)
    {
        if (metricNames == null)
            throw new ArgumentNullException(nameof(metricNames));

        if (horizon <= 0)
            throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon must be positive");

        var results = new Dictionary<string, MetricForecastResult?>();

        foreach (var metricName in metricNames.Distinct())
        {
            try
            {
                var result = Predict(metricName, horizon);
                results[metricName] = result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch prediction for {MetricName}", metricName);
                results[metricName] = null;
            }
        }

        _logger.LogInformation("Completed batch prediction for {Count} metrics", results.Count);
        return results;
    }

    public async Task<IDictionary<string, MetricForecastResult?>> PredictBatchAsync(IEnumerable<string> metricNames, int horizon = 12, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(() => PredictBatch(metricNames, horizon), cancellationToken);
    }

    public bool CanPredict(string metricName)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

        // Check if model exists
        if (_modelManager.HasModel(metricName))
            return true;

        // Check if auto-training is enabled and we have sufficient data
        if (_config.AutoTrainOnForecast)
        {
            var history = _repository.GetHistory(metricName, TimeSpan.FromDays(_config.TrainingDataWindowDays)).ToList();
            return history.Count >= _config.MinimumDataPoints;
        }

        return false;
    }

    public PredictionMetadata? GetPredictionMetadata(string metricName)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null, empty, or whitespace", nameof(metricName));

        return _predictionMetadata.TryGetValue(metricName, out var metadata) ? metadata : null;
    }
}