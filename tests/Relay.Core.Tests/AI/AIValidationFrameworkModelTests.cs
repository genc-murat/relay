using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIValidationFrameworkModelTests
    {
        private readonly ILogger<AIValidationFramework> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly AIValidationFramework _validationFramework;

        public AIValidationFrameworkModelTests()
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
    }
}