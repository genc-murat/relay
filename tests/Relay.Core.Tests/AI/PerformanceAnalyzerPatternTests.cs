using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class PerformanceAnalyzerPatternTests
    {
        private readonly ILogger<PerformanceAnalyzer> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly PerformanceAnalyzer _analyzer;

        public PerformanceAnalyzerPatternTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<PerformanceAnalyzer>();
            _options = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = true,
                MinConfidenceScore = 0.7,
                MinExecutionsForAnalysis = 5
            };
            _analyzer = new PerformanceAnalyzer(_logger, _options);
        }

        #region AnalyzePerformancePatterns Tests

        [Fact]
        public void AnalyzePerformancePatterns_Should_Recommend_ParallelProcessing_For_High_Variance()
        {
            // Arrange
            var context = CreateContext(
                avgExecutionTime: 1200,
                variance: 0.4,
                errorRate: 0.01,
                trend: 0.1,
                cpuUtilization: 0.5
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.ParallelProcessing, result.RecommendedStrategy);
            Assert.Equal(0.85, result.Confidence);
            Assert.Equal(OptimizationPriority.High, result.Priority);
            Assert.Equal(RiskLevel.Low, result.Risk);
            Assert.Equal(0.4, result.GainPercentage);
            Assert.Contains("parallel processing", result.Reasoning, StringComparison.OrdinalIgnoreCase);
            Assert.True(result.EstimatedImprovement.TotalMilliseconds > 0);
        }

        [Fact]
        public void AnalyzePerformancePatterns_Should_Recommend_BatchProcessing_For_Increasing_Trend()
        {
            // Arrange
            var context = CreateContext(
                avgExecutionTime: 600,
                variance: 0.1,
                errorRate: 0.01,
                trend: 0.25,
                cpuUtilization: 0.5
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.BatchProcessing, result.RecommendedStrategy);
            Assert.Equal(0.75, result.Confidence);
            Assert.Equal(OptimizationPriority.Medium, result.Priority);
            Assert.Equal(RiskLevel.Medium, result.Risk);
            Assert.Equal(0.3, result.GainPercentage);
            Assert.Contains("batching", result.Reasoning, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AnalyzePerformancePatterns_Should_Recommend_MemoryPooling_For_High_CPU()
        {
            // Arrange
            var context = CreateContext(
                avgExecutionTime: 300,
                variance: 0.1,
                errorRate: 0.01,
                trend: 0.1,
                cpuUtilization: 0.85
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.MemoryPooling, result.RecommendedStrategy);
            Assert.Equal(0.70, result.Confidence);
            Assert.Equal(OptimizationPriority.Medium, result.Priority);
            Assert.Equal(RiskLevel.Low, result.Risk);
            Assert.Equal(0.2, result.GainPercentage);
            Assert.Contains("memory", result.Reasoning, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AnalyzePerformancePatterns_Should_Recommend_CircuitBreaker_For_High_ErrorRate()
        {
            // Arrange
            var context = CreateContext(
                avgExecutionTime: 300,
                variance: 0.1,
                errorRate: 0.08,
                trend: 0.1,
                cpuUtilization: 0.5
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.CircuitBreaker, result.RecommendedStrategy);
            Assert.Equal(0.90, result.Confidence);
            Assert.Equal(OptimizationPriority.Critical, result.Priority);
            Assert.Equal(RiskLevel.VeryLow, result.Risk);
            Assert.Equal(0.5, result.GainPercentage);
            Assert.Contains("circuit breaker", result.Reasoning, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AnalyzePerformancePatterns_Should_Not_Optimize_When_Metrics_Are_Good()
        {
            // Arrange
            var context = CreateContext(
                avgExecutionTime: 100,
                variance: 0.1,
                errorRate: 0.01,
                trend: 0.05,
                cpuUtilization: 0.4
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.False(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.None, result.RecommendedStrategy);
            Assert.Equal(0.95, result.Confidence);
            Assert.Equal(OptimizationPriority.Low, result.Priority);
            Assert.Equal(RiskLevel.VeryLow, result.Risk);
            Assert.Contains("acceptable", result.Reasoning, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AnalyzePerformancePatterns_Should_Prioritize_ErrorRate_Over_Performance()
        {
            // Arrange - High error rate with otherwise good metrics
            var context = CreateContext(
                avgExecutionTime: 50,
                variance: 0.05,
                errorRate: 0.10,
                trend: 0.0,
                cpuUtilization: 0.3
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.CircuitBreaker, result.RecommendedStrategy);
            Assert.Equal(OptimizationPriority.Critical, result.Priority);
        }

        [Fact]
        public void AnalyzePerformancePatterns_Should_Have_Parameters_Dictionary()
        {
            // Arrange
            var context = CreateContext(
                avgExecutionTime: 1200,
                variance: 0.4,
                errorRate: 0.01,
                trend: 0.1,
                cpuUtilization: 0.5
            );

            // Act
            var result = _analyzer.AnalyzePerformancePatterns(context);

            // Assert
            Assert.NotNull(result.Parameters);
        }

        #endregion

        #region Helper Methods

        private PatternAnalysisContext CreateContext(
            double avgExecutionTime,
            double variance,
            double errorRate,
            double trend,
            double cpuUtilization)
        {
            var analysisData = new RequestAnalysisData();

            // Add metrics to set average execution time
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(avgExecutionTime),
                TotalExecutions = 100,
                SuccessfulExecutions = (long)(100 * (1 - errorRate)),
                FailedExecutions = (long)(100 * errorRate)
            };
            analysisData.AddMetrics(metrics);

            // Simulate variance by adding varied execution times
            if (variance > 0.1)
            {
                for (int i = 0; i < 10; i++)
                {
                    var variedTime = avgExecutionTime * (1 + (i % 2 == 0 ? variance : -variance));
                    var variedMetrics = new RequestExecutionMetrics
                    {
                        AverageExecutionTime = TimeSpan.FromMilliseconds(variedTime),
                        TotalExecutions = 1,
                        SuccessfulExecutions = 1,
                        FailedExecutions = 0
                    };
                    analysisData.AddMetrics(variedMetrics);
                }
            }

            return new PatternAnalysisContext
            {
                RequestType = typeof(TestRequest),
                AnalysisData = analysisData,
                CurrentMetrics = metrics,
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = cpuUtilization,
                    MemoryUtilization = 0.5,
                    ActiveRequestCount = 10
                },
                HistoricalTrend = trend
            };
        }

        #endregion

        #region Test Types

        private class TestRequest { }

        #endregion
    }
}