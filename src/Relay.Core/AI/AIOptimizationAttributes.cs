using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Marks a handler for AI optimization analysis and automatic performance enhancement.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AIOptimizedAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to enable automatic optimization application.
        /// </summary>
        public bool AutoApplyOptimizations { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum confidence score required for optimization.
        /// </summary>
        public double MinConfidenceScore { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the maximum risk level allowed for automatic optimization.
        /// </summary>
        public RiskLevel MaxRiskLevel { get; set; } = RiskLevel.Low;

        /// <summary>
        /// Gets or sets specific optimization strategies to consider.
        /// If empty, all strategies are considered.
        /// </summary>
        public OptimizationStrategy[] AllowedStrategies { get; set; } = Array.Empty<OptimizationStrategy>();

        /// <summary>
        /// Gets or sets optimization strategies to exclude.
        /// </summary>
        public OptimizationStrategy[] ExcludedStrategies { get; set; } = Array.Empty<OptimizationStrategy>();

        /// <summary>
        /// Gets or sets whether to track performance metrics for this handler.
        /// </summary>
        public bool EnableMetricsTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable learning from execution results.
        /// </summary>
        public bool EnableLearning { get; set; } = true;

        /// <summary>
        /// Gets or sets the priority level for AI analysis.
        /// </summary>
        public OptimizationPriority Priority { get; set; } = OptimizationPriority.Medium;
    }

    /// <summary>
    /// Enables intelligent request batching for high-frequency operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SmartBatchingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the batching algorithm to use.
        /// </summary>
        public string Algorithm { get; set; } = "AI-Predictive";

        /// <summary>
        /// Gets or sets the minimum batch size.
        /// </summary>
        public int MinBatchSize { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum batch size.
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum wait time for batch completion.
        /// </summary>
        public int MaxWaitTimeMilliseconds { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to use AI-predicted optimal batch sizes.
        /// </summary>
        public bool UseAIPrediction { get; set; } = true;

        /// <summary>
        /// Gets or sets the batching strategy.
        /// </summary>
        public BatchingStrategy Strategy { get; set; } = BatchingStrategy.Dynamic;
    }

    /// <summary>
    /// Available batching strategies.
    /// </summary>
    public enum BatchingStrategy
    {
        /// <summary>Fixed batch size</summary>
        Fixed,
        
        /// <summary>Dynamic batch size based on system load</summary>
        Dynamic,
        
        /// <summary>AI-predicted optimal batch size</summary>
        AIPredictive,
        
        /// <summary>Time-based batching</summary>
        TimeBased,
        
        /// <summary>Adaptive batching based on throughput</summary>
        Adaptive
    }

    /// <summary>
    /// Provides performance hints to the AI optimization engine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PerformanceHintAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the PerformanceHintAttribute.
        /// </summary>
        /// <param name="hint">The performance hint message</param>
        public PerformanceHintAttribute(string hint)
        {
            Hint = hint ?? throw new ArgumentNullException(nameof(hint));
        }

        /// <summary>
        /// Gets the performance hint message.
        /// </summary>
        public string Hint { get; }

        /// <summary>
        /// Gets or sets the hint category.
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Gets or sets the priority of this hint.
        /// </summary>
        public OptimizationPriority Priority { get; set; } = OptimizationPriority.Medium;

        /// <summary>
        /// Gets or sets whether this is a suggestion or requirement.
        /// </summary>
        public bool IsRequired { get; set; } = false;
    }

    /// <summary>
    /// Configures AI-powered caching recommendations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IntelligentCachingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to enable AI-based cache analysis.
        /// </summary>
        public bool EnableAIAnalysis { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum access frequency for caching consideration.
        /// </summary>
        public int MinAccessFrequency { get; set; } = 5;

        /// <summary>
        /// Gets or sets the minimum cache hit rate prediction for enabling caching.
        /// </summary>
        public double MinPredictedHitRate { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the cache scope for AI recommendations.
        /// </summary>
        public CacheScope PreferredScope { get; set; } = CacheScope.Global;

        /// <summary>
        /// Gets or sets the preferred cache strategy.
        /// </summary>
        public CacheStrategy PreferredStrategy { get; set; } = CacheStrategy.Adaptive;

        /// <summary>
        /// Gets or sets whether to use dynamic TTL based on access patterns.
        /// </summary>
        public bool UseDynamicTtl { get; set; } = true;
    }

    /// <summary>
    /// Enables AI-powered memory optimization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SmartMemoryOptimizationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to enable object pooling.
        /// </summary>
        public bool EnableObjectPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use stack allocation where possible.
        /// </summary>
        public bool PreferStackAllocation { get; set; } = true;

        /// <summary>
        /// Gets or sets the memory threshold (in bytes) for optimization consideration.
        /// </summary>
        public long MemoryThreshold { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Gets or sets whether to enable buffer pooling.
        /// </summary>
        public bool EnableBufferPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets the optimization aggressiveness level.
        /// </summary>
        public OptimizationAggressiveness Aggressiveness { get; set; } = OptimizationAggressiveness.Moderate;
    }

    /// <summary>
    /// Optimization aggressiveness levels.
    /// </summary>
    public enum OptimizationAggressiveness
    {
        Conservative,
        Moderate,
        Aggressive,
        Maximum
    }

    /// <summary>
    /// Enables AI-powered parallel processing optimization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SmartParallelizationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the minimum collection size for parallelization consideration.
        /// </summary>
        public int MinCollectionSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = -1; // Use system default

        /// <summary>
        /// Gets or sets whether to use AI-predicted optimal parallelism.
        /// </summary>
        public bool UseAIPrediction { get; set; } = true;

        /// <summary>
        /// Gets or sets the parallelization strategy.
        /// </summary>
        public ParallelizationStrategy Strategy { get; set; } = ParallelizationStrategy.Dynamic;

        /// <summary>
        /// Gets or sets whether to enable work stealing.
        /// </summary>
        public bool EnableWorkStealing { get; set; } = true;
    }

    /// <summary>
    /// Available parallelization strategies.
    /// </summary>
    public enum ParallelizationStrategy
    {
        None,
        Static,
        Dynamic,
        WorkStealing,
        AIPredictive
    }

    /// <summary>
    /// Configures AI-powered database query optimization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SmartDatabaseOptimizationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to enable query batching.
        /// </summary>
        public bool EnableQueryBatching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable connection pooling optimization.
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of database calls before optimization suggestion.
        /// </summary>
        public int MaxDatabaseCalls { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to enable read replica usage for read operations.
        /// </summary>
        public bool PreferReadReplicas { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable query result caching.
        /// </summary>
        public bool EnableQueryCaching { get; set; } = true;
    }

    /// <summary>
    /// Marks a request type for AI-powered monitoring and optimization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class AIMonitoredAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the monitoring level.
        /// </summary>
        public MonitoringLevel Level { get; set; } = MonitoringLevel.Standard;

        /// <summary>
        /// Gets or sets whether to collect detailed execution metrics.
        /// </summary>
        public bool CollectDetailedMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to track user access patterns.
        /// </summary>
        public bool TrackAccessPatterns { get; set; } = true;

        /// <summary>
        /// Gets or sets the sampling rate for metrics collection (0.0 to 1.0).
        /// </summary>
        public double SamplingRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets custom tags for categorizing this request type.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Monitoring levels for AI analysis.
    /// </summary>
    public enum MonitoringLevel
    {
        None,
        Basic,
        Standard,
        Detailed,
        Comprehensive
    }
}