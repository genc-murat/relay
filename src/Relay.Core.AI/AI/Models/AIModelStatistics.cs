using System;

namespace Relay.Core.AI
{

    /// <summary>
    /// AI model performance statistics.
    /// </summary>
    public sealed class AIModelStatistics
    {
        public DateTime ModelTrainingDate { get; init; }
        public long TotalPredictions { get; init; }
        public double AccuracyScore { get; init; }
        public double PrecisionScore { get; init; }
        public double RecallScore { get; init; }
        public double F1Score { get; init; }
        public TimeSpan AveragePredictionTime { get; init; }
        public long TrainingDataPoints { get; init; }
        
        /// <summary>
        /// Model version identifier
        /// </summary>
        public string ModelVersion { get; init; } = string.Empty;
        
        /// <summary>
        /// Last retraining date
        /// </summary>
        public DateTime LastRetraining { get; init; }
        
        /// <summary>
        /// Model confidence level
        /// </summary>
        public double ModelConfidence { get; init; }
    }
}