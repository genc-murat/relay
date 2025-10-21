namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Configuration options for forecasting service
/// </summary>
public class ForecastingConfiguration
{
    /// <summary>
    /// Default forecast horizon in hours
    /// </summary>
    public int DefaultForecastHorizon { get; set; } = 24;

    /// <summary>
    /// Default forecasting method
    /// </summary>
    public ForecastingMethod DefaultForecastingMethod { get; set; } = ForecastingMethod.SSA;

    /// <summary>
    /// Minimum data points required for training
    /// </summary>
    public int MinimumDataPoints { get; set; } = 10;

    /// <summary>
    /// Historical data window for training (in days)
    /// </summary>
    public int TrainingDataWindowDays { get; set; } = 7;

    /// <summary>
    /// Whether to auto-train models when forecasting without existing model
    /// </summary>
    public bool AutoTrainOnForecast { get; set; } = true;

    /// <summary>
    /// ML.NET random seed for reproducible results
    /// </summary>
    public int MlContextSeed { get; set; } = 42;
}