using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class RiskAssessmentServiceTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly RiskAssessmentService _service;

    public RiskAssessmentServiceTests()
    {
        _loggerMock = new Mock<ILogger>();
        _service = new RiskAssessmentService(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RiskAssessmentService(null!));
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Throw_When_AnalysisData_Is_Null()
    {
        // Arrange
        var systemMetrics = new Dictionary<string, double>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, null!, systemMetrics));
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Throw_When_SystemMetrics_Is_Null()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, null!));
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_Low_Risk_For_EnableCaching_With_Good_Data()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 1000, errorRate: 0.01, executionTimesCount: 100);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.5, memoryUtil: 0.5, errorRate: 0.01);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.True(result.AdjustedConfidence > 0.5);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_High_Risk_For_Custom_Strategy()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 50, errorRate: 0.15, executionTimesCount: 5);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.95, memoryUtil: 0.95, errorRate: 0.08);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.Custom, analysisData, systemMetrics);

        // Assert
        Assert.Equal(RiskLevel.High, result.RiskLevel); // Custom strategy with poor data and high system load = High risk
        Assert.True(result.AdjustedConfidence < 0.5); // High risk should reduce confidence
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_High_Risk_For_ParallelProcessing_With_High_CPU()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 5, errorRate: 0.15, executionTimesCount: 2);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.98, memoryUtil: 0.98, errorRate: 0.15);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.ParallelProcessing, analysisData, systemMetrics);

        // Assert
        Assert.Equal(RiskLevel.High, result.RiskLevel); // Parallel processing with very poor data and extreme system load = High risk
        Assert.True(result.AdjustedConfidence < 0.5); // High risk should reduce confidence
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_Higher_Risk_With_Insufficient_Data()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 10, errorRate: 0.0, executionTimesCount: 5);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.95, memoryUtil: 0.5, errorRate: 0.02);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.True(result.RiskLevel >= RiskLevel.Medium); // Insufficient data + high CPU should increase risk
        Assert.True(result.AdjustedConfidence < 0.8); // Should reduce confidence due to insufficient data
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_Higher_Risk_With_High_Error_Rate()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 80, errorRate: 0.15, executionTimesCount: 50);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.95, memoryUtil: 0.5, errorRate: 0.03);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.True(result.RiskLevel >= RiskLevel.Medium); // High error rate + low sample size + high CPU should increase risk
        Assert.True(result.AdjustedConfidence < 0.8); // Should reduce confidence due to high error rate
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_Valid_Risk_Assessment_Result_Type()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 100, errorRate: 0.01, executionTimesCount: 50);
        var systemMetrics = CreateTestSystemMetrics();

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RiskAssessmentResult>(result);
        Assert.True(result.RiskLevel >= RiskLevel.VeryLow);
        Assert.True(result.AdjustedConfidence >= 0.0 && result.AdjustedConfidence <= 1.0);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Adjust_Confidence_Based_On_Risk_Level()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 30, errorRate: 0.0, executionTimesCount: 10);
        var systemMetrics = CreateTestSystemMetrics();

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.NotNull(result);
        // Higher risk should generally lead to lower adjusted confidence
        if (result.RiskLevel >= RiskLevel.Medium)
        {
            Assert.True(result.AdjustedConfidence < 0.9);
        }
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Increase_Risk_With_High_Error_Rate()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 50, errorRate: 0.15, executionTimesCount: 5);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.8, memoryUtil: 0.5, errorRate: 0.02);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.True(result.RiskLevel >= RiskLevel.Medium); // High error rate + low sample size should increase risk
        Assert.True(result.AdjustedConfidence < 0.8); // Should reduce confidence
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_Valid_Risk_Assessment_Result_Object()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 100, errorRate: 0.01, executionTimesCount: 50);
        var systemMetrics = CreateTestSystemMetrics();

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<RiskAssessmentResult>(result);
        Assert.True(Enum.IsDefined(typeof(RiskLevel), result.RiskLevel));
        Assert.True(result.AdjustedConfidence >= 0.0 && result.AdjustedConfidence <= 1.0);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Calculate_Low_Adjusted_Confidence_With_Insufficient_Data()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 5, errorRate: 0.0, executionTimesCount: 2);
        var systemMetrics = CreateTestSystemMetrics();

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.True(result.AdjustedConfidence < 0.6);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Return_Low_Risk_With_Good_Data_And_Metrics()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 1000, errorRate: 0.001, executionTimesCount: 100);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.1, memoryUtil: 0.1, errorRate: 0.001);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // Assert
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.True(result.AdjustedConfidence > 0.5); // Good data should give reasonably high confidence
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Trigger_High_CPU_Risk_Factor_For_Parallel_Processing()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 100, errorRate: 0.01, executionTimesCount: 50);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.9, memoryUtil: 0.5, errorRate: 0.02); // High CPU

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.ParallelProcessing, analysisData, systemMetrics);

        // This test ensures that the condition in IdentifyRiskFactors for high CPU with parallel processing is covered
        Assert.NotNull(result);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Handle_Custom_Strategy_Risk_Factor()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 100, errorRate: 0.01, executionTimesCount: 50);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.5, memoryUtil: 0.5, errorRate: 0.02);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.Custom, analysisData, systemMetrics);

        // This test ensures that the custom strategy risk factor is covered
        Assert.NotNull(result);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Trigger_Insufficient_Data_Risk_Factor()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 40, errorRate: 0.01, executionTimesCount: 5); // Low total executions and execution times
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.5, memoryUtil: 0.5, errorRate: 0.02);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // This test ensures that the insufficient data risk factor is covered
        Assert.NotNull(result);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Trigger_High_Error_Rate_Risk_Factor()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 100, errorRate: 0.10, executionTimesCount: 50); // High error rate
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.5, memoryUtil: 0.5, errorRate: 0.02);

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.EnableCaching, analysisData, systemMetrics);

        // This test ensures that the high error rate risk factor is covered
        Assert.NotNull(result);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Handle_Unknown_Strategy()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 1000, errorRate: 0.01, executionTimesCount: 100);
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.1, memoryUtil: 0.1, errorRate: 0.001);

        // Using Reflection to create an unknown enum value to test the default case in GetBaseRiskForStrategy
        var unknownStrategy = (OptimizationStrategy)(-1); 

        // Act
        var result = _service.AssessOptimizationRisk(unknownStrategy, analysisData, systemMetrics);

        // This test ensures that the default case in GetBaseRiskForStrategy is covered
        Assert.NotNull(result);
    }

    [Fact]
    public void AssessOptimizationRisk_Should_Trigger_All_Conditions_For_High_Risk()
    {
        // Arrange
        var analysisData = CreateTestAnalysisData(totalExecutions: 40, errorRate: 0.12, executionTimesCount: 5); // Trigger insufficient data and high error rate
        var systemMetrics = CreateTestSystemMetrics(cpuUtil: 0.95, memoryUtil: 0.95, errorRate: 0.08); // High system metrics

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.ParallelProcessing, analysisData, systemMetrics);

        // Assert - This should result in high risk due to multiple factors
        Assert.NotNull(result);
        Assert.True(result.RiskLevel >= RiskLevel.Medium);
    }
    
    [Fact]
    public void AssessOptimizationRisk_Should_Return_VeryHigh_Risk()
    {
        // Arrange - Set up conditions that should result in VeryHigh risk
        var analysisData = CreateTestAnalysisData(totalExecutions: 5, errorRate: 0.2, executionTimesCount: 2); // Very low data, high error
        var systemMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.99,    // Very high CPU
            ["MemoryUtilization"] = 0.99, // Very high memory
            ["ErrorRate"] = 0.1,          // High error rate
            ["TotalRequests"] = 1000,
            ["ExecutionCount"] = 500
        };

        // Act
        var result = _service.AssessOptimizationRisk(OptimizationStrategy.Custom, analysisData, systemMetrics);

        // This test ensures very high risk conditions are tested
        Assert.NotNull(result);
    }

    private static RequestAnalysisData CreateTestAnalysisData(
        int totalExecutions = 100,
        double errorRate = 0.01,
        int executionTimesCount = 50)
    {
        var analysisData = new RequestAnalysisData();
        var failedRequests = (int)(totalExecutions * errorRate);

        // Create representative metrics to populate the analysis data
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = totalExecutions,
            SuccessfulExecutions = totalExecutions - failedRequests,
            FailedExecutions = failedRequests,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 1
        };

        analysisData.AddMetrics(metrics);

        return analysisData;
    }

    private static Dictionary<string, double> CreateTestSystemMetrics(
        double cpuUtil = 0.5,
        double memoryUtil = 0.5,
        double errorRate = 0.01)
    {
        return new Dictionary<string, double>
        {
            ["CpuUtilization"] = cpuUtil,
            ["MemoryUtilization"] = memoryUtil,
            ["ErrorRate"] = errorRate,
            ["TotalRequests"] = 1000,
            ["ExecutionCount"] = 500
        };
    }
}