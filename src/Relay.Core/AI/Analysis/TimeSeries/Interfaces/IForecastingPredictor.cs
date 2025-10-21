using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for making forecasts using trained models
/// </summary>
public interface IForecastingPredictor
{
    /// <summary>
    /// Makes a forecast for a metric
    /// </summary>
    MetricForecastResult? Predict(string metricName, int horizon = 12);

    /// <summary>
    /// Makes a forecast for a metric (async)
    /// </summary>
    Task<MetricForecastResult?> PredictAsync(string metricName, int horizon = 12, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes forecasts for multiple metrics
    /// </summary>
    IDictionary<string, MetricForecastResult?> PredictBatch(IEnumerable<string> metricNames, int horizon = 12);

    /// <summary>
    /// Makes forecasts for multiple metrics (async)
    /// </summary>
    Task<IDictionary<string, MetricForecastResult?>> PredictBatchAsync(IEnumerable<string> metricNames, int horizon = 12, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a metric can be forecasted
    /// </summary>
    bool CanPredict(string metricName);

    /// <summary>
    /// Gets prediction metadata for a metric
    /// </summary>
    PredictionMetadata? GetPredictionMetadata(string metricName);
}