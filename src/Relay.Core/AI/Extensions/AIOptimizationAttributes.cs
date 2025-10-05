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
}