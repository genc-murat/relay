using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Relay.Core.AI.Analysis.TimeSeries.ForecastingStrategies;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Service for time-series forecasting operations
    /// </summary>
    internal class ForecastingService : IForecastingService
    {
        private readonly ILogger _logger;
        private readonly ITimeSeriesRepository _repository;
        private readonly MLContext _mlContext;
        private readonly ConcurrentDictionary<string, ITransformer> _forecastModels;
        private readonly ConcurrentDictionary<string, ForecastingMethod> _forecastMethods;
        private readonly ConcurrentDictionary<ForecastingMethod, IForecastingStrategy> _strategies;
        private readonly int _forecastHorizon;
        private readonly ForecastingMethod _defaultForecastingMethod;

        public ForecastingService(
            ILogger logger,
            ITimeSeriesRepository repository,
            int forecastHorizon = 24,
            ForecastingMethod defaultMethod = ForecastingMethod.SSA)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _forecastHorizon = forecastHorizon;
            _defaultForecastingMethod = defaultMethod;
            _mlContext = new MLContext(seed: 42);
            _forecastModels = new ConcurrentDictionary<string, ITransformer>();
            _forecastMethods = new ConcurrentDictionary<string, ForecastingMethod>();
            _strategies = new ConcurrentDictionary<ForecastingMethod, IForecastingStrategy>();

            // Register strategies
            RegisterStrategy(new SsaForecastingStrategy());
            RegisterStrategy(new ExponentialSmoothingForecastingStrategy());
            RegisterStrategy(new MovingAverageForecastingStrategy());
            RegisterStrategy(new EnsembleForecastingStrategy());

            _logger.LogInformation("Forecasting service initialized with horizon={Horizon}, method={Method}",
                _forecastHorizon, _defaultForecastingMethod);
        }

        private void RegisterStrategy(IForecastingStrategy strategy)
        {
            _strategies[strategy.Method] = strategy;
        }

        /// <inheritdoc/>
        public void TrainForecastModel(string metricName, ForecastingMethod? method = null)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            var forecastingMethod = method ?? _defaultForecastingMethod;

            try
            {
                var history = _repository.GetHistory(metricName, TimeSpan.FromDays(7)).ToList();

                if (history.Count < 10) // Minimum data requirement
                {
                    var ex = new InsufficientDataException(
                        $"Insufficient data for {forecastingMethod} forecasting in {metricName}: {history.Count} data points (minimum 10 required)")
                    {
                        MetricName = metricName,
                        Operation = $"Train{forecastingMethod}Model",
                        MinimumRequired = 10,
                        ActualCount = history.Count
                    };
                    _logger.LogError(ex, "Error training {Method} forecast model for {MetricName}", forecastingMethod, metricName);
                    return;
                }

                _logger.LogInformation("Training {Method} forecast model for {MetricName} with {Count} data points",
                    forecastingMethod, metricName, history.Count);

                if (!_strategies.TryGetValue(forecastingMethod, out var strategy))
                {
                    _logger.LogWarning("Unsupported forecasting method: {Method} for {MetricName}", forecastingMethod, metricName);
                    return;
                }

                var model = strategy.TrainModel(_mlContext, history, _forecastHorizon);

                _forecastModels[metricName] = model;
                _forecastMethods[metricName] = forecastingMethod;

                _logger.LogInformation("{Method} forecast model trained for {MetricName} with horizon={Horizon}",
                    forecastingMethod, metricName, _forecastHorizon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training {Method} forecast model for {MetricName}", forecastingMethod, metricName);
                // Do not throw - handle gracefully
            }
        }

        /// <inheritdoc/>
        public async Task TrainForecastModelAsync(string metricName, ForecastingMethod? method = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                TrainForecastModel(metricName, method);
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public MetricForecastResult? Forecast(string metricName, int horizon = 12)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            if (horizon <= 0)
                throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon must be positive");

            try
            {
                if (!_forecastModels.TryGetValue(metricName, out var model))
                {
                    _logger.LogDebug("No forecast model available for {MetricName}, training new model", metricName);
                    TrainForecastModel(metricName);

                    if (!_forecastModels.TryGetValue(metricName, out model))
                    {
                        return null;
                    }
                }

                var history = _repository.GetHistory(metricName, TimeSpan.FromDays(7)).ToList();
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

        /// <inheritdoc/>
        public async Task<MetricForecastResult?> ForecastAsync(string metricName, int horizon = 12, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Forecast(metricName, horizon);
            }, cancellationToken);
        }

        /// <inheritdoc/>
        public ForecastingMethod GetForecastingMethod(string metricName)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            return _forecastMethods.GetValueOrDefault(metricName, _defaultForecastingMethod);
        }

        /// <inheritdoc/>
        public void SetForecastingMethod(string metricName, ForecastingMethod method)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

            _forecastMethods[metricName] = method;
            _logger.LogInformation("Forecasting method for {MetricName} set to {Method}", metricName, method);
        }
    }
}