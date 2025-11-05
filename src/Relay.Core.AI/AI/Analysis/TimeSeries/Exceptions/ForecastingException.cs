using System;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Exception thrown when forecasting fails.
/// </summary>
public class ForecastingException : TimeSeriesException
{
    public ForecastingException(string message) : base(message) { }
    public ForecastingException(string message, Exception innerException) : base(message, innerException) { }
}