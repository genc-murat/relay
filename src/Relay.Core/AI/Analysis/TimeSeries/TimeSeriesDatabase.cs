using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Relay.Core.AI.Optimization.Models;

namespace Relay.Core.AI
{
    /// <summary>
    /// Time-series database for storing and analyzing metric trends using ML.NET
    /// </summary>
    internal class TimeSeriesDatabase : IDisposable
    {
        private readonly ILogger<TimeSeriesDatabase> _logger;
        private readonly MLContext _mlContext;
        private readonly ConcurrentDictionary<string, CircularBuffer<MetricDataPoint>> _metricHistories;
        private readonly ConcurrentDictionary<string, ITransformer> _forecastModels;
        private readonly int _maxHistorySize;
        private readonly int _forecastHorizon;
        private readonly int _windowSize;
        private bool _disposed;

        public TimeSeriesDatabase(ILogger<TimeSeriesDatabase> logger, int maxHistorySize = 10000, IConfiguration? configuration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxHistorySize = configuration?.GetValue<int>("TimeSeries:MaxHistorySize", maxHistorySize) ?? maxHistorySize;
            _forecastHorizon = configuration?.GetValue<int>("TimeSeries:ForecastHorizon", 24) ?? 24;
            _windowSize = configuration?.GetValue<int>("TimeSeries:WindowSize", 48) ?? 48;
            _mlContext = new MLContext(seed: 42);
            _metricHistories = new ConcurrentDictionary<string, CircularBuffer<MetricDataPoint>>();
            _forecastModels = new ConcurrentDictionary<string, ITransformer>();

            _logger.LogInformation("Time-series database initialized with max history size: {MaxSize}, forecast horizon: {Horizon}, window size: {WindowSize}",
                _maxHistorySize, _forecastHorizon, _windowSize);
        }

