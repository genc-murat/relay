using System;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Exception thrown when model training fails.
/// </summary>
public class ModelTrainingException : TimeSeriesException
{
    public ModelTrainingException(string message) : base(message) { }
    public ModelTrainingException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Gets the forecasting method that was being used.
    /// </summary>
    public ForecastingMethod? ForecastingMethod { get; set; }
}