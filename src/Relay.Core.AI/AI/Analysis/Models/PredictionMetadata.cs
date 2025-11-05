using System;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Metadata about a prediction operation
    /// </summary>
    public class PredictionMetadata
    {
        /// <summary>
        /// Name of the metric
        /// </summary>
        public string MetricName { get; set; } = string.Empty;

        /// <summary>
        /// Forecasting method used
        /// </summary>
        public ForecastingMethod? Method { get; set; }

        /// <summary>
        /// When the model was last trained
        /// </summary>
        public DateTime? LastTrainedAt { get; set; }

        /// <summary>
        /// Number of data points used for training
        /// </summary>
        public int TrainingDataPoints { get; set; }

        /// <summary>
        /// Whether auto-training was performed
        /// </summary>
        public bool AutoTrained { get; set; }

        /// <summary>
        /// Prediction horizon requested
        /// </summary>
        public int Horizon { get; set; }

        /// <summary>
        /// When the prediction was made
        /// </summary>
        public DateTime PredictedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the prediction was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if prediction failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}