using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Relay.Core.AI.Optimization.Models;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Service for anomaly detection in time-series data
/// </summary>
internal class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly ILogger<AnomalyDetectionService> _logger;
    private readonly ITimeSeriesRepository _repository;
    private readonly MLContext _mlContext;

    public AnomalyDetectionService(
        ILogger<AnomalyDetectionService> logger,
        ITimeSeriesRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mlContext = new MLContext(seed: 42);
    }

    /// <inheritdoc/>
    public List<AnomalyDetectionResult> DetectAnomalies(string metricName, int lookbackPoints = 100)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(metricName));

        if (lookbackPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(lookbackPoints), "Lookback points must be positive");

        try
        {
            var history = _repository.GetHistory(metricName).TakeLast(lookbackPoints).ToList();

            if (history.Count < 12)
            {
                _logger.LogWarning("Insufficient data for anomaly detection in {MetricName}: {Count} data points (minimum 12 required). Returning empty results.",
                    metricName, history.Count);
                return new List<AnomalyDetectionResult>();
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
            throw new AnomalyDetectionException($"Failed to detect anomalies for metric '{metricName}'", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(string metricName, int lookbackPoints = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return DetectAnomalies(metricName, lookbackPoints);
        }, cancellationToken);
    }
}