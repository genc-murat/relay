using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents optimization recommendations from the AI engine.
    /// </summary>
    public sealed class OptimizationRecommendation
    {
        public OptimizationStrategy Strategy { get; init; }
        public double ConfidenceScore { get; init; }
        public TimeSpan EstimatedImprovement { get; init; }
        public string Reasoning { get; init; } = string.Empty;
        public Dictionary<string, object> Parameters { get; init; } = new();
        public OptimizationPriority Priority { get; init; }
        
        /// <summary>
        /// Estimated performance gain percentage (0.0 to 1.0)
        /// </summary>
        public double EstimatedGainPercentage { get; init; }
        
        /// <summary>
        /// Risk level of applying this optimization
        /// </summary>
        public RiskLevel Risk { get; init; }
    }

    /// <summary>
    /// Available optimization strategies.
    /// </summary>
    public enum OptimizationStrategy
    {
        /// <summary>No optimization needed</summary>
        None,
        
        /// <summary>Enable caching for this request type</summary>
        EnableCaching,
        
        /// <summary>Batch multiple requests together</summary>
        BatchProcessing,
        
        /// <summary>Use async enumerable for streaming</summary>
        StreamingOptimization,
        
        /// <summary>Apply memory pooling</summary>
        MemoryPooling,
        
        /// <summary>Use parallel processing</summary>
        ParallelProcessing,
        
        /// <summary>Apply circuit breaker pattern</summary>
        CircuitBreaker,
        
        /// <summary>Optimize database queries</summary>
        DatabaseOptimization,
        
        /// <summary>Use SIMD acceleration</summary>
        SIMDAcceleration,
        
        /// <summary>Apply custom optimization</summary>
        Custom
    }

    /// <summary>
    /// Priority levels for optimizations.
    /// </summary>
    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Risk levels for optimization strategies.
    /// </summary>
    public enum RiskLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Metrics for request execution.
    /// </summary>
    public sealed class RequestExecutionMetrics
    {
        public TimeSpan AverageExecutionTime { get; init; }
        public TimeSpan MedianExecutionTime { get; init; }
        public TimeSpan P95ExecutionTime { get; init; }
        public TimeSpan P99ExecutionTime { get; init; }
        public long TotalExecutions { get; init; }
        public long SuccessfulExecutions { get; init; }
        public long FailedExecutions { get; init; }
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;
        public long MemoryAllocated { get; init; }
        public int ConcurrentExecutions { get; init; }
        public DateTime LastExecution { get; init; }
        public TimeSpan SamplePeriod { get; init; }
        
        /// <summary>
        /// CPU usage during request execution (0.0 to 1.0)
        /// </summary>
        public double CpuUsage { get; init; }
        
        /// <summary>
        /// Memory usage in bytes
        /// </summary>
        public long MemoryUsage { get; init; }
        
        /// <summary>
        /// Number of database calls made
        /// </summary>
        public int DatabaseCalls { get; init; }
        
        /// <summary>
        /// Number of external API calls made
        /// </summary>
        public int ExternalApiCalls { get; init; }
    }

    /// <summary>
    /// System load metrics for optimization decisions.
    /// </summary>
    public sealed class SystemLoadMetrics
    {
        public double CpuUtilization { get; init; }
        public double MemoryUtilization { get; init; }
        public long AvailableMemory { get; init; }
        public int ActiveRequestCount { get; init; }
        public int QueuedRequestCount { get; init; }
        public double ThroughputPerSecond { get; init; }
        public TimeSpan AverageResponseTime { get; init; }
        public double ErrorRate { get; init; }
        public DateTime Timestamp { get; init; }
        
        /// <summary>
        /// Number of active connections
        /// </summary>
        public int ActiveConnections { get; init; }
        
        /// <summary>
        /// Database connection pool utilization
        /// </summary>
        public double DatabasePoolUtilization { get; init; }
        
        /// <summary>
        /// Thread pool utilization
        /// </summary>
        public double ThreadPoolUtilization { get; init; }
    }

    /// <summary>
    /// Represents access patterns for caching recommendations.
    /// </summary>
    public sealed class AccessPattern
    {
        public DateTime Timestamp { get; init; }
        public string RequestKey { get; init; } = string.Empty;
        public int AccessCount { get; init; }
        public TimeSpan TimeSinceLastAccess { get; init; }
        public bool WasCacheHit { get; init; }
        public TimeSpan ExecutionTime { get; init; }
        
        /// <summary>
        /// Geographic region of the request
        /// </summary>
        public string Region { get; init; } = string.Empty;
        
        /// <summary>
        /// User context for personalized caching
        /// </summary>
        public string UserContext { get; init; } = string.Empty;
    }

    /// <summary>
    /// Caching recommendations from AI analysis.
    /// </summary>
    public sealed class CachingRecommendation
    {
        public bool ShouldCache { get; init; }
        public TimeSpan RecommendedTtl { get; init; }
        public CacheStrategy Strategy { get; init; }
        public double ExpectedHitRate { get; init; }
        public string CacheKey { get; init; } = string.Empty;
        public CacheScope Scope { get; init; }
        public double ConfidenceScore { get; init; }
        
        /// <summary>
        /// Estimated memory savings from caching
        /// </summary>
        public long EstimatedMemorySavings { get; init; }
        
        /// <summary>
        /// Estimated performance improvement
        /// </summary>
        public TimeSpan EstimatedPerformanceGain { get; init; }
    }

    /// <summary>
    /// Available caching strategies.
    /// </summary>
    public enum CacheStrategy
    {
        None,
        LRU,
        LFU,
        TimeBasedExpiration,
        SlidingExpiration,
        Adaptive,
        Distributed
    }

    /// <summary>
    /// Cache scope definitions.
    /// </summary>
    public enum CacheScope
    {
        Global,
        User,
        Session,
        Request,
        Regional
    }

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
    }

    /// <summary>
    /// Represents a performance bottleneck identified by AI.
    /// </summary>
    public sealed class PerformanceBottleneck
    {
        public string Component { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public BottleneckSeverity Severity { get; init; }
        public double Impact { get; init; }
        public List<string> RecommendedActions { get; init; } = new();
        public TimeSpan EstimatedResolutionTime { get; init; }
    }

    /// <summary>
    /// Severity levels for performance bottlenecks.
    /// </summary>
    public enum BottleneckSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Represents an optimization opportunity.
    /// </summary>
    public sealed class OptimizationOpportunity
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public double ExpectedImprovement { get; init; }
        public TimeSpan ImplementationEffort { get; init; }
        public OptimizationPriority Priority { get; init; }
        public List<string> Steps { get; init; } = new();
    }

    /// <summary>
    /// System health scoring.
    /// </summary>
    public sealed class SystemHealthScore
    {
        public double Overall { get; init; }
        public double Performance { get; init; }
        public double Reliability { get; init; }
        public double Scalability { get; init; }
        public double Security { get; init; }
        public double Maintainability { get; init; }
        
        /// <summary>
        /// Health status description
        /// </summary>
        public string Status { get; init; } = string.Empty;
        
        /// <summary>
        /// Areas needing immediate attention
        /// </summary>
        public List<string> CriticalAreas { get; init; } = new();
    }

    /// <summary>
    /// Predictive analysis results.
    /// </summary>
    public sealed class PredictiveAnalysis
    {
        public Dictionary<string, double> NextHourPredictions { get; init; } = new();
        public Dictionary<string, double> NextDayPredictions { get; init; } = new();
        public List<string> PotentialIssues { get; init; } = new();
        public List<string> ScalingRecommendations { get; init; } = new();
        
        /// <summary>
        /// Confidence level of predictions (0.0 to 1.0)
        /// </summary>
        public double PredictionConfidence { get; init; }
    }

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