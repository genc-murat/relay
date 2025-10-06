using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class PerformanceAnalyzerTests
    {
        private readonly ILogger<PerformanceAnalyzer> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly PerformanceAnalyzer _analyzer;

        public PerformanceAnalyzerTests()
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

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PerformanceAnalyzer(null!, _options));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PerformanceAnalyzer(_logger, null!));
        }

        #endregion

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

        #region IdentifyBottlenecks Tests

        [Fact]
        public void IdentifyBottlenecks_Should_Detect_Slow_Execution_Time()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(SlowRequest)] = CreateAnalysisData(avgExecutionMs: 1500, errorRate: 0.01, variance: 0.1)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(bottlenecks);
            var bottleneck = bottlenecks.First(b => b.Component == nameof(SlowRequest));
            Assert.Equal(BottleneckSeverity.High, bottleneck.Severity);
            Assert.Contains("execution time", bottleneck.Description, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(bottleneck.RecommendedActions);
            Assert.True(bottleneck.Impact > 0);
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Detect_High_Error_Rate()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(ErrorProneRequest)] = CreateAnalysisData(avgExecutionMs: 200, errorRate: 0.15, variance: 0.1)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(bottlenecks);
            var bottleneck = bottlenecks.First(b => b.Component == nameof(ErrorProneRequest));
            Assert.Equal(BottleneckSeverity.Critical, bottleneck.Severity);
            Assert.Contains("error rate", bottleneck.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(bottleneck.RecommendedActions, a => a.Contains("circuit breaker", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Detect_High_Variance()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(InconsistentRequest)] = CreateAnalysisData(avgExecutionMs: 500, errorRate: 0.01, variance: 0.6)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(bottlenecks);
            var bottleneck = bottlenecks.First(b => b.Component == nameof(InconsistentRequest));
            Assert.Equal(BottleneckSeverity.Medium, bottleneck.Severity);
            Assert.Contains("variance", bottleneck.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(bottleneck.RecommendedActions, a => a.Contains("resource contention", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Return_Empty_For_Good_Performance()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(GoodRequest)] = CreateAnalysisData(avgExecutionMs: 100, errorRate: 0.01, variance: 0.1)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.Empty(bottlenecks);
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Detect_Multiple_Issues_For_Same_Request()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(ProblematicRequest)] = CreateAnalysisData(avgExecutionMs: 1200, errorRate: 0.12, variance: 0.55)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.True(bottlenecks.Count >= 3); // Slow execution, high error rate, high variance
            var component = nameof(ProblematicRequest);
            Assert.Contains(bottlenecks, b => b.Component == component && b.Severity == BottleneckSeverity.High);
            Assert.Contains(bottlenecks, b => b.Component == component && b.Severity == BottleneckSeverity.Critical);
            Assert.Contains(bottlenecks, b => b.Component == component && b.Severity == BottleneckSeverity.Medium);
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Sort_By_Severity_Then_Impact()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(Request1)] = CreateAnalysisData(avgExecutionMs: 100, errorRate: 0.15, variance: 0.1),  // Critical (error)
                [typeof(Request2)] = CreateAnalysisData(avgExecutionMs: 1500, errorRate: 0.02, variance: 0.1), // High (slow)
                [typeof(Request3)] = CreateAnalysisData(avgExecutionMs: 500, errorRate: 0.02, variance: 0.6),  // Medium (variance)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(bottlenecks);
            // First should be Critical severity (high error rate has Critical severity)
            var criticalBottlenecks = bottlenecks.Where(b => b.Severity == BottleneckSeverity.Critical).ToList();
            Assert.NotEmpty(criticalBottlenecks);
            // Verify ordering - Critical first, then High, then Medium
            var severityOrder = bottlenecks.Select(b => b.Severity).ToList();
            Assert.Equal(BottleneckSeverity.Critical, severityOrder.First());
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Handle_Empty_Analytics()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>();

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.Empty(bottlenecks);
        }

        [Fact]
        public void IdentifyBottlenecks_Should_Include_Estimated_Resolution_Time()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(SlowRequest)] = CreateAnalysisData(avgExecutionMs: 1500, errorRate: 0.01, variance: 0.1)
            };

            // Act
            var bottlenecks = _analyzer.IdentifyBottlenecks(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.All(bottlenecks, b => Assert.True(b.EstimatedResolutionTime > TimeSpan.Zero));
        }

        #endregion

        #region IdentifyOptimizationOpportunities Tests

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Recommend_Caching_For_Frequent_Requests()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(FrequentRequest)] = CreateAnalysisDataWithExecutions(
                    totalExecutions: 150,
                    avgExecutionMs: 120,
                    concurrentPeaks: 5
                )
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(opportunities);
            var cacheOpportunity = opportunities.First(o => o.Title.Contains("Cache", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("FrequentRequest", cacheOpportunity.Title);
            Assert.True(cacheOpportunity.ExpectedImprovement > 0);
            Assert.Equal(OptimizationPriority.High, cacheOpportunity.Priority);
            Assert.NotEmpty(cacheOpportunity.Steps);
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Recommend_Batching_For_High_Concurrency()
        {
            // Arrange
            var processorCount = Environment.ProcessorCount;
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(ConcurrentRequest)] = CreateAnalysisDataWithExecutions(
                    totalExecutions: 50,
                    avgExecutionMs: 150,
                    concurrentPeaks: processorCount * 3
                )
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(opportunities);
            var batchOpportunity = opportunities.First(o => o.Title.Contains("batch", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("ConcurrentRequest", batchOpportunity.Title);
            Assert.Equal(OptimizationPriority.Medium, batchOpportunity.Priority);
            Assert.Contains(batchOpportunity.Steps, s => s.Contains("batch", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Recommend_Consistency_For_High_Variance()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(VariableRequest)] = CreateAnalysisDataWithVariance(
                    avgExecutionMs: 600,
                    variance: 0.45
                )
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.NotEmpty(opportunities);
            var consistencyOpportunity = opportunities.First(o => o.Title.Contains("consistent", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("VariableRequest", consistencyOpportunity.Title);
            Assert.Equal(OptimizationPriority.Medium, consistencyOpportunity.Priority);
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Return_Empty_For_Low_Usage()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(RareRequest)] = CreateAnalysisDataWithExecutions(
                    totalExecutions: 10,
                    avgExecutionMs: 50,
                    concurrentPeaks: 1
                )
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.Empty(opportunities);
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Sort_By_ROI()
        {
            // Arrange
            var processorCount = Environment.ProcessorCount;
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                // High improvement, low effort (high ROI)
                [typeof(Request1)] = CreateAnalysisDataWithExecutions(150, 120, 5),

                // Medium improvement, high effort (low ROI)
                [typeof(Request2)] = CreateAnalysisDataWithVariance(600, 0.45),

                // Medium improvement, medium effort (medium ROI)
                [typeof(Request3)] = CreateAnalysisDataWithExecutions(50, 150, processorCount * 3)
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.True(opportunities.Count >= 3);
            // First opportunity should have better ROI than the last
            var firstROI = opportunities.First().ExpectedImprovement / opportunities.First().ImplementationEffort.TotalHours;
            var lastROI = opportunities.Last().ExpectedImprovement / opportunities.Last().ImplementationEffort.TotalHours;
            Assert.True(firstROI >= lastROI);
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Include_Implementation_Steps()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(FrequentRequest)] = CreateAnalysisDataWithExecutions(150, 120, 5)
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.All(opportunities, o =>
            {
                Assert.NotEmpty(o.Steps);
                Assert.True(o.ImplementationEffort > TimeSpan.Zero);
            });
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Handle_Empty_Analytics()
        {
            // Arrange
            var analytics = new Dictionary<Type, RequestAnalysisData>();

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.Empty(opportunities);
        }

        [Fact]
        public void IdentifyOptimizationOpportunities_Should_Find_Multiple_Opportunities_For_Same_Request()
        {
            // Arrange
            var processorCount = Environment.ProcessorCount;
            var analytics = new Dictionary<Type, RequestAnalysisData>
            {
                [typeof(ComplexRequest)] = CreateComplexAnalysisData(
                    totalExecutions: 200,
                    avgExecutionMs: 700,
                    concurrentPeaks: processorCount * 3,
                    variance: 0.5
                )
            };

            // Act
            var opportunities = _analyzer.IdentifyOptimizationOpportunities(analytics, TimeSpan.FromHours(1));

            // Assert
            Assert.True(opportunities.Count >= 2);
            var complexRequestOpportunities = opportunities.Where(o => o.Title.Contains("ComplexRequest")).ToList();
            Assert.True(complexRequestOpportunities.Count >= 2);
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

        private RequestAnalysisData CreateAnalysisData(double avgExecutionMs, double errorRate, double variance)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(avgExecutionMs),
                TotalExecutions = 100,
                SuccessfulExecutions = (long)(100 * (1 - errorRate)),
                FailedExecutions = (long)(100 * errorRate)
            };
            data.AddMetrics(metrics);

            if (variance > 0.1)
            {
                for (int i = 0; i < 10; i++)
                {
                    var variedTime = avgExecutionMs * (1 + (i % 2 == 0 ? variance : -variance));
                    var variedMetrics = new RequestExecutionMetrics
                    {
                        AverageExecutionTime = TimeSpan.FromMilliseconds(variedTime),
                        TotalExecutions = 1,
                        SuccessfulExecutions = 1,
                        FailedExecutions = 0
                    };
                    data.AddMetrics(variedMetrics);
                }
            }

            return data;
        }

        private RequestAnalysisData CreateAnalysisDataWithExecutions(
            int totalExecutions,
            double avgExecutionMs,
            int concurrentPeaks)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(avgExecutionMs),
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = totalExecutions - 1,
                FailedExecutions = 1,
                ConcurrentExecutions = concurrentPeaks
            };
            data.AddMetrics(metrics);
            return data;
        }

        private RequestAnalysisData CreateAnalysisDataWithVariance(double avgExecutionMs, double variance)
        {
            var data = new RequestAnalysisData();
            var baseMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(avgExecutionMs),
                TotalExecutions = 50,
                SuccessfulExecutions = 48,
                FailedExecutions = 2
            };
            data.AddMetrics(baseMetrics);

            for (int i = 0; i < 10; i++)
            {
                var variedTime = avgExecutionMs * (1 + (i % 2 == 0 ? variance : -variance));
                var variedMetrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(variedTime),
                    TotalExecutions = 1,
                    SuccessfulExecutions = 1,
                    FailedExecutions = 0
                };
                data.AddMetrics(variedMetrics);
            }

            return data;
        }

        private RequestAnalysisData CreateComplexAnalysisData(
            int totalExecutions,
            double avgExecutionMs,
            int concurrentPeaks,
            double variance)
        {
            var data = new RequestAnalysisData();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(avgExecutionMs),
                TotalExecutions = totalExecutions,
                SuccessfulExecutions = totalExecutions - 5,
                FailedExecutions = 5,
                ConcurrentExecutions = concurrentPeaks
            };
            data.AddMetrics(metrics);

            for (int i = 0; i < 10; i++)
            {
                var variedTime = avgExecutionMs * (1 + (i % 2 == 0 ? variance : -variance));
                var variedMetrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(variedTime),
                    TotalExecutions = 1,
                    SuccessfulExecutions = 1,
                    FailedExecutions = 0
                };
                data.AddMetrics(variedMetrics);
            }

            return data;
        }

        #endregion

        #region Test Types

        private class TestRequest { }
        private class SlowRequest { }
        private class ErrorProneRequest { }
        private class InconsistentRequest { }
        private class GoodRequest { }
        private class ProblematicRequest { }
        private class Request1 { }
        private class Request2 { }
        private class Request3 { }
        private class FrequentRequest { }
        private class ConcurrentRequest { }
        private class VariableRequest { }
        private class RareRequest { }
        private class ComplexRequest { }

        #endregion
    }
}
