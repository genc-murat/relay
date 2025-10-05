using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Statistics for data cleanup operations.
    /// </summary>
    internal class DataCleanupStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CutoffTime { get; set; }

        public int RequestAnalyticsRemoved { get; set; }
        public int CachingAnalyticsRemoved { get; set; }
        public int PredictionResultsRemoved { get; set; }
        public int ExecutionTimesRemoved { get; set; }
        public int OptimizationResultsRemoved { get; set; }
        public int InternalDataItemsRemoved { get; set; }
        public int CachingDataItemsRemoved { get; set; }

        public long EstimatedMemoryFreed { get; set; }

        public int TotalItemsRemoved =>
            RequestAnalyticsRemoved +
            CachingAnalyticsRemoved +
            PredictionResultsRemoved +
            ExecutionTimesRemoved +
            OptimizationResultsRemoved +
            InternalDataItemsRemoved +
            CachingDataItemsRemoved;
    }

    /// <summary>
    /// Supporting analysis classes for advanced pattern recognition
    /// </summary>
    internal class PatternAnalysisContext
    {
        public Type RequestType { get; set; } = null!;
        public RequestAnalysisData AnalysisData { get; set; } = null!;
        public RequestExecutionMetrics CurrentMetrics { get; set; } = null!;
        public SystemLoadMetrics SystemLoad { get; set; } = null!;
        public double HistoricalTrend { get; set; }
    }

    /// <summary>
    /// Performance analysis result
    /// </summary>
    internal class PerformanceAnalysisResult
    {
        public bool ShouldOptimize { get; set; }
        public OptimizationStrategy RecommendedStrategy { get; set; }
        public double Confidence { get; set; }
        public TimeSpan EstimatedImprovement { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public OptimizationPriority Priority { get; set; }
        public RiskLevel Risk { get; set; }
        public double GainPercentage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Caching analysis result
    /// </summary>
    internal class CachingAnalysisResult
    {
        public bool ShouldCache { get; set; }
        public double ExpectedHitRate { get; set; }
        public double ExpectedImprovement { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public CacheStrategy RecommendedStrategy { get; set; }
        public TimeSpan RecommendedTTL { get; set; }
    }

    /// <summary>
    /// Resource optimization result
    /// </summary>
    internal class ResourceOptimizationResult
    {
        public bool ShouldOptimize { get; set; }
        public OptimizationStrategy Strategy { get; set; }
        public double Confidence { get; set; }
        public TimeSpan EstimatedImprovement { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public OptimizationPriority Priority { get; set; }
        public RiskLevel Risk { get; set; }
        public double GainPercentage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Machine learning enhancement result
    /// </summary>
    internal class MachineLearningEnhancement
    {
        public OptimizationStrategy AlternativeStrategy { get; set; }
        public double EnhancedConfidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }

    /// <summary>
    /// Risk assessment result
    /// </summary>
    internal class RiskAssessmentResult
    {
        public RiskLevel RiskLevel { get; set; }
        public double AdjustedConfidence { get; set; }
    }

    /// <summary>
    /// Pattern analysis result
    /// </summary>
    internal class PatternAnalysisResult
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
