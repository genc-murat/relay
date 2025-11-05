namespace Relay.Core.AI.Analysis.TimeSeries;

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
