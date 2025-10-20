namespace Relay.Core.AI;

/// <summary>
/// Training phases
/// </summary>
public enum TrainingPhase
{
    /// <summary>
    /// Validating training data
    /// </summary>
    Validation,

    /// <summary>
    /// Training performance regression models
    /// </summary>
    PerformanceModels,

    /// <summary>
    /// Training optimization classifiers
    /// </summary>
    OptimizationClassifiers,

    /// <summary>
    /// Training anomaly detection models
    /// </summary>
    AnomalyDetection,

    /// <summary>
    /// Training forecasting models
    /// </summary>
    Forecasting,

    /// <summary>
    /// Calculating model statistics
    /// </summary>
    Statistics,

    /// <summary>
    /// Training completed
    /// </summary>
    Completed
}
