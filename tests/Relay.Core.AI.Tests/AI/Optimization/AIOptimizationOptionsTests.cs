using System;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization;

/// <summary>
/// Tests for AIOptimizationOptions.Validate method
/// </summary>
public class AIOptimizationOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Validate_WithDefaultValues_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions();

        // Act & Assert
        options.Validate(); // Should not throw
    }

    #endregion

    #region Batch Size Validation Tests

    [Fact]
    public void Validate_DefaultBatchSizeZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { DefaultBatchSize = 0 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("DefaultBatchSize must be greater than 0", ex.Message);
        Assert.Equal("DefaultBatchSize", ex.ParamName);
    }

    [Fact]
    public void Validate_DefaultBatchSizeNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { DefaultBatchSize = -1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("DefaultBatchSize must be greater than 0", ex.Message);
        Assert.Equal("DefaultBatchSize", ex.ParamName);
    }

    [Fact]
    public void Validate_MaxBatchSizeZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MaxBatchSize = 0 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MaxBatchSize must be greater than 0", ex.Message);
        Assert.Equal("MaxBatchSize", ex.ParamName);
    }

    [Fact]
    public void Validate_MaxBatchSizeNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MaxBatchSize = -1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MaxBatchSize must be greater than 0", ex.Message);
        Assert.Equal("MaxBatchSize", ex.ParamName);
    }

    [Fact]
    public void Validate_DefaultBatchSizeGreaterThanMaxBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            DefaultBatchSize = 50,
            MaxBatchSize = 25
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("DefaultBatchSize cannot be greater than MaxBatchSize", ex.Message);
    }

    [Fact]
    public void Validate_DefaultBatchSizeEqualToMaxBatchSize_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            DefaultBatchSize = 50,
            MaxBatchSize = 50
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    #endregion

    #region Cache TTL Validation Tests

    [Fact]
    public void Validate_MinCacheTtlZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinCacheTtl = TimeSpan.Zero };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinCacheTtl must be greater than zero", ex.Message);
        Assert.Equal("MinCacheTtl", ex.ParamName);
    }

    [Fact]
    public void Validate_MinCacheTtlNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinCacheTtl = TimeSpan.FromMinutes(-1) };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinCacheTtl must be greater than zero", ex.Message);
        Assert.Equal("MinCacheTtl", ex.ParamName);
    }

    [Fact]
    public void Validate_MaxCacheTtlZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MaxCacheTtl = TimeSpan.Zero };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MaxCacheTtl must be greater than zero", ex.Message);
        Assert.Equal("MaxCacheTtl", ex.ParamName);
    }

    [Fact]
    public void Validate_MaxCacheTtlNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MaxCacheTtl = TimeSpan.FromMinutes(-1) };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MaxCacheTtl must be greater than zero", ex.Message);
        Assert.Equal("MaxCacheTtl", ex.ParamName);
    }

    [Fact]
    public void Validate_MinCacheTtlGreaterThanMaxCacheTtl_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            MinCacheTtl = TimeSpan.FromHours(2),
            MaxCacheTtl = TimeSpan.FromHours(1)
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinCacheTtl cannot be greater than MaxCacheTtl", ex.Message);
    }

    [Fact]
    public void Validate_MinCacheTtlEqualToMaxCacheTtl_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            MinCacheTtl = TimeSpan.FromHours(1),
            MaxCacheTtl = TimeSpan.FromHours(1)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    #endregion

    #region Confidence Score Validation Tests

    [Fact]
    public void Validate_MinConfidenceScoreNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinConfidenceScore = -0.1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinConfidenceScore must be between 0.0 and 1.0", ex.Message);
        Assert.Equal("MinConfidenceScore", ex.ParamName);
    }

    [Fact]
    public void Validate_MinConfidenceScoreGreaterThanOne_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinConfidenceScore = 1.1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinConfidenceScore must be between 0.0 and 1.0", ex.Message);
        Assert.Equal("MinConfidenceScore", ex.ParamName);
    }

    [Fact]
    public void Validate_MinConfidenceScoreZero_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinConfidenceScore = 0.0 };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_MinConfidenceScoreOne_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinConfidenceScore = 1.0 };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    #endregion

    #region Min Executions Validation Tests

    [Fact]
    public void Validate_MinExecutionsForAnalysisZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinExecutionsForAnalysis = 0 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinExecutionsForAnalysis must be greater than 0", ex.Message);
        Assert.Equal("MinExecutionsForAnalysis", ex.ParamName);
    }

    [Fact]
    public void Validate_MinExecutionsForAnalysisNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MinExecutionsForAnalysis = -1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MinExecutionsForAnalysis must be greater than 0", ex.Message);
        Assert.Equal("MinExecutionsForAnalysis", ex.ParamName);
    }

    #endregion

    #region Error Rate Threshold Validation Tests

    [Fact]
    public void Validate_HighErrorRateThresholdNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { HighErrorRateThreshold = -0.1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("HighErrorRateThreshold must be between 0.0 and 1.0", ex.Message);
        Assert.Equal("HighErrorRateThreshold", ex.ParamName);
    }

    [Fact]
    public void Validate_HighErrorRateThresholdGreaterThanOne_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { HighErrorRateThreshold = 1.1 };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("HighErrorRateThreshold must be between 0.0 and 1.0", ex.Message);
        Assert.Equal("HighErrorRateThreshold", ex.ParamName);
    }

    [Fact]
    public void Validate_HighErrorRateThresholdZero_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions { HighErrorRateThreshold = 0.0 };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_HighErrorRateThresholdOne_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions { HighErrorRateThreshold = 1.0 };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    #endregion

    #region Weight Validation Tests

    [Fact]
    public void Validate_WeightsSumToLessThanOne_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            PerformanceWeight = 0.3,
            ReliabilityWeight = 0.3,
            ResourceWeight = 0.2,
            UserExperienceWeight = 0.1
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("The sum of all weight properties must equal 1.0", ex.Message);
    }

    [Fact]
    public void Validate_WeightsSumToMoreThanOne_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            PerformanceWeight = 0.5,
            ReliabilityWeight = 0.3,
            ResourceWeight = 0.2,
            UserExperienceWeight = 0.1
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("The sum of all weight properties must equal 1.0", ex.Message);
    }

    [Fact]
    public void Validate_WeightsSumToOne_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            PerformanceWeight = 0.4,
            ReliabilityWeight = 0.3,
            ResourceWeight = 0.2,
            UserExperienceWeight = 0.1
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WeightsSumToOneWithPrecision_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            PerformanceWeight = 0.4,
            ReliabilityWeight = 0.299999,
            ResourceWeight = 0.2,
            UserExperienceWeight = 0.100001
        };

        // Act & Assert
        options.Validate(); // Should not throw (within tolerance)
    }

    #endregion

    #region TimeSpan Validation Tests

    [Fact]
    public void Validate_ModelUpdateIntervalZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { ModelUpdateInterval = TimeSpan.Zero };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("ModelUpdateInterval must be greater than zero", ex.Message);
        Assert.Equal("ModelUpdateInterval", ex.ParamName);
    }

    [Fact]
    public void Validate_ModelUpdateIntervalNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { ModelUpdateInterval = TimeSpan.FromMinutes(-1) };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("ModelUpdateInterval must be greater than zero", ex.Message);
        Assert.Equal("ModelUpdateInterval", ex.ParamName);
    }

    [Fact]
    public void Validate_MetricsExportIntervalZero_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MetricsExportInterval = TimeSpan.Zero };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MetricsExportInterval must be greater than zero", ex.Message);
        Assert.Equal("MetricsExportInterval", ex.ParamName);
    }

    [Fact]
    public void Validate_MetricsExportIntervalNegative_ThrowsArgumentException()
    {
        // Arrange
        var options = new AIOptimizationOptions { MetricsExportInterval = TimeSpan.FromMinutes(-1) };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MetricsExportInterval must be greater than zero", ex.Message);
        Assert.Equal("MetricsExportInterval", ex.ParamName);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void Validate_AllValidValues_Succeeds()
    {
        // Arrange
        var options = new AIOptimizationOptions
        {
            Enabled = true,
            LearningEnabled = true,
            ModelUpdateInterval = TimeSpan.FromMinutes(30),
            DefaultBatchSize = 10,
            MaxBatchSize = 100,
            MinCacheTtl = TimeSpan.FromMinutes(1),
            MaxCacheTtl = TimeSpan.FromHours(24),
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.3,
            MinExecutionsForAnalysis = 10,
            MaxRecentPredictions = 1000,
            HighExecutionTimeThreshold = 500.0,
            HighErrorRateThreshold = 0.05,
            HighMemoryAllocationThreshold = 1024 * 1024,
            HighConcurrencyThreshold = 50,
            ModelTrainingDate = DateTime.UtcNow,
            ModelVersion = "1.0.0",
            LastRetrainingDate = DateTime.UtcNow,
            EnablePredictiveAnalysis = true,
            EnableHealthMonitoring = true,
            EnableBottleneckDetection = true,
            EnableOpportunityIdentification = true,
            PerformanceWeight = 0.4,
            ReliabilityWeight = 0.3,
            ResourceWeight = 0.2,
            UserExperienceWeight = 0.1,
            EnableAutomaticOptimization = false,
            MaxAutomaticOptimizationRisk = RiskLevel.Low,
            EnableDecisionLogging = true,
            EnableMetricsExport = true,
            MetricsExportInterval = TimeSpan.FromMinutes(15),
            MaxEstimatedHttpConnections = 200,
            MaxEstimatedDbConnections = 50,
            EstimatedMaxDbConnections = 100,
            MaxEstimatedExternalConnections = 30,
            MaxEstimatedWebSocketConnections = 1000
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_MultipleValidationErrors_AllReported()
    {
        // Arrange - Create options with multiple validation errors
        var options = new AIOptimizationOptions
        {
            DefaultBatchSize = 0, // Invalid
            MaxBatchSize = 0, // Invalid
            MinCacheTtl = TimeSpan.Zero, // Invalid
            MaxCacheTtl = TimeSpan.Zero, // Invalid
            MinConfidenceScore = -0.1, // Invalid
            MinExecutionsForAnalysis = 0, // Invalid
            HighErrorRateThreshold = 1.5, // Invalid
            PerformanceWeight = 0.5, // Invalid (weights don't sum to 1)
            ReliabilityWeight = 0.5,
            ResourceWeight = 0.5,
            UserExperienceWeight = 0.5,
            ModelUpdateInterval = TimeSpan.Zero, // Invalid
            MetricsExportInterval = TimeSpan.Zero // Invalid
        };

        // Act & Assert - Should throw on first validation error encountered
        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("DefaultBatchSize must be greater than 0", ex.Message);
    }

    #endregion
}