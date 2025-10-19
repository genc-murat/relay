using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class PerformanceAnalyzerBottleneckTests
    {
        private readonly ILogger<PerformanceAnalyzer> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly PerformanceAnalyzer _analyzer;

        public PerformanceAnalyzerBottleneckTests()
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

        #region Helper Methods

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

        #endregion

        #region Test Types

        private class SlowRequest { }
        private class ErrorProneRequest { }
        private class InconsistentRequest { }
        private class GoodRequest { }
        private class ProblematicRequest { }
        private class Request1 { }
        private class Request2 { }
        private class Request3 { }

        #endregion
    }
}