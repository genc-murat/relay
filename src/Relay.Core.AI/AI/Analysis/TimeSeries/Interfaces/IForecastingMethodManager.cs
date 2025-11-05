using System.Collections.Generic;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for managing forecasting methods and strategies
/// </summary>
public interface IForecastingMethodManager
{
    /// <summary>
    /// Gets the forecasting method for a metric
    /// </summary>
    ForecastingMethod GetForecastingMethod(string metricName);

    /// <summary>
    /// Sets the forecasting method for a metric
    /// </summary>
    void SetForecastingMethod(string metricName, ForecastingMethod method);

    /// <summary>
    /// Gets all available forecasting strategies
    /// </summary>
    IEnumerable<IForecastingStrategy> GetAvailableStrategies();

    /// <summary>
    /// Gets a specific forecasting strategy
    /// </summary>
    IForecastingStrategy? GetStrategy(ForecastingMethod method);
}