 using System;
 using System.Threading;
 using System.Threading.Tasks;
 using Microsoft.Extensions.Logging;

 namespace Relay.Core.AI.Analysis.TimeSeries;

 /// <summary>
 /// Service for time-series forecasting operations
 /// </summary>
 internal class ForecastingService : IForecastingService
 {
     private readonly ILogger<ForecastingService> _logger;
     private readonly IForecastingTrainer _trainer;
     private readonly IForecastingPredictor _predictor;
     private readonly IForecastingMethodManager _methodManager;

     public ForecastingService(
         ILogger<ForecastingService> logger,
         IForecastingTrainer trainer,
         IForecastingPredictor predictor,
         IForecastingMethodManager methodManager)
     {
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _trainer = trainer ?? throw new ArgumentNullException(nameof(trainer));
         _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
         _methodManager = methodManager ?? throw new ArgumentNullException(nameof(methodManager));

         _logger.LogInformation("Forecasting service initialized");
     }

      /// <inheritdoc/>
      public void TrainForecastModel(string metricName, ForecastingMethod? method = null)
      {
          if (string.IsNullOrWhiteSpace(metricName))
              throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));

          if (method.HasValue)
          {
              _methodManager.SetForecastingMethod(metricName, method.Value);
          }

          _trainer.TrainModel(metricName, method);
      }

      /// <inheritdoc/>
      public async Task TrainForecastModelAsync(string metricName, ForecastingMethod? method = null, CancellationToken cancellationToken = default)
      {
          if (string.IsNullOrWhiteSpace(metricName))
              throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));

          if (method.HasValue)
          {
              _methodManager.SetForecastingMethod(metricName, method.Value);
          }

          await _trainer.TrainModelAsync(metricName, method, cancellationToken);
      }

      /// <inheritdoc/>
      public MetricForecastResult? Forecast(string metricName, int horizon = 12)
      {
          if (string.IsNullOrWhiteSpace(metricName))
              throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));
          if (horizon <= 0)
              throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon must be greater than zero");

          return _predictor.Predict(metricName, horizon);
      }

      /// <inheritdoc/>
      public async Task<MetricForecastResult?> ForecastAsync(string metricName, int horizon = 12, CancellationToken cancellationToken = default)
      {
          if (string.IsNullOrWhiteSpace(metricName))
              throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));
          if (horizon <= 0)
              throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon must be greater than zero");

          return await _predictor.PredictAsync(metricName, horizon, cancellationToken);
      }

      /// <inheritdoc/>
      public ForecastingMethod GetForecastingMethod(string metricName)
      {
          if (string.IsNullOrWhiteSpace(metricName))
              throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));

          return _methodManager.GetForecastingMethod(metricName);
      }

      /// <inheritdoc/>
      public void SetForecastingMethod(string metricName, ForecastingMethod method)
      {
          if (string.IsNullOrWhiteSpace(metricName))
              throw new ArgumentException("Metric name cannot be null or whitespace", nameof(metricName));

          _methodManager.SetForecastingMethod(metricName, method);
      }
}