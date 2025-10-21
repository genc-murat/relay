using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for time-series forecasting operations
/// </summary>
public interface IForecastingService
{
    /// <summary>
    /// Train or update forecast model for a specific metric
    /// </summary>
    void TrainForecastModel(string metricName, ForecastingMethod? method = null);

    /// <summary>
    /// Train or update forecast model for a specific metric (async)
    /// </summary>
    Task TrainForecastModelAsync(string metricName, ForecastingMethod? method = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forecast future values for a metric
    /// </summary>
    MetricForecastResult? Forecast(string metricName, int horizon = 12);

    /// <summary>
    /// Forecast future values for a metric (async)
    /// </summary>
    Task<MetricForecastResult?> ForecastAsync(string metricName, int horizon = 12, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the forecasting method used for a specific metric
    /// </summary>
    ForecastingMethod GetForecastingMethod(string metricName);

    /// <summary>
    /// Set the forecasting method for a specific metric
    /// </summary>
    void SetForecastingMethod(string metricName, ForecastingMethod method);
}