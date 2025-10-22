using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class MachineLearningEnhancementServiceTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly MachineLearningEnhancementService _service;

    public MachineLearningEnhancementServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _service = new MachineLearningEnhancementService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MachineLearningEnhancementService(null!));
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Throw_When_BaseRecommendation_Is_Null()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData();
        var systemMetrics = new Dictionary<string, double>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.ApplyMachineLearningEnhancements(null!, analysisData, systemMetrics));
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Throw_When_AnalysisData_Is_Null()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var systemMetrics = new Dictionary<string, double>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.ApplyMachineLearningEnhancements(baseRecommendation, null!, systemMetrics));
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Throw_When_SystemMetrics_Is_Null()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var analysisData = CreateTestAnalysisData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, null!));
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Return_Enhanced_Result()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var analysisData = CreateTestAnalysisData();
        var systemMetrics = new Dictionary<string, double> { ["CpuUtilization"] = 0.9 };

        // Act
        var result = _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, systemMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OptimizationStrategy.CircuitBreaker, result.AlternativeStrategy); // Due to high error rate
        Assert.True(result.EnhancedConfidence >= 0.1 && result.EnhancedConfidence <= 0.95);
        Assert.NotNull(result.AdditionalParameters);
        Assert.Contains("insight_0", result.AdditionalParameters.Keys);
        Assert.Contains("High CPU utilization may limit parallel processing effectiveness", result.AdditionalParameters["insight_0"].ToString());
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Enable_Caching_For_High_Hit_Ratio()
    {
        // Arrange
        var baseRecommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.None,
            ConfidenceScore = 0.5
        };
        var analysisData = new RequestAnalysisData
        {
            CacheHitRatio = 0.9,
            RepeatRequestRate = 0.5,
            DatabaseCalls = 5
        };
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            FailedExecutions = 2
        });
        var systemMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, systemMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.EnableCaching, result.AlternativeStrategy);
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Generate_Insights_For_High_Repeat_Rate()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var analysisData = new RequestAnalysisData
        {
            CacheHitRatio = 0.5,
            RepeatRequestRate = 0.8,
            DatabaseCalls = 5
        };
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            FailedExecutions = 2
        });
        var systemMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, systemMetrics);

        // Assert
        Assert.Contains("insight_0", result.AdditionalParameters.Keys);
        Assert.Contains("High repeat request rate suggests strong caching opportunity", result.AdditionalParameters["insight_0"].ToString());
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Generate_Insights_For_Multiple_Database_Calls()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var analysisData = new RequestAnalysisData
        {
            CacheHitRatio = 0.5,
            RepeatRequestRate = 0.5,
            DatabaseCalls = 15
        };
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 8,
            FailedExecutions = 2
        });
        var systemMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, systemMetrics);

        // Assert
        Assert.Contains("insight_0", result.AdditionalParameters.Keys);
        Assert.Contains("Multiple database calls per request indicate optimization potential", result.AdditionalParameters["insight_0"].ToString());
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Predict_Trend_Enhancement()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var analysisData = CreateTestAnalysisData();
        var systemMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, systemMetrics);

        // Assert
        Assert.Contains("trend_direction", result.AdditionalParameters.Keys);
        Assert.Contains("trend_magnitude", result.AdditionalParameters.Keys);
        Assert.Contains("trend_confidence", result.AdditionalParameters.Keys);
        Assert.Equal("degrading", result.AdditionalParameters["trend_direction"]);
        Assert.True((double)result.AdditionalParameters["trend_magnitude"] > 0);
        Assert.Equal(0.7, result.AdditionalParameters["trend_confidence"]);
    }

    [Fact]
    public void ApplyMachineLearningEnhancements_Should_Not_Predict_Trend_For_Stable_Performance()
    {
        // Arrange
        var baseRecommendation = CreateTestRecommendation();
        var analysisData = new RequestAnalysisData
        {
            CacheHitRatio = 0.5,
            RepeatRequestRate = 0.5,
            DatabaseCalls = 5
        };

        // Add stable performance metrics
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 5,
            SuccessfulExecutions = 4,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });

        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 5,
            SuccessfulExecutions = 4,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(101)
        });

        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 5,
            SuccessfulExecutions = 4,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(99)
        });

        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 5,
            SuccessfulExecutions = 4,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });

        var systemMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.ApplyMachineLearningEnhancements(baseRecommendation, analysisData, systemMetrics);

        // Assert
        Assert.DoesNotContain("trend_direction", result.AdditionalParameters.Keys);
        Assert.DoesNotContain("trend_magnitude", result.AdditionalParameters.Keys);
        Assert.DoesNotContain("trend_confidence", result.AdditionalParameters.Keys);
    }

    private static OptimizationRecommendation CreateTestRecommendation()
    {
        return new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.7
        };
    }

    private static RequestAnalysisData CreateTestAnalysisData()
    {
        var data = new RequestAnalysisData
        {
            CacheHitRatio = 0.5,
            RepeatRequestRate = 0.5,
            DatabaseCalls = 5
        };

        // Add metrics to simulate high error rate and degrading performance
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 4,
            SuccessfulExecutions = 3,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });

        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 4,
            SuccessfulExecutions = 3,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(90)
        });

        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 4,
            SuccessfulExecutions = 3,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(80)
        });

        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 4,
            SuccessfulExecutions = 3,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(70)
        });

        return data;
    }
}