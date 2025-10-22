using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// System-wide performance insights from AI analysis.
    /// </summary>
    public sealed class SystemPerformanceInsights
    {
        public DateTime AnalysisTime { get; init; }
        public TimeSpan AnalysisPeriod { get; init; }
        public List<PerformanceBottleneck> Bottlenecks { get; init; } = new();
        public List<OptimizationOpportunity> Opportunities { get; init; } = new();
        public SystemHealthScore HealthScore { get; init; } = new();
        public PredictiveAnalysis Predictions { get; init; } = new();
        
        /// <summary>
        /// Overall system performance grade (A-F)
        /// </summary>
        public char PerformanceGrade { get; init; }
        
        /// <summary>
        /// Key performance indicators
        /// </summary>
        public Dictionary<string, double> KeyMetrics { get; init; } = new();

        /// <summary>
        /// Detected seasonal patterns in system metrics
        /// </summary>
        public List<Optimization.Data.SeasonalPattern> SeasonalPatterns { get; init; } = new();

        /// <summary>
        /// Resource optimization recommendations
        /// </summary>
        public Optimization.Services.ResourceOptimizationRecommendation ResourceOptimization { get; init; } = new();
    }
}