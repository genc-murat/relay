using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Analysis.TimeSeries
{
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
    }
}