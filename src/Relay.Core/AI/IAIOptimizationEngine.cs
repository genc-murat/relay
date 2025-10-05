using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    /// <summary>
    /// AI-powered optimization engine for request processing and performance enhancement.
    /// Uses machine learning to analyze patterns and optimize request handling strategies.
    /// </summary>
    public interface IAIOptimizationEngine
    {
        /// <summary>
        /// Analyzes request patterns and suggests optimizations.
        /// </summary>
        /// <typeparam name="TRequest">Type of the request</typeparam>
        /// <param name="request">The request to analyze</param>
        /// <param name="executionMetrics">Historical execution metrics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization recommendations</returns>
        ValueTask<OptimizationRecommendation> AnalyzeRequestAsync<TRequest>(
            TRequest request, 
            RequestExecutionMetrics executionMetrics,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Predicts optimal batch size for request processing.
        /// </summary>
        /// <param name="requestType">Type of requests</param>
        /// <param name="currentLoad">Current system load</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimal batch size</returns>
        ValueTask<int> PredictOptimalBatchSizeAsync(
            Type requestType, 
            SystemLoadMetrics currentLoad,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if caching would be beneficial for this request type.
        /// </summary>
        /// <param name="requestType">Type of request</param>
        /// <param name="accessPatterns">Historical access patterns</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Caching recommendation</returns>
        ValueTask<CachingRecommendation> ShouldCacheAsync(
            Type requestType,
            AccessPattern[] accessPatterns,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Learns from execution results to improve future recommendations.
        /// </summary>
        /// <param name="requestType">Type of request that was executed</param>
        /// <param name="appliedOptimizations">Optimizations that were applied</param>
        /// <param name="actualMetrics">Actual execution metrics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask LearnFromExecutionAsync(
            Type requestType,
            OptimizationStrategy[] appliedOptimizations,
            RequestExecutionMetrics actualMetrics,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets AI-powered performance insights for the entire system.
        /// </summary>
        /// <param name="timeWindow">Time window for analysis</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>System-wide performance insights</returns>
        ValueTask<SystemPerformanceInsights> GetSystemInsightsAsync(
            TimeSpan timeWindow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enables or disables AI learning mode.
        /// </summary>
        /// <param name="enabled">Whether to enable learning</param>
        void SetLearningMode(bool enabled);

        /// <summary>
        /// Gets current AI model performance statistics.
        /// </summary>
        AIModelStatistics GetModelStatistics();
    }
}