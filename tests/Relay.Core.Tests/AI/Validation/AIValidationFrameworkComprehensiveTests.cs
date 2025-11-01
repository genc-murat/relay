using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Validation;

public class AIValidationFrameworkComprehensiveTests
{
    private readonly Mock<ILogger<AIValidationFramework>> _mockLogger;
    private readonly AIValidationFramework _validationFramework;
    private readonly AIOptimizationOptions _options;

    public AIValidationFrameworkComprehensiveTests()
    {
        _mockLogger = new Mock<ILogger<AIValidationFramework>>();
        _options = new AIOptimizationOptions
        {
            MinConfidenceScore = 0.7,
            MaxAutomaticOptimizationRisk = RiskLevel.Medium,
            EnableAutomaticOptimization = true
        };
        _validationFramework = new AIValidationFramework(_mockLogger.Object, _options);
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithHighConfidenceCaching_ReturnsSuccess()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.9,
            Risk = RiskLevel.Low,
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>
            {
                ["RequestType"] = "TestRequest",
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Success, result.Severity);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithLowConfidence_ReturnsError()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.5, // Below threshold
            Risk = RiskLevel.Low,
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>
            {
                ["RequestType"] = "TestRequest",
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Confidence score"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithHighRisk_ReturnsWarning()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Risk = RiskLevel.High, // Above threshold for automatic optimization
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>
            {
                ["RequestType"] = "TestRequest",
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.True(result.IsValid); // Still valid but with warning
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("Risk level"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithNoImprovement_ReturnsWarning()
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
                ["RequestType"] = "TestRequest",
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("No performance improvement"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithMissingParameters_ReturnsError()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Risk = RiskLevel.Low,
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>() // Missing required parameters
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("RequestType"));
        Assert.Contains(result.Errors, e => e.Contains("ExpectedHitRate"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithNullParameterValues_ReturnsError()
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
                ["RequestType"] = null,
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("RequestType") && e.Contains("null"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithBatchingStrategy_ValidatesBatchSize()
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
                ["RequestType"] = "TestRequest",
                ["OptimalBatchSize"] = 1 // Too small for batching
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Batch size must be at least 2"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithLargeBatchSize_ReturnsWarning()
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
                ["RequestType"] = "TestRequest",
                ["OptimalBatchSize"] = 150 // Large batch size
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("Large batch size"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithMemoryPoolingStrategy_ValidatesMemoryThreshold()
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
                ["RequestType"] = "TestRequest",
                ["MemoryThreshold"] = 512L // Very low threshold
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("Memory threshold") && w.Contains("very low"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithCommandRequestType_ReturnsCachingWarning()
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
                ["RequestType"] = "CreateUserCommand",
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(CreateUserCommand));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("Commands are typically not suitable for caching"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithLowCacheHitRate_ReturnsWarning()
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
                ["ExpectedHitRate"] = 0.2 // Low hit rate
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("cache hit rate") && w.Contains("low"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithUnknownStrategy_HandlesGracefully()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = (OptimizationStrategy)999, // Unknown strategy
            ConfidenceScore = 0.8,
            Risk = RiskLevel.Low,
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert - Should handle gracefully without throwing
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity); // Warning for no parameters
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithMultipleIssues_ReturnsCombinedSeverity()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.5, // Low confidence - Error
            Risk = RiskLevel.High, // High risk - Warning
            EstimatedImprovement = TimeSpan.Zero, // No improvement - Warning
            Parameters = new Dictionary<string, object>() // Missing parameters - Error
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity); // Error takes precedence
        Assert.NotEmpty(result.Errors);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithDisabledAutomaticOptimization_SkipsRiskValidation()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            MinConfidenceScore = 0.7,
            MaxAutomaticOptimizationRisk = RiskLevel.Low,
            EnableAutomaticOptimization = false // Disabled
        };
        var framework = new AIValidationFramework(_mockLogger.Object, options);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Risk = RiskLevel.High, // Would normally be a warning
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>
            {
                ["RequestType"] = "TestRequest",
                ["ExpectedHitRate"] = 0.8
            }
        };

        // Act
        var result = await framework.ValidateRecommendationAsync(recommendation, typeof(QueryRequest));

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
        // When automatic optimization is disabled, automatic optimization risk warnings are skipped
        // But strategy-specific risk warnings still apply
        if (result.Warnings.Any())
        {
            foreach (var warning in result.Warnings)
            {
                // Should not contain automatic optimization risk warning
                Assert.DoesNotContain("exceeds maximum automatic optimization risk", warning);
                // May contain strategy-specific risk warnings
            }
        }
        
        // Strategy risk validation still applies regardless of automatic optimization setting
        // Since Risk=High exceeds MaxRisk=Low for caching strategy, expect Warning
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Single(result.Warnings);
        Assert.Contains("Strategy risk High exceeds recommended Low", result.Warnings[0]);
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithComplexParameters_ValidatesAll()
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
                ["ExpectedHitRate"] = 0.8,
                ["CacheDuration"] = TimeSpan.FromMinutes(30),
                ["MaxCacheSize"] = 1024L,
                ["CachePolicy"] = "LRU",
                ["NullParameter"] = null
            }
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("NullParameter") && e.Contains("null"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithEmptyParameters_ReturnsWarning()
    {
        // Arrange
        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ConfidenceScore = 0.8,
            Risk = RiskLevel.Low,
            EstimatedImprovement = TimeSpan.FromMilliseconds(100),
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string));

        // Assert
        Assert.False(result.IsValid); // Missing required parameters
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.NotEmpty(result.Errors);
        // Should have errors for missing required parameters
        Assert.Equal(2, result.Errors.Length);
        Assert.Contains(result.Errors, e => e.Contains("Required parameter 'RequestType' is missing"));
        Assert.Contains(result.Errors, e => e.Contains("Required parameter 'ExpectedHitRate' is missing"));
    }

    [Fact]
    public async Task ValidateRecommendationAsync_WithCancellationToken_RespectsCancellation()
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
                ["ExpectedHitRate"] = 0.8
            }
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _validationFramework.ValidateRecommendationAsync(recommendation, typeof(string), cts.Token).AsTask());
    }

    private class QueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    private class CreateUserCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }


}