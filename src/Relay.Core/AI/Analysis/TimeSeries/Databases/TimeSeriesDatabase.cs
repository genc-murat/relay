using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Forecasting methods available for time-series analysis
    /// </summary>
    public enum ForecastingMethod
    {
        /// <summary>
        /// Singular Spectrum Analysis - Good for seasonal data
        /// </summary>
        SSA,

        /// <summary>
        /// Simple Exponential Smoothing - Good for trending data
        /// </summary>
        ExponentialSmoothing,

        /// <summary>
        /// Moving Average forecasting - Simple but effective
        /// </summary>
        MovingAverage,

        /// <summary>
        /// Ensemble method combining multiple approaches
        /// </summary>
        Ensemble
    }

    /// <summary>
    /// Time-series database facade for storing and analyzing metric trends
    /// </summary>
    public class TimeSeriesDatabase : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly ITimeSeriesRepository _repository;
        private readonly IForecastingService _forecastingService;
        private readonly IAnomalyDetectionService _anomalyDetectionService;
        private readonly ITimeSeriesStatisticsService _statisticsService;
        private bool _disposed;

        public TimeSeriesDatabase(
            ILogger<TimeSeriesDatabase> logger,
            ITimeSeriesRepository repository,
            IForecastingService forecastingService,
            IAnomalyDetectionService anomalyDetectionService,
            ITimeSeriesStatisticsService statisticsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _forecastingService = forecastingService ?? throw new ArgumentNullException(nameof(forecastingService));
            _anomalyDetectionService = anomalyDetectionService ?? throw new ArgumentNullException(nameof(anomalyDetectionService));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));

            _logger.LogInformation("Time-series database initialized");
        }

        // Factory method for backward compatibility
        public static TimeSeriesDatabase Create(
            ILogger<TimeSeriesDatabase> logger,
            int maxHistorySize = 10000,
            IConfiguration? configuration = null)
        {
            // Validate and get configuration values
            var validatedMaxHistorySize = ValidateMaxHistorySize(maxHistorySize, configuration);
            var forecastHorizon = ValidateForecastHorizon(configuration);
            var defaultMethod = ValidateForecastingMethod(configuration);

            // Create loggers for each service
            var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var repositoryLogger = loggerFactory.CreateLogger<TimeSeriesRepository>();
            var anomalyLogger = loggerFactory.CreateLogger<AnomalyDetectionService>();
            var statisticsLogger = loggerFactory.CreateLogger<TimeSeriesStatisticsService>();

            var repository = new TimeSeriesRepository(repositoryLogger, validatedMaxHistorySize);

            // Create forecasting service dependencies
            var config = new ForecastingConfiguration
            {
                DefaultForecastHorizon = forecastHorizon,
                DefaultForecastingMethod = defaultMethod
            };
            var modelManagerLogger = loggerFactory.CreateLogger<ForecastingModelManager>();
            var modelManager = new ForecastingModelManager(modelManagerLogger);
            var methodManagerLogger = loggerFactory.CreateLogger<ForecastingMethodManager>();
            var methodManager = new ForecastingMethodManager(methodManagerLogger, config);
            var trainerLogger = loggerFactory.CreateLogger<ForecastingTrainer>();
            var trainer = new ForecastingTrainer(trainerLogger, repository, modelManager, methodManager, config);
            var predictorLogger = loggerFactory.CreateLogger<ForecastingPredictor>();
            var predictor = new ForecastingPredictor(predictorLogger, repository, modelManager, trainer, config);
            var forecastingServiceLogger = loggerFactory.CreateLogger<ForecastingService>();
            var forecastingService = new ForecastingService(forecastingServiceLogger, trainer, predictor, methodManager);
            var anomalyDetectionService = new AnomalyDetectionService(anomalyLogger, repository);
            var statisticsService = new TimeSeriesStatisticsService(statisticsLogger, repository);

            return new TimeSeriesDatabase(logger, repository, forecastingService, anomalyDetectionService, statisticsService);
        }

        private static int ValidateMaxHistorySize(int defaultValue, IConfiguration? configuration)
        {
            var configValue = configuration?.GetValue<int?>("TimeSeries:MaxHistorySize");
            if (configValue.HasValue)
            {
                if (configValue.Value <= 0)
                    throw new ArgumentException("MaxHistorySize must be positive", nameof(configuration));
                if (configValue.Value > 1000000)
                    throw new ArgumentException("MaxHistorySize cannot exceed 1,000,000", nameof(configuration));
                return configValue.Value;
            }
            return defaultValue;
        }

        private static int ValidateForecastHorizon(IConfiguration? configuration)
        {
            var configValue = configuration?.GetValue<int?>("TimeSeries:ForecastHorizon");
            if (configValue.HasValue)
            {
                if (configValue.Value <= 0)
                    throw new ArgumentException("ForecastHorizon must be positive", nameof(configuration));
                if (configValue.Value > 1000)
                    throw new ArgumentException("ForecastHorizon cannot exceed 1000", nameof(configuration));
                return configValue.Value;
            }
            return 24;
        }

        private static ForecastingMethod ValidateForecastingMethod(IConfiguration? configuration)
        {
            var configValue = configuration?.GetValue<string?>("TimeSeries:ForecastingMethod");
            if (!string.IsNullOrEmpty(configValue))
            {
                if (!Enum.TryParse<ForecastingMethod>(configValue, out var method))
                    throw new ArgumentException($"Invalid ForecastingMethod: {configValue}", nameof(configuration));
                return method;
            }
            return ForecastingMethod.SSA;
        }

        /// <summary>
        /// Store metric data point for time-series analysis
        /// </summary>
        public void StoreMetric(string metricName, double value, DateTime timestamp,
            double? movingAverage5 = null, double? movingAverage15 = null,
            TrendDirection trend = TrendDirection.Stable)
        {
            _repository.StoreMetric(metricName, value, timestamp, movingAverage5, movingAverage15, trend);
        }

        /// <summary>
        /// Store multiple metrics at once
        /// </summary>
        public void StoreBatch(Dictionary<string, double> metrics, DateTime timestamp,
            Dictionary<string, MovingAverageData>? movingAverages = null,
            Dictionary<string, TrendDirection>? trendDirections = null)
        {
            _repository.StoreBatch(metrics, timestamp, movingAverages, trendDirections);
        }

        /// <summary>
        /// Get historical data for a specific metric
        /// </summary>
        public IEnumerable<MetricDataPoint> GetHistory(string metricName, TimeSpan? lookbackPeriod = null)
        {
            return _repository.GetHistory(metricName, lookbackPeriod);
        }

        /// <summary>
        /// Get most recent N metrics for a specific metric name
        /// </summary>
        public virtual List<MetricDataPoint> GetRecentMetrics(string metricName, int count)
        {
            return _repository.GetRecentMetrics(metricName, count);
        }

        /// <summary>
        /// Train or update forecast model for a specific metric using specified forecasting method
        /// </summary>
        public void TrainForecastModel(string metricName, ForecastingMethod? method = null)
        {
            try
            {
                _forecastingService.TrainForecastModel(metricName, method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training forecast model for {MetricName}", metricName);
                // Do not throw - handle gracefully
            }
        }

        /// <summary>
        /// Train or update forecast model for a specific metric using ML.NET (async)
        /// </summary>
        public async Task TrainForecastModelAsync(string metricName, ForecastingMethod? method = null)
        {
            try
            {
                await _forecastingService.TrainForecastModelAsync(metricName, method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training forecast model for {MetricName}", metricName);
                // Do not throw - handle gracefully
            }
        }

        /// <summary>
        /// Forecast future values for a metric using ML.NET
        /// </summary>
        public MetricForecastResult? Forecast(string metricName, int horizon = 12)
        {
            return _forecastingService.Forecast(metricName, horizon);
        }

        /// <summary>
        /// Forecast future values for a metric using ML.NET (async)
        /// </summary>
        public Task<MetricForecastResult?> ForecastAsync(string metricName)
        {
            return _forecastingService.ForecastAsync(metricName);
        }

        /// <summary>
        /// Detect anomalies in time-series data using ML.NET
        /// </summary>
        public List<AnomalyDetectionResult> DetectAnomalies(string metricName, int lookbackPoints = 100)
        {
            return _anomalyDetectionService.DetectAnomalies(metricName, lookbackPoints);
        }

        /// <summary>
        /// Detect anomalies in time-series data using ML.NET (async)
        /// </summary>
        public Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(string metricName, int lookbackPoints = 100)
        {
            return _anomalyDetectionService.DetectAnomaliesAsync(metricName, lookbackPoints);
        }

        /// <summary>
        /// Calculate statistics for a metric
        /// </summary>
        public MetricStatistics? GetStatistics(string metricName, TimeSpan? period = null)
        {
            return _statisticsService.GetStatistics(metricName, period);
        }

        /// <summary>
        /// Clean up old data beyond retention period
        /// </summary>
        public void CleanupOldData(TimeSpan retentionPeriod)
        {
            _repository.CleanupOldData(retentionPeriod);
        }



        /// <summary>
        /// Get the forecasting method used for a specific metric
        /// </summary>
        public ForecastingMethod GetForecastingMethod(string metricName)
        {
            return _forecastingService.GetForecastingMethod(metricName);
        }

        /// <summary>
        /// Set the forecasting method for a specific metric
        /// </summary>
        public void SetForecastingMethod(string metricName, ForecastingMethod method)
        {
            _forecastingService.SetForecastingMethod(metricName, method);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // Clear all data
            _repository.Clear();

            // Dispose services if they implement IDisposable
            (_repository as IDisposable)?.Dispose();
            (_forecastingService as IDisposable)?.Dispose();
            (_anomalyDetectionService as IDisposable)?.Dispose();
            (_statisticsService as IDisposable)?.Dispose();

            _logger.LogInformation("Time-series database disposed");
        }
    }
}