        /// <summary>
        /// Store metric data point for time-series analysis
        /// </summary>
        public void StoreMetric(string metricName, double value, DateTime timestamp, 
            double? movingAverage5 = null, double? movingAverage15 = null, 
            TrendDirection trend = TrendDirection.Stable)
        {
            try
            {
                var dataPoint = new MetricDataPoint
                {
                    MetricName = metricName,
                    Timestamp = timestamp,
                    Value = (float)value,
                    MA5 = movingAverage5.HasValue ? (float)movingAverage5.Value : (float)value,
                    MA15 = movingAverage15.HasValue ? (float)movingAverage15.Value : (float)value,
                    Trend = (int)trend,
                    HourOfDay = timestamp.Hour,
                    DayOfWeek = (int)timestamp.DayOfWeek
                };

                var history = _metricHistories.GetOrAdd(metricName, 
                    _ => new CircularBuffer<MetricDataPoint>(_maxHistorySize));
                
                history.Add(dataPoint);

                _logger.LogTrace("Stored metric {MetricName} with value {Value} at {Timestamp}", 
                    metricName, value, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error storing metric {MetricName}", metricName);
            }
        }

        /// <summary>
        /// Store multiple metrics at once
        /// </summary>
        public void StoreBatch(Dictionary<string, double> metrics, DateTime timestamp,
            Dictionary<string, MovingAverageData>? movingAverages = null,
            Dictionary<string, TrendDirection>? trendDirections = null)
        {
            foreach (var metric in metrics)
            {
                var ma = movingAverages?.GetValueOrDefault(metric.Key);
                var trend = trendDirections?.GetValueOrDefault(metric.Key, TrendDirection.Stable) ?? TrendDirection.Stable;

                StoreMetric(metric.Key, metric.Value, timestamp, ma?.MA5, ma?.MA15, trend);
            }
        }

        /// <summary>
        /// Get historical data for a specific metric
        /// </summary>
        public IEnumerable<MetricDataPoint> GetHistory(string metricName, TimeSpan? lookbackPeriod = null)
        {
            if (!_metricHistories.TryGetValue(metricName, out var history))
            {
                return Enumerable.Empty<MetricDataPoint>();
            }

            var allData = history.AsEnumerable();

            if (lookbackPeriod.HasValue)
            {
                var cutoffTime = DateTime.UtcNow - lookbackPeriod.Value;
                return allData.Where(d => d.Timestamp >= cutoffTime).OrderBy(d => d.Timestamp);
            }

            return allData.OrderBy(d => d.Timestamp);
        }

        /// <summary>
        /// Get most recent N metrics for a specific metric name
        /// </summary>
        public List<MetricDataPoint> GetRecentMetrics(string metricName, int count)
        {
            if (!_metricHistories.TryGetValue(metricName, out var history))
            {
                return new List<MetricDataPoint>();
            }

            return history
                .OrderByDescending(d => d.Timestamp)
                .Take(count)
                .OrderBy(d => d.Timestamp) // Re-order chronologically
                .ToList();
        }

        /// <summary>
        /// Train or update forecast model for a specific metric using ML.NET SSA
        /// </summary>
        public void TrainForecastModel(string metricName)
        {
            try
            {
                var history = GetHistory(metricName, TimeSpan.FromDays(7)).ToList();

                if (history.Count < _windowSize)
                {
                    throw new InsufficientDataException($"Insufficient data for {metricName}: {history.Count} data points (minimum {_windowSize} required)");
                }

                _logger.LogInformation("Training forecast model for {MetricName} with {Count} data points", 
                    metricName, history.Count);

                // Prepare data for ML.NET
                var dataView = _mlContext.Data.LoadFromEnumerable(history);

                // Create SSA (Singular Spectrum Analysis) forecasting pipeline
                var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MetricForecastResult.ForecastedValues),
                    inputColumnName: nameof(MetricDataPoint.Value),
                    windowSize: _windowSize,
                    seriesLength: history.Count,
                    trainSize: history.Count,
                    horizon: _forecastHorizon,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: nameof(MetricForecastResult.LowerBound),
                    confidenceUpperBoundColumn: nameof(MetricForecastResult.UpperBound));

                // Train the model
                var model = forecastingPipeline.Fit(dataView);
                _forecastModels[metricName] = model;

                _logger.LogInformation("Forecast model trained for {MetricName} with horizon={Horizon}, windowSize={WindowSize}",
                    metricName, _forecastHorizon, _windowSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training forecast model for {MetricName}", metricName);
            }
        }

        /// <summary>
        /// Train or update forecast model for a specific metric using ML.NET SSA (async)
        /// </summary>
        public Task TrainForecastModelAsync(string metricName)
        {
            return Task.Run(() => TrainForecastModel(metricName));
        }

        /// <summary>
        /// Forecast future values for a metric using ML.NET
        /// </summary>
        public MetricForecastResult? Forecast(string metricName, int horizon = 12)
        {
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

                var history = GetHistory(metricName, TimeSpan.FromDays(7)).ToList();
                if (history.Count == 0)
                {
                    throw new InsufficientDataException($"No data available for forecasting {metricName}");
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(history);
                
                // Create forecast using the model
                // Note: For production, you would need to properly handle time-series prediction
                // This is a simplified version
                _logger.LogInformation("Forecast model available for {MetricName}. " +
                    "Note: Full forecasting requires CreateTimeSeriesEngine which is version-specific.", metricName);
                
                // Full forecasting implementation would use TimeSeriesPredictionEngine
                // For now, return null as placeholder
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forecasting for {MetricName}", metricName);
                return null;
            }
        }

        /// <summary>
        /// Forecast future values for a metric using ML.NET (async)
        /// </summary>
        public Task<MetricForecastResult?> ForecastAsync(string metricName)
        {
            return Task.Run(() => Forecast(metricName));
        }

