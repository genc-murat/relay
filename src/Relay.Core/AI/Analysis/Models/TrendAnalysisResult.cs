using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    public class TrendAnalysisResult
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, MovingAverageData> MovingAverages { get; set; } = new();
        public Dictionary<string, TrendDirection> TrendDirections { get; set; } = new();
        public Dictionary<string, double> TrendVelocities { get; set; } = new();
        public Dictionary<string, SeasonalityPattern> SeasonalityPatterns { get; set; } = new();
        public Dictionary<string, RegressionResult> RegressionResults { get; set; } = new();
        public Dictionary<string, List<string>> Correlations { get; set; } = new();
        public List<MetricAnomaly> Anomalies { get; set; } = new();
    }
}
