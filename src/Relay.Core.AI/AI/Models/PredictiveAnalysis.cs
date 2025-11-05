using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Predictive analysis results.
    /// </summary>
    public sealed class PredictiveAnalysis
    {
        public Dictionary<string, ForecastResult> NextHourPredictions { get; init; } = new();
        public Dictionary<string, ForecastResult> NextDayPredictions { get; init; } = new();
        public List<string> PotentialIssues { get; init; } = new();
        public List<string> ScalingRecommendations { get; init; } = new();

        /// <summary>
        /// Confidence level of predictions (0.0 to 1.0)
        /// </summary>
        public double PredictionConfidence { get; init; }
    }
}