        /// <summary>
        /// Detect anomalies in time-series data using ML.NET
        /// </summary>
        public List<AnomalyDetectionResult> DetectAnomalies(string metricName, int lookbackPoints = 100)
        {
            try
            {
                var history = GetHistory(metricName).TakeLast(lookbackPoints).ToList();

                if (history.Count < 12)
                {
                    throw new InsufficientDataException($"Insufficient data for anomaly detection in {metricName}: {history.Count} data points (minimum 12 required)");
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(history);

                // Use SR-CNN for anomaly detection
                var anomalyPipeline = _mlContext.Transforms.DetectAnomalyBySrCnn(
                    outputColumnName: nameof(AnomalyPrediction.Prediction),
                    inputColumnName: nameof(MetricDataPoint.Value),
                    threshold: 0.35);

                var transformedData = anomalyPipeline.Fit(dataView).Transform(dataView);
                var predictions = _mlContext.Data.CreateEnumerable<AnomalyPrediction>(transformedData, reuseRowObject: false).ToList();

                var anomalies = new List<AnomalyDetectionResult>();
                for (int i = 0; i < Math.Min(history.Count, predictions.Count); i++)
                {
                    var prediction = predictions[i].Prediction;
                    // prediction[0] = is anomaly (0 or 1)
                    // prediction[1] = raw score
                    // prediction[2] = magnitude
                    
                    if (prediction[0] == 1)
                    {
                        anomalies.Add(new AnomalyDetectionResult
                        {
                            MetricName = metricName,
                            Timestamp = history[i].Timestamp,
                            Value = history[i].Value,
                            IsAnomaly = true,
                            Score = (float)prediction[1],
                            Magnitude = (float)prediction[2]
                        });
                    }
                }

                if (anomalies.Count > 0)
                {
                    _logger.LogInformation("Detected {Count} anomalies in {MetricName}", anomalies.Count, metricName);
                }

                return anomalies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting anomalies for {MetricName}", metricName);
                return new List<AnomalyDetectionResult>();
            }
        }

        /// <summary>
        /// Detect anomalies in time-series data using ML.NET (async)
        /// </summary>
        public Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(string metricName, int lookbackPoints = 100)
        {
            return Task.Run(() => DetectAnomalies(metricName, lookbackPoints));
        }

        /// <summary>
        /// Calculate statistics for a metric
        /// </summary>
        /// Calculate statistics for a metric
        /// </summary>
        public MetricStatistics? GetStatistics(string metricName, TimeSpan? period = null)
        {
            var history = GetHistory(metricName, period).ToList();

            if (history.Count == 0)
            {
                return null;
            }

            var values = history.Select(h => h.Value).ToArray();

            return new MetricStatistics
            {
                MetricName = metricName,
                Count = values.Length,
                Mean = values.Average(),
                Min = values.Min(),
                Max = values.Max(),
                StdDev = TimeSeriesStatistics.CalculateStdDev(values),
                Median = TimeSeriesStatistics.CalculateMedian(values),
                P95 = TimeSeriesStatistics.CalculatePercentile(values, 0.95),
                P99 = TimeSeriesStatistics.CalculatePercentile(values, 0.99)
            };
        }

        /// <summary>
        /// Clean up old data beyond retention period
        /// </summary>
        public void CleanupOldData(TimeSpan retentionPeriod)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - retentionPeriod;
                var totalRemoved = 0;

                foreach (var kvp in _metricHistories)
                {
                    var history = kvp.Value;
                    var originalCount = history.Count;
                    
                    // Remove old data points efficiently
                    var oldCount = history.Where(d => d.Timestamp < cutoffTime).Count();
                    history.RemoveFront(oldCount);

                    totalRemoved += originalCount - history.Count;
                }

                if (totalRemoved > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old time-series data points", totalRemoved);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old time-series data");
            }
        }



        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _metricHistories.Clear();
            _forecastModels.Clear();

            _logger.LogInformation("Time-series database disposed");
        }
    }
}
