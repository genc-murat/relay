using System;

namespace Relay.Core.AI
{

    /// <summary>
    /// Pattern analysis result
    /// </summary>
    public class PatternAnalysisResult
    {
        public int TotalPredictions { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
        public PredictionResult[] SuccessfulPredictions { get; set; } = Array.Empty<PredictionResult>();
        public PredictionResult[] FailedPredictions { get; set; } = Array.Empty<PredictionResult>();
        public double OverallAccuracy { get; set; }
        public double SuccessRate { get; set; }
        public double FailureRate { get; set; }
        public int HighImpactSuccesses { get; set; }
        public int MediumImpactSuccesses { get; set; }
        public int LowImpactSuccesses { get; set; }
        public double AverageImprovement { get; set; }
        public Type[] BestRequestTypes { get; set; } = Array.Empty<Type>();
        public Type[] WorstRequestTypes { get; set; } = Array.Empty<Type>();
        public int PatternsUpdated { get; set; }
    }
}
