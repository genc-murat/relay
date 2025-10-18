using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for analyzing performance metric trends and detecting patterns
    /// </summary>
    public interface ITrendAnalyzer
    {
        /// <summary>
        /// Analyzes metric trends and returns comprehensive analysis results
        /// </summary>
        TrendAnalysisResult AnalyzeMetricTrends(Dictionary<string, double> currentMetrics);

        /// <summary>
        /// Calculates moving averages for the given metrics
        /// </summary>
        Dictionary<string, MovingAverageData> CalculateMovingAverages(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp);

        /// <summary>
        /// Detects performance anomalies in the metrics
        /// </summary>
        List<MetricAnomaly> DetectPerformanceAnomalies(
            Dictionary<string, double> currentMetrics,
            Dictionary<string, MovingAverageData> movingAverages);
    }
}