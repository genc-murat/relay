using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Validation
{
    public class AIValidationFrameworkCustomValidationTests
    {
        private readonly ILogger<AIValidationFramework> _logger;
        private readonly AIOptimizationOptions _options;
        private readonly AIValidationFramework _validationFramework;

        public AIValidationFrameworkCustomValidationTests()
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

        #region Caching Strategy Custom Validation Tests

        [Fact]
        public async Task ValidateCachingStrategy_WithLowHitRate_ShouldReturnWarning()
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
                    ["RequestType"] = typeof(TestQuery),
                    ["ExpectedHitRate"] = 0.25 // Below 0.3 threshold
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestQuery),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Warning, result.Severity);
            Assert.Contains(result.Warnings, w => w.Contains("cache hit rate") && w.Contains("low"));
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithHighHitRate_ShouldReturnSuccess()
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
                    ["RequestType"] = typeof(TestQuery),
                    ["ExpectedHitRate"] = 0.8 // Above 0.3 threshold
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestQuery),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Success, result.Severity);
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithExactThresholdHitRate_ShouldReturnSuccess()
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
                    ["RequestType"] = typeof(TestQuery),
                    ["ExpectedHitRate"] = 0.3 // Exactly at threshold
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestQuery),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Success, result.Severity);
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithCommandType_ShouldReturnWarning()
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
                    ["RequestType"] = typeof(UpdateUserCommand),
                    ["ExpectedHitRate"] = 0.7
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(UpdateUserCommand),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Warning, result.Severity);
            Assert.Contains(result.Warnings, w => w.Contains("Commands") && w.Contains("not suitable for caching"));
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithQueryType_ShouldReturnSuccess()
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
                    ["RequestType"] = typeof(GetUserQuery),
                    ["ExpectedHitRate"] = 0.7
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(GetUserQuery),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Success, result.Severity);
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithCommandInTypeNameButNotActualCommand_ShouldReturnWarning()
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
                    ["RequestType"] = typeof(CommandLikeQuery),
                    ["ExpectedHitRate"] = 0.7
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(CommandLikeQuery),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Warning, result.Severity);
            Assert.Contains(result.Warnings, w => w.Contains("Commands") && w.Contains("not suitable for caching"));
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithMissingHitRateParameter_ShouldReturnError()
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
                    ["RequestType"] = typeof(TestQuery)
                    // Missing ExpectedHitRate
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestQuery),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ValidationSeverity.Error, result.Severity);
            Assert.Contains(result.Errors, e => e.Contains("Required parameter") && e.Contains("ExpectedHitRate"));
        }

        [Fact]
        public async Task ValidateCachingStrategy_WithInvalidHitRateType_ShouldReturnSuccess()
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
                    ["RequestType"] = typeof(TestQuery),
                    ["ExpectedHitRate"] = "invalid" // String instead of double
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(TestQuery),
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Success, result.Severity);
            // Custom validation only triggers if parameter is of correct type
        }

        #endregion

        #region Batching Strategy Custom Validation Tests

        [Fact]
        public async Task ValidateBatchingStrategy_WithSmallBatchSize_ShouldReturnError()
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
                    ["OptimalBatchSize"] = 1 // Below minimum of 2
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
            Assert.Contains(result.Errors, e => e.Contains("Batch size must be at least 2"));
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithMinimumBatchSize_ShouldReturnSuccess()
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
                    ["OptimalBatchSize"] = 2 // Exactly at minimum
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
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithLargeBatchSize_ShouldReturnWarning()
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
                    ["OptimalBatchSize"] = 150 // Above 100 threshold
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
            Assert.Contains(result.Warnings, w => w.Contains("Large batch size") && w.Contains("memory pressure"));
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithMaximumSafeBatchSize_ShouldReturnSuccess()
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
                    ["OptimalBatchSize"] = 100 // Exactly at warning threshold
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
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithModerateBatchSize_ShouldReturnSuccess()
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
                    ["OptimalBatchSize"] = 50 // Moderate size
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
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithMissingBatchSizeParameter_ShouldReturnError()
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
                    ["RequestType"] = typeof(TestRequest)
                    // Missing OptimalBatchSize
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
            Assert.Contains(result.Errors, e => e.Contains("Required parameter") && e.Contains("OptimalBatchSize"));
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithInvalidBatchSizeType_ShouldReturnSuccess()
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
                    ["OptimalBatchSize"] = "invalid" // String instead of int
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
            // Custom validation only triggers if parameter is of correct type
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithNegativeBatchSize_ShouldReturnError()
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
                    ["OptimalBatchSize"] = -5 // Negative batch size
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
            Assert.Contains(result.Errors, e => e.Contains("Batch size must be at least 2"));
        }

        [Fact]
        public async Task ValidateBatchingStrategy_WithZeroBatchSize_ShouldReturnError()
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
                    ["OptimalBatchSize"] = 0 // Zero batch size
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
            Assert.Contains(result.Errors, e => e.Contains("Batch size must be at least 2"));
        }

        #endregion

        #region Memory Pooling Strategy Custom Validation Tests

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithLowThreshold_ShouldReturnWarning()
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
                    ["MemoryThreshold"] = 512L // Below 1024 threshold
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
            Assert.Contains(result.Warnings, w => w.Contains("Memory threshold") && w.Contains("very low"));
        }

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithMinimumThreshold_ShouldReturnSuccess()
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
                    ["MemoryThreshold"] = 1024L // Exactly at threshold
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
        }

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithHighThreshold_ShouldReturnSuccess()
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
                    ["MemoryThreshold"] = 8192L // Above threshold
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
        }

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithMissingThresholdParameter_ShouldReturnError()
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
                    ["RequestType"] = typeof(TestRequest)
                    // Missing MemoryThreshold
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
            Assert.Contains(result.Errors, e => e.Contains("Required parameter") && e.Contains("MemoryThreshold"));
        }

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithInvalidThresholdType_ShouldReturnSuccess()
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
                    ["MemoryThreshold"] = "invalid" // String instead of long
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
            // Custom validation only triggers if parameter is of correct type
        }

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithNegativeThreshold_ShouldReturnWarning()
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
                    ["MemoryThreshold"] = -1024L // Negative threshold
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
            Assert.Contains(result.Warnings, w => w.Contains("Memory threshold") && w.Contains("very low"));
        }

        [Fact]
        public async Task ValidateMemoryPoolingStrategy_WithZeroThreshold_ShouldReturnWarning()
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
                    ["MemoryThreshold"] = 0L // Zero threshold
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
            Assert.Contains(result.Warnings, w => w.Contains("Memory threshold") && w.Contains("very low"));
        }

        #endregion

        #region Edge Cases and Error Handling Tests

        [Fact]
        public async Task ValidateRecommendationAsync_WithUnknownStrategy_ShouldReturnSuccess()
        {
            // Arrange
            var recommendation = new OptimizationRecommendation
            {
                Strategy = (OptimizationStrategy)999, // Unknown strategy
                ConfidenceScore = 0.8,
                Risk = RiskLevel.Low,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Parameters = new Dictionary<string, object>
                {
                    ["SomeParameter"] = "value"
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
            // Unknown strategies should not trigger custom validation
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithNullRequestType_ShouldReturnSuccess()
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
                    ["RequestType"] = "TestRequest",
                    ["ExpectedHitRate"] = 0.7
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                null, // Null request type
                CancellationToken.None);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(ValidationSeverity.Success, result.Severity);
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithMultipleValidationIssues_ShouldReturnAllIssues()
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
                    ["RequestType"] = typeof(UpdateUserCommand), // Command type (affects caching validation)
                    ["OptimalBatchSize"] = 1, // Too small
                    ["ExtraParam"] = null // Null parameter
                }
            };

            // Act
            var result = await _validationFramework.ValidateRecommendationAsync(
                recommendation,
                typeof(UpdateUserCommand),
                CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(ValidationSeverity.Error, result.Severity);
            Assert.Contains(result.Errors, e => e.Contains("Batch size must be at least 2"));
            Assert.Contains(result.Errors, e => e.Contains("ExtraParam") && e.Contains("null value"));
        }

        [Fact]
        public async Task ValidateRecommendationAsync_WithCancellationRequested_ShouldRespectCancellation()
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
                    ["RequestType"] = typeof(TestQuery),
                    ["ExpectedHitRate"] = 0.7
                }
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _validationFramework.ValidateRecommendationAsync(
                    recommendation,
                    typeof(TestQuery),
                    cts.Token));
        }

        #endregion

        #region Test Helper Classes

        private class TestRequest
        {
        }

        private class TestQuery
        {
        }

        private class GetUserQuery
        {
        }

        private class UpdateUserCommand
        {
        }

        private class CommandLikeQuery
        {
            // Class name contains "Command" but it's actually a query
        }

        #endregion
    }
}