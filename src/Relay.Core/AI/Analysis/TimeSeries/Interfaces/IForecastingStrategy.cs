using System.Collections.Generic;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for forecasting strategy implementations
/// </summary>
public interface IForecastingStrategy
{
    /// <summary>
    /// The forecasting method this strategy implements
    /// </summary>
    ForecastingMethod Method { get; }

    /// <summary>
    /// Train a forecasting model for the given historical data
    /// </summary>
    ITransformer TrainModel(MLContext mlContext, List<MetricDataPoint> history, int horizon);
}