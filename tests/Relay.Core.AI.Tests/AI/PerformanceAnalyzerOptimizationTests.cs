using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class PerformanceAnalyzerOptimizationTests
    {
        private readonly ILogger<PerformanceAnalyzer> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly PerformanceAnalyzer _analyzer;

        public PerformanceAnalyzerOptimizationTests()
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

        private class FrequentRequest { }
        private class ConcurrentRequest { }
        private class VariableRequest { }
        private class RareRequest { }
        private class ComplexRequest { }
        private class Request1 { }
        private class Request2 { }
        private class Request3 { }

        #endregion
    }
}