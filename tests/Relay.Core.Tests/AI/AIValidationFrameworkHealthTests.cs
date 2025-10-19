using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIValidationFrameworkHealthTests
    {
        private readonly ILogger<AIValidationFramework> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly AIValidationFramework _validationFramework;

        public AIValidationFrameworkHealthTests()
        {
            _logger = NullLogger<AIValidationFramework>.Instance;
            _options = new AIOptimizationOptions
            {
                MinConfidenceScore = 0.7,
                MaxAutomaticOptimizationRisk = RiskLevel.Medium,
                EnableAutomaticOptimization = true
            };
            _validationFramework = new AIValidationFramework(_logger, _options);
        }

        #region ValidateSystemHealthAsync Tests

        [Fact]
        public async Task ValidateSystemHealthAsync_WithHealthySystem_ShouldReturnStable()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.9,
                    Reliability = 0.95
                },
                PerformanceGrade = 'D', // D or better to avoid performance grade validation
                Bottlenecks = new List<PerformanceBottleneck>(),
                Predictions = new PredictiveAnalysis
                {
                    PredictionConfidence = 0.85
                }
            };

            // Act
            var result = await _validationFramework.ValidateSystemHealthAsync(
                insights,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsStable);
            Assert.Empty(result.Issues);
            Assert.True(result.StabilityScore > 0.8);
        }

        [Fact]
        public async Task ValidateSystemHealthAsync_WithLowHealthScore_ShouldReturnIssue()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.4, // Critical
                    Reliability = 0.9
                },
                PerformanceGrade = 'B',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Predictions = new PredictiveAnalysis
                {
                    PredictionConfidence = 0.8
                }
            };

            // Act
            var result = await _validationFramework.ValidateSystemHealthAsync(
                insights,
                CancellationToken.None);

            // Assert
            Assert.False(result.IsStable);
            Assert.Contains(result.Issues, i =>
                i.Component == "Overall System" &&
                i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public async Task ValidateSystemHealthAsync_WithPoorPerformanceGrade_ShouldReturnIssue()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.4, // Low enough to trigger error
                    Reliability = 0.9
                },
                PerformanceGrade = 'F', // Failed
                Bottlenecks = new List<PerformanceBottleneck>(),
                Predictions = new PredictiveAnalysis
                {
                    PredictionConfidence = 0.8
                }
            };

            // Act
            var result = await _validationFramework.ValidateSystemHealthAsync(
                insights,
                CancellationToken.None);

            // Assert
            Assert.False(result.IsStable);
            Assert.Contains(result.Issues, i => i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public async Task ValidateSystemHealthAsync_WithCriticalBottleneck_ShouldReturnError()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.8,
                    Reliability = 0.9
                },
                PerformanceGrade = 'B',
                Bottlenecks = new List<PerformanceBottleneck>
                {
                    new PerformanceBottleneck
                    {
                        Component = "Database",
                        Description = "High query latency",
                        Severity = BottleneckSeverity.Critical,
                        RecommendedActions = new List<string> { "Add index" }
                    }
                },
                Predictions = new PredictiveAnalysis
                {
                    PredictionConfidence = 0.8
                }
            };

            // Act
            var result = await _validationFramework.ValidateSystemHealthAsync(
                insights,
                CancellationToken.None);

            // Assert
            Assert.False(result.IsStable);
            Assert.Contains(result.Issues, i =>
                i.Component == "Database" &&
                i.Severity == ValidationSeverity.Error &&
                i.Impact == "Critical");
        }

        [Fact]
        public async Task ValidateSystemHealthAsync_WithLowReliability_ShouldReturnWarning()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.8,
                    Reliability = 0.7 // Low reliability
                },
                PerformanceGrade = 'B',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Predictions = new PredictiveAnalysis
                {
                    PredictionConfidence = 0.8
                }
            };

            // Act
            var result = await _validationFramework.ValidateSystemHealthAsync(
                insights,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsStable); // Still stable (warning, not error)
            Assert.Contains(result.Issues, i =>
                i.Component == "Reliability" &&
                i.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public async Task ValidateSystemHealthAsync_WithLowPredictionConfidence_ShouldReturnWarning()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.8,
                    Reliability = 0.95
                },
                PerformanceGrade = 'B',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Predictions = new PredictiveAnalysis
                {
                    PredictionConfidence = 0.5 // Low
                }
            };

            // Act
            var result = await _validationFramework.ValidateSystemHealthAsync(
                insights,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsStable);
            Assert.Contains(result.Issues, i =>
                i.Component == "Predictive Analytics" &&
                i.Severity == ValidationSeverity.Warning);
        }

        #endregion
    }
}