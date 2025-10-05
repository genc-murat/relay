using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Model statistics for monitoring ML performance
    /// </summary>
    internal class ModelStatistics
    {
        public double AccuracyScore { get; set; }
        public double PrecisionScore { get; set; }
        public double RecallScore { get; set; }
        public double F1Score { get; set; }
        public double ModelConfidence { get; set; }
        public long TotalPredictions { get; set; }
        public long CorrectPredictions { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
