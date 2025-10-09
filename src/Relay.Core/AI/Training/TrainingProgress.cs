using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents training progress information
    /// </summary>
    public class TrainingProgress
    {
        /// <summary>
        /// Current training phase
        /// </summary>
        public TrainingPhase Phase { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public double ProgressPercentage { get; set; }

        /// <summary>
        /// Current status message
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Number of samples processed
        /// </summary>
        public int SamplesProcessed { get; set; }

        /// <summary>
        /// Total number of samples
        /// </summary>
        public int TotalSamples { get; set; }

        /// <summary>
        /// Elapsed time since training started
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Current model metrics (if available)
        /// </summary>
        public ModelMetrics? CurrentMetrics { get; set; }
    }

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

    /// <summary>
    /// Model evaluation metrics
    /// </summary>
    public class ModelMetrics
    {
        /// <summary>
        /// R-Squared (for regression)
        /// </summary>
        public double? RSquared { get; set; }

        /// <summary>
        /// Mean Absolute Error (for regression)
        /// </summary>
        public double? MAE { get; set; }

        /// <summary>
        /// Root Mean Squared Error (for regression)
        /// </summary>
        public double? RMSE { get; set; }

        /// <summary>
        /// Accuracy (for classification)
        /// </summary>
        public double? Accuracy { get; set; }

        /// <summary>
        /// Area Under ROC Curve (for classification)
        /// </summary>
        public double? AUC { get; set; }

        /// <summary>
        /// F1 Score (for classification)
        /// </summary>
        public double? F1Score { get; set; }
    }

    /// <summary>
    /// Training progress callback delegate
    /// </summary>
    /// <param name="progress">Current training progress</param>
    public delegate void TrainingProgressCallback(TrainingProgress progress);
}
