using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Data
{
    /// <summary>
    /// Load pattern data for analysis
    /// </summary>
    internal class LoadPatternData
    {
        public LoadLevel Level { get; set; }
        public List<PredictionResult> Predictions { get; set; } = new();
        public double SuccessRate { get; set; }
        public double AverageImprovement { get; set; }
        public int TotalPredictions { get; set; }
        public Dictionary<string, double> StrategyEffectiveness { get; set; } = new();
    }
}
