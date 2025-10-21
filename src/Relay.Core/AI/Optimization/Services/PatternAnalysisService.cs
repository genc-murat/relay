using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Service for analyzing request patterns and generating optimization recommendations
    /// </summary>
    internal class PatternAnalysisService
    {
        private readonly ILogger _logger;

        public PatternAnalysisService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<OptimizationRecommendation> AnalyzePatternsAsync(
            Type requestType,
            RequestAnalysisData analysisData,
            RequestExecutionMetrics executionMetrics,
            CancellationToken cancellationToken = default)
        {
            if (requestType == null) throw new ArgumentNullException(nameof(requestType));
            if (analysisData == null) throw new ArgumentNullException(nameof(analysisData));
            if (executionMetrics == null) throw new ArgumentNullException(nameof(executionMetrics));

            cancellationToken.ThrowIfCancellationRequested();

            // Analyze execution patterns
            var strategy = DetermineOptimalStrategy(analysisData, executionMetrics);
            var confidence = CalculateConfidenceScore(analysisData, executionMetrics);
            var estimatedImprovement = EstimateImprovement(analysisData, strategy);
            var reasoning = GenerateReasoning(analysisData, executionMetrics, strategy);
            var parameters = BuildParameters(strategy, analysisData);
            var priority = DeterminePriority(analysisData, executionMetrics);
            var estimatedGain = CalculateEstimatedGain(analysisData, strategy);
            var risk = AssessRisk(strategy, analysisData);

            var recommendation = new OptimizationRecommendation
            {
                Strategy = strategy,
                ConfidenceScore = confidence,
                EstimatedImprovement = estimatedImprovement,
                Reasoning = reasoning,
                Parameters = parameters,
                Priority = (Relay.Core.AI.OptimizationPriority)priority,
                EstimatedGainPercentage = estimatedGain,
                Risk = (Relay.Core.AI.RiskLevel)risk
            };

            _logger.LogDebug("Generated pattern analysis recommendation for {RequestType}: {Strategy} (Confidence: {Confidence:P})",
                requestType.Name, strategy, confidence);

            await Task.CompletedTask; // For async consistency
            return recommendation;
        }

        private OptimizationStrategy DetermineOptimalStrategy(RequestAnalysisData analysisData, RequestExecutionMetrics executionMetrics)
        {
            // High error rate -> Circuit breaker
            if (analysisData.ErrorRate > 0.1)
                return OptimizationStrategy.CircuitBreaker;

            // High concurrent executions -> Batching or parallel processing
            if (executionMetrics.ConcurrentExecutions > 10)
            {
                if (executionMetrics.AverageExecutionTime.TotalMilliseconds < 100)
                    return OptimizationStrategy.BatchProcessing;
                else
                    return OptimizationStrategy.ParallelProcessing;
            }

            // Long execution time -> Enable caching
            if (executionMetrics.AverageExecutionTime.TotalMilliseconds > 1000)
                return OptimizationStrategy.EnableCaching;

            // High memory usage -> Memory pooling
            if (executionMetrics.MemoryUsage > 100 * 1024 * 1024) // 100MB
                return OptimizationStrategy.MemoryPooling;

            // Many database calls -> Database optimization
            if (executionMetrics.DatabaseCalls > 5)
                return OptimizationStrategy.DatabaseOptimization;

            // Default to no optimization if patterns don't suggest specific strategies
            return OptimizationStrategy.None;
        }

        private double CalculateConfidenceScore(RequestAnalysisData analysisData, RequestExecutionMetrics executionMetrics)
        {
            var baseConfidence = 0.5;

            // Increase confidence with more data points
            if (analysisData.TotalExecutions > 100)
                baseConfidence += 0.2;
            else if (analysisData.TotalExecutions > 10)
                baseConfidence += 0.1;

            // Increase confidence for clear patterns
            if (analysisData.ErrorRate > 0.2) baseConfidence += 0.2;
            if (executionMetrics.ConcurrentExecutions > 20) baseConfidence += 0.15;
            if (executionMetrics.AverageExecutionTime.TotalMilliseconds > 2000) baseConfidence += 0.15;

            return Math.Min(baseConfidence, 0.95);
        }

        private TimeSpan EstimateImprovement(RequestAnalysisData analysisData, OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.EnableCaching => TimeSpan.FromMilliseconds(analysisData.AverageExecutionTime.TotalMilliseconds * 0.7),
                OptimizationStrategy.BatchProcessing => TimeSpan.FromMilliseconds(analysisData.AverageExecutionTime.TotalMilliseconds * 0.5),
                OptimizationStrategy.ParallelProcessing => TimeSpan.FromMilliseconds(analysisData.AverageExecutionTime.TotalMilliseconds * 0.6),
                OptimizationStrategy.MemoryPooling => TimeSpan.FromMilliseconds(50),
                OptimizationStrategy.DatabaseOptimization => TimeSpan.FromMilliseconds(analysisData.AverageExecutionTime.TotalMilliseconds * 0.3),
                OptimizationStrategy.CircuitBreaker => TimeSpan.FromMilliseconds(100),
                _ => TimeSpan.Zero
            };
        }

        private string GenerateReasoning(RequestAnalysisData analysisData, RequestExecutionMetrics executionMetrics, OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.EnableCaching =>
                    $"High repeat request rate ({analysisData.RepeatRequestRate:P}) and long execution time ({executionMetrics.AverageExecutionTime.TotalMilliseconds:F0}ms) suggest caching would be beneficial.",

                OptimizationStrategy.BatchProcessing =>
                    $"High concurrent executions ({executionMetrics.ConcurrentExecutions}) with fast execution time indicate batching could improve throughput.",

                OptimizationStrategy.ParallelProcessing =>
                    $"High concurrent executions ({executionMetrics.ConcurrentExecutions}) with longer execution time suggest parallel processing would help.",

                OptimizationStrategy.MemoryPooling =>
                    $"High memory usage ({executionMetrics.MemoryUsage / 1024 / 1024:F0}MB) indicates memory pooling could reduce allocation overhead.",

                OptimizationStrategy.DatabaseOptimization =>
                    $"Multiple database calls ({executionMetrics.DatabaseCalls}) per request suggest query optimization opportunities.",

                OptimizationStrategy.CircuitBreaker =>
                    $"High error rate ({analysisData.ErrorRate:P}) indicates circuit breaker protection is needed.",

                _ => "No specific optimization pattern detected."
            };
        }

        private Dictionary<string, object> BuildParameters(OptimizationStrategy strategy, RequestAnalysisData analysisData)
        {
            var parameters = new Dictionary<string, object>();

            switch (strategy)
            {
                case OptimizationStrategy.EnableCaching:
                    parameters["CacheDuration"] = TimeSpan.FromMinutes(5);
                    parameters["MaxCacheSize"] = 1000;
                    break;

                case OptimizationStrategy.BatchProcessing:
                    parameters["MaxBatchSize"] = Math.Min(50, analysisData.ConcurrentExecutionPeaks);
                    parameters["BatchTimeout"] = TimeSpan.FromMilliseconds(100);
                    break;

                case OptimizationStrategy.ParallelProcessing:
                    parameters["MaxDegreeOfParallelism"] = Environment.ProcessorCount / 2;
                    break;

                case OptimizationStrategy.MemoryPooling:
                    parameters["PoolSize"] = 1024 * 1024; // 1MB pool
                    break;
            }

            return parameters;
        }

        private Relay.Core.AI.OptimizationPriority DeterminePriority(RequestAnalysisData analysisData, RequestExecutionMetrics executionMetrics)
        {
            if (analysisData.ErrorRate > 0.2 || executionMetrics.AverageExecutionTime.TotalSeconds > 5)
                return OptimizationPriority.High;

            if (analysisData.TotalExecutions > 1000 || executionMetrics.ConcurrentExecutions > 50)
                return OptimizationPriority.Medium;

            return OptimizationPriority.Low;
        }

        private double CalculateEstimatedGain(RequestAnalysisData analysisData, OptimizationStrategy strategy)
        {
            return strategy switch
            {
                OptimizationStrategy.EnableCaching => 0.7,
                OptimizationStrategy.BatchProcessing => 0.5,
                OptimizationStrategy.ParallelProcessing => 0.6,
                OptimizationStrategy.MemoryPooling => 0.3,
                OptimizationStrategy.DatabaseOptimization => 0.4,
                OptimizationStrategy.CircuitBreaker => 0.2,
                _ => 0.0
            };
        }

        private Relay.Core.AI.RiskLevel AssessRisk(OptimizationStrategy strategy, RequestAnalysisData analysisData)
        {
            return strategy switch
            {
                OptimizationStrategy.EnableCaching => RiskLevel.Low,
                OptimizationStrategy.BatchProcessing => RiskLevel.Medium,
                OptimizationStrategy.ParallelProcessing => RiskLevel.Medium,
                OptimizationStrategy.MemoryPooling => RiskLevel.Low,
                OptimizationStrategy.DatabaseOptimization => RiskLevel.Medium,
                OptimizationStrategy.CircuitBreaker => RiskLevel.Low,
                _ => Relay.Core.AI.RiskLevel.VeryLow
            };
        }
    }
}