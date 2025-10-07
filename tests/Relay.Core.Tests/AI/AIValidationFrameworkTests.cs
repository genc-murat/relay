using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIValidationFrameworkTests
    {
        private readonly ILogger<AIValidationFramework> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly AIValidationFramework _validationFramework;

        public AIValidationFrameworkTests()
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

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitialize()
        {
            // Arrange & Act
            var framework = new AIValidationFramework(_logger, _options);

            // Assert
            Assert.NotNull(framework);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIValidationFramework(null!, _options));
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIValidationFramework(_logger, null!));
        }

        #endregion

        #region ValidateRecommendationAsync Tests

        [Fact]
        public async Task ValidateRecommendationAsync_WithValidRecommendation_ShouldReturnValid()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.85,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["ExpectedHitRate"] = 0.7
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Success, result.Severity);
            Assert.Empty(result.Errors);
            Assert.Equal(OptimizationStrategy.EnableCaching, result.ValidatedStrategy);
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithLowConfidence_ShouldReturnInvalid()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.5, // Below threshold
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["OptimalBatchSize"] = 10
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ValidationSeverity.Error, result.Severity);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("below minimum threshold", result.Errors[0]);
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithHighRisk_ShouldReturnWarning()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.MemoryPooling,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.High, // Exceeds max automatic risk
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["MemoryThreshold"] = 2048L
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Warning, result.Severity);
            Assert.NotEmpty(result.Warnings);
            Assert.Contains("exceeds maximum automatic optimization risk", result.Warnings[0]);
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithNoImprovement_ShouldReturnWarning()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.Zero, // No improvement
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["ExpectedHitRate"] = 0.5
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Warning, result.Severity);
            Assert.Contains(result.Warnings, w => w.Contains("No performance improvement"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithMissingRequiredParameter_ShouldReturnInvalid()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest)
                    // Missing ExpectedHitRate
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ValidationSeverity.Error, result.Severity);
            Assert.Contains(result.Errors, e => e.Contains("Required parameter") && e.Contains("ExpectedHitRate"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithNullParameter_ShouldReturnInvalid()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = null,
                    ["OptimalBatchSize"] = 10
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("null value"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_Caching_WithLowHitRate_ShouldReturnWarning()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["ExpectedHitRate"] = 0.2 // Low hit rate
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.NotEmpty(result.Warnings);
            Assert.Contains(result.Warnings, w => w.Contains("cache hit rate") && w.Contains("low"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_Caching_WithCommandType_ShouldReturnWarning()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestCommand),
                    ["ExpectedHitRate"] = 0.7
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestCommand),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Commands") && w.Contains("not suitable for caching"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_Batching_WithInvalidBatchSize_ShouldReturnInvalid()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["OptimalBatchSize"] = 1 // Too small
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Batch size must be at least 2"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_Batching_WithLargeBatchSize_ShouldReturnWarning()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["OptimalBatchSize"] = 150 // Very large
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Large batch size") && w.Contains("memory pressure"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_MemoryPooling_WithLowThreshold_ShouldReturnWarning()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.MemoryPooling,
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["RequestType"] = typeof(TestRequest),
                    ["MemoryThreshold"] = 512L // Low threshold
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestRequest),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Memory threshold") && w.Contains("very low"));
        }

        #endregion

        #region ValidateModelPerformanceAsync Tests

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithHealthyModel_ShouldReturnHealthy()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.85,
                F1Score = 0.8,
                TrainingDataPoints = 500,
                LastRetraining = DateTime.UtcNow.AddDays(-2),
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                ModelConfidence = 0.9
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Empty(result.Issues);
            Assert.True(result.OverallScore > 0.7);
        }

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithLowAccuracy_ShouldReturnIssue()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.4, // Very low
                F1Score = 0.8,
                TrainingDataPoints = 500,
                LastRetraining = DateTime.UtcNow,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                ModelConfidence = 0.9
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Contains(result.Issues, i =>
                i.Type == ModelIssueType.LowAccuracy &&
                i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithLowF1Score_ShouldReturnWarning()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.8,
                F1Score = 0.5, // Low F1
                TrainingDataPoints = 500,
                LastRetraining = DateTime.UtcNow,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                ModelConfidence = 0.9
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsHealthy); // Should still be healthy (warning, not error)
            Assert.Contains(result.Issues, i =>
                i.Type == ModelIssueType.InconsistentPredictions &&
                i.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithInsufficientData_ShouldReturnWarning()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.8,
                F1Score = 0.8,
                TrainingDataPoints = 50, // Insufficient
                LastRetraining = DateTime.UtcNow,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                ModelConfidence = 0.9
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Contains(result.Issues, i =>
                i.Type == ModelIssueType.InsufficientData &&
                i.Description.Contains("50 training data points"));
        }

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithStaleModel_ShouldReturnWarning()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.8,
                F1Score = 0.8,
                TrainingDataPoints = 500,
                LastRetraining = DateTime.UtcNow.AddDays(-30), // Stale
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                ModelConfidence = 0.9
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Contains(result.Issues, i =>
                i.Type == ModelIssueType.StaleModel &&
                i.Description.Contains("30 days ago"));
        }

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithSlowPredictions_ShouldReturnWarning()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.8,
                F1Score = 0.8,
                TrainingDataPoints = 500,
                LastRetraining = DateTime.UtcNow,
                AveragePredictionTime = TimeSpan.FromMilliseconds(250), // Slow
                ModelConfidence = 0.9
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Contains(result.Issues, i =>
                i.Type == ModelIssueType.SlowPredictions &&
                i.Description.Contains("250"));
        }

        [Fact]
        public async Task ValidateModelPerformanceAsync_WithMultipleIssues_ShouldCalculateScore()
        {
            // Arrange
            var statistics = new AIModelStatistics
            {
                AccuracyScore = 0.65, // Warning-level
                F1Score = 0.55, // Warning
                TrainingDataPoints = 80, // Warning
                LastRetraining = DateTime.UtcNow.AddDays(-10), // Warning
                AveragePredictionTime = TimeSpan.FromMilliseconds(150), // Warning
                ModelConfidence = 0.7
            };

            // Act
            var result = await _validationFramework.ValidateModelPerformanceAsync(
                statistics,
                CancellationToken.None);

            // Assert
            Assert.True(result.IsHealthy); // All warnings, no errors
            Assert.True(result.Issues.Length >= 4);
            Assert.True(result.OverallScore < 0.8); // Score should be reduced due to issues
        }

        #endregion

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

        #region ValidateOptimizationResultsAsync Tests

        [Fact]
        public async Task ValidateOptimizationResultsAsync_WithImprovement_ShouldReturnSuccessful()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 1024
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100), // 50% improvement
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                MemoryAllocated = 512
            };

            var strategies = new[] { OptimizationStrategy.EnableCaching };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.True(result.WasSuccessful);
            Assert.True(result.OverallImprovement > 0);
            Assert.Single(result.StrategyResults);
            Assert.True(result.StrategyResults[0].WasSuccessful);
        }

        [Fact]
        public async Task ValidateOptimizationResultsAsync_WithNoImprovement_ShouldReturnUnsuccessful()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 512
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(150), // Worse
                TotalExecutions = 100,
                SuccessfulExecutions = 85,
                MemoryAllocated = 1024
            };

            var strategies = new[] { OptimizationStrategy.BatchProcessing };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.False(result.WasSuccessful);
            Assert.True(result.OverallImprovement < 0);
        }

        [Fact]
        public async Task ValidateOptimizationResultsAsync_WithMultipleStrategies_ShouldValidateAll()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(300),
                TotalExecutions = 100,
                SuccessfulExecutions = 80,
                MemoryAllocated = 2048
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                MemoryAllocated = 512
            };

            var strategies = new[]
            {
                OptimizationStrategy.EnableCaching,
                OptimizationStrategy.MemoryPooling,
                OptimizationStrategy.BatchProcessing
            };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.True(result.WasSuccessful);
            Assert.Equal(3, result.StrategyResults.Length);
            Assert.All(result.StrategyResults, sr => Assert.True(sr.WasSuccessful));
        }

        [Fact]
        public async Task ValidateOptimizationResultsAsync_ShouldCalculatePerformanceGain()
        {
            // Arrange
            var beforeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 1024
            };

            var afterMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                MemoryAllocated = 1024
            };

            var strategies = new[] { OptimizationStrategy.EnableCaching };

            // Act
            var result = await _validationFramework.ValidateOptimizationResultsAsync(
                strategies,
                beforeMetrics,
                afterMetrics,
                CancellationToken.None);

            // Assert
            Assert.True(result.WasSuccessful);
            Assert.Equal(0.5, result.StrategyResults[0].PerformanceGain, 2); // 50% improvement
        }

        #endregion

        #region Helper Classes

        private class TestRequest
        {
        }

        private class TestCommand
        {
        }

        #endregion
    }
}
