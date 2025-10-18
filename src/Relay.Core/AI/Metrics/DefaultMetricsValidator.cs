using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Default implementation of metrics validator.
    /// </summary>
    internal class DefaultMetricsValidator : IMetricsValidator
    {
        public void Validate(AIModelStatistics statistics)
        {
            if (string.IsNullOrEmpty(statistics.ModelVersion))
                throw new ArgumentException("Model version cannot be null or empty", nameof(statistics.ModelVersion));

            if (statistics.TotalPredictions < 0)
                throw new ArgumentException("Total predictions cannot be negative", nameof(statistics.TotalPredictions));

            if (double.IsNaN(statistics.AccuracyScore) || statistics.AccuracyScore < 0 || statistics.AccuracyScore > 1)
                throw new ArgumentException("Accuracy score must be a valid number between 0 and 1", nameof(statistics.AccuracyScore));

            if (double.IsNaN(statistics.PrecisionScore) || statistics.PrecisionScore < 0 || statistics.PrecisionScore > 1)
                throw new ArgumentException("Precision score must be a valid number between 0 and 1", nameof(statistics.PrecisionScore));

            if (double.IsNaN(statistics.RecallScore) || statistics.RecallScore < 0 || statistics.RecallScore > 1)
                throw new ArgumentException("Recall score must be a valid number between 0 and 1", nameof(statistics.RecallScore));

            if (double.IsNaN(statistics.F1Score) || statistics.F1Score < 0 || statistics.F1Score > 1)
                throw new ArgumentException("F1 score must be a valid number between 0 and 1", nameof(statistics.F1Score));

            if (double.IsNaN(statistics.ModelConfidence) || statistics.ModelConfidence < 0 || statistics.ModelConfidence > 1)
                throw new ArgumentException("Model confidence must be a valid number between 0 and 1", nameof(statistics.ModelConfidence));

            if (statistics.AveragePredictionTime.TotalMilliseconds < 0)
                throw new ArgumentException("Average prediction time cannot be negative", nameof(statistics.AveragePredictionTime));

            if (statistics.TrainingDataPoints < 0)
                throw new ArgumentException("Training data points cannot be negative", nameof(statistics.TrainingDataPoints));
        }
    }
}