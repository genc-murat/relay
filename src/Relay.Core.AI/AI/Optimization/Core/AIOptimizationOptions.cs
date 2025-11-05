using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Configuration options for the AI optimization engine.
    /// </summary>
    public sealed class AIOptimizationOptions
    {
        /// <summary>
        /// Gets or sets whether AI optimization is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether learning mode is enabled.
        /// AI will continuously learn from execution results.
        /// </summary>
        public bool LearningEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for model updates.
        /// </summary>
        public TimeSpan ModelUpdateInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the default batch size for batch processing optimization.
        /// </summary>
        public int DefaultBatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum batch size allowed.
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum cache TTL for caching recommendations.
        /// </summary>
        public TimeSpan MinCacheTtl { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum cache TTL for caching recommendations.
        /// </summary>
        public TimeSpan MaxCacheTtl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the minimum confidence score required for applying optimizations.
        /// </summary>
        public double MinConfidenceScore { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the minimum cache hit rate required for enabling caching.
        /// </summary>
        public double MinCacheHitRate { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the minimum number of executions required before AI analysis.
        /// </summary>
        public int MinExecutionsForAnalysis { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of recent predictions to keep for accuracy calculation.
        /// </summary>
        public int MaxRecentPredictions { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the threshold for high execution time (in milliseconds).
        /// Requests exceeding this will be flagged for optimization.
        /// </summary>
        public double HighExecutionTimeThreshold { get; set; } = 500.0;

        /// <summary>
        /// Gets or sets the threshold for high error rate.
        /// Error rates exceeding this will trigger reliability optimizations.
        /// </summary>
        public double HighErrorRateThreshold { get; set; } = 0.05; // 5%

        /// <summary>
        /// Gets or sets the threshold for high memory allocation (in bytes).
        /// Memory allocations exceeding this will trigger memory optimizations.
        /// </summary>
        public long HighMemoryAllocationThreshold { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// Gets or sets the threshold for high concurrency.
        /// Concurrent executions exceeding this will trigger scaling optimizations.
        /// </summary>
        public int HighConcurrencyThreshold { get; set; } = 50;

        /// <summary>
        /// Gets or sets the model training date.
        /// </summary>
        public DateTime ModelTrainingDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the model version identifier.
        /// </summary>
        public string ModelVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the last retraining date.
        /// </summary>
        public DateTime LastRetrainingDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether to enable predictive analysis.
        /// </summary>
        public bool EnablePredictiveAnalysis { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable system health monitoring.
        /// </summary>
        public bool EnableHealthMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable performance bottleneck detection.
        /// </summary>
        public bool EnableBottleneckDetection { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable optimization opportunity identification.
        /// </summary>
        public bool EnableOpportunityIdentification { get; set; } = true;

        /// <summary>
        /// Gets or sets the weight for performance metrics in AI decisions.
        /// </summary>
        public double PerformanceWeight { get; set; } = 0.4;

        /// <summary>
        /// Gets or sets the weight for reliability metrics in AI decisions.
        /// </summary>
        public double ReliabilityWeight { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the weight for resource usage metrics in AI decisions.
        /// </summary>
        public double ResourceWeight { get; set; } = 0.2;

        /// <summary>
        /// Gets or sets the weight for user experience metrics in AI decisions.
        /// </summary>
        public double UserExperienceWeight { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets whether to enable automatic optimization application.
        /// When enabled, the AI will automatically apply low-risk optimizations.
        /// </summary>
        public bool EnableAutomaticOptimization { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum risk level for automatic optimization.
        /// Only optimizations with risk level at or below this will be applied automatically.
        /// </summary>
        public RiskLevel MaxAutomaticOptimizationRisk { get; set; } = RiskLevel.Low;

        /// <summary>
        /// Gets or sets whether to log AI decisions and reasoning.
        /// </summary>
        public bool EnableDecisionLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to export AI metrics for external analysis.
        /// </summary>
        public bool EnableMetricsExport { get; set; } = true;

        /// <summary>
        /// Gets or sets the metrics export interval.
        /// </summary>
        public TimeSpan MetricsExportInterval { get; set; } = TimeSpan.FromMinutes(15);

        // Connection monitoring configuration
        /// <summary>
        /// Gets or sets the maximum estimated HTTP connections for monitoring.
        /// Default: 200
        /// </summary>
        public int MaxEstimatedHttpConnections { get; set; } = 200;

        /// <summary>
        /// Gets or sets the maximum estimated database connections for monitoring.
        /// Default: 50
        /// </summary>
        public int MaxEstimatedDbConnections { get; set; } = 50;

        /// <summary>
        /// Gets or sets the estimated maximum database connections in the pool.
        /// Default: 100
        /// </summary>
        public int EstimatedMaxDbConnections { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to enable HTTP connection metrics collection via reflection.
        /// This may have performance implications and requires appropriate permissions.
        /// Default: true
        /// </summary>
        public bool EnableHttpConnectionReflection { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum retry attempts for reflection-based metrics collection.
        /// Default: 3
        /// </summary>
        public int HttpMetricsReflectionMaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the timeout for reflection-based metrics collection (in milliseconds).
        /// Default: 5000 (5 seconds)
        /// </summary>
        public int HttpMetricsReflectionTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the maximum estimated external service connections.
        /// Default: 30
        /// </summary>
        public int MaxEstimatedExternalConnections { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum estimated WebSocket connections.  
        /// Default: 1000
        /// </summary>
        public int MaxEstimatedWebSocketConnections { get; set; } = 1000;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        public void Validate()
        {
            if (DefaultBatchSize <= 0)
                throw new ArgumentException("DefaultBatchSize must be greater than 0", nameof(DefaultBatchSize));

            if (MaxBatchSize <= 0)
                throw new ArgumentException("MaxBatchSize must be greater than 0", nameof(MaxBatchSize));

            if (DefaultBatchSize > MaxBatchSize)
                throw new ArgumentException("DefaultBatchSize cannot be greater than MaxBatchSize");

            if (MinCacheTtl <= TimeSpan.Zero)
                throw new ArgumentException("MinCacheTtl must be greater than zero", nameof(MinCacheTtl));

            if (MaxCacheTtl <= TimeSpan.Zero)
                throw new ArgumentException("MaxCacheTtl must be greater than zero", nameof(MaxCacheTtl));

            if (MinCacheTtl > MaxCacheTtl)
                throw new ArgumentException("MinCacheTtl cannot be greater than MaxCacheTtl");

            if (MinConfidenceScore < 0.0 || MinConfidenceScore > 1.0)
                throw new ArgumentException("MinConfidenceScore must be between 0.0 and 1.0", nameof(MinConfidenceScore));

            if (MinExecutionsForAnalysis <= 0)
                throw new ArgumentException("MinExecutionsForAnalysis must be greater than 0", nameof(MinExecutionsForAnalysis));

            if (HighErrorRateThreshold < 0.0 || HighErrorRateThreshold > 1.0)
                throw new ArgumentException("HighErrorRateThreshold must be between 0.0 and 1.0", nameof(HighErrorRateThreshold));

            var totalWeight = PerformanceWeight + ReliabilityWeight + ResourceWeight + UserExperienceWeight;
            if (Math.Abs(totalWeight - 1.0) > 0.001)
                throw new ArgumentException("The sum of all weight properties must equal 1.0");

            if (ModelUpdateInterval <= TimeSpan.Zero)
                throw new ArgumentException("ModelUpdateInterval must be greater than zero", nameof(ModelUpdateInterval));

            if (MetricsExportInterval <= TimeSpan.Zero)
                throw new ArgumentException("MetricsExportInterval must be greater than zero", nameof(MetricsExportInterval));
        }
    }
}