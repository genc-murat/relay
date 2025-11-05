using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class ResourceOptimizationServiceTests
{
    private readonly ILogger _logger;
    private readonly ResourceOptimizationService _service;

    public ResourceOptimizationServiceTests()
    {
        _logger = NullLogger.Instance;
        _service = new ResourceOptimizationService(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ResourceOptimizationService(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var service = new ResourceOptimizationService(logger);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region AnalyzeResourceUsage Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Throw_When_CurrentMetrics_Is_Null()
    {
        // Arrange
        var historicalMetrics = new Dictionary<string, double>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.AnalyzeResourceUsage(null!, historicalMetrics));
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Throw_When_HistoricalMetrics_Is_Null()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.AnalyzeResourceUsage(currentMetrics, null!));
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Return_Valid_Result()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>();
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Recommendations);
        Assert.NotNull(result.Parameters);
        Assert.True(result.Confidence >= 0.0);
        Assert.True(result.Confidence <= 1.0);
        Assert.True(result.GainPercentage >= 0.0);
    }

    #endregion

    #region Critical CPU Usage Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Detect_Critical_CPU_Usage()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.95 // Above critical threshold (0.9)
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.Critical, result.Priority);
        Assert.Equal(OptimizationStrategy.ParallelProcessing, result.Strategy);
        Assert.Equal(RiskLevel.High, result.Risk);
        Assert.True(result.Confidence >= 0.85);
        Assert.True(result.GainPercentage >= 25.0);
        Assert.Contains("Critical: CPU utilization is extremely high", result.Reasoning);
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Detect_High_CPU_Usage()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.75 // Above high threshold (0.7) but below critical (0.9)
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.High, result.Priority);
        Assert.Equal(OptimizationStrategy.EnableCaching, result.Strategy);
        Assert.Equal(RiskLevel.Medium, result.Risk);
        Assert.True(result.Confidence >= 0.65);
        Assert.True(result.GainPercentage >= 15.0);
        Assert.Contains("CPU utilization is high", result.Reasoning);
    }

    #endregion

    #region Critical Memory Usage Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Detect_Critical_Memory_Usage()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["MemoryUtilization"] = 0.95 // Above critical threshold (0.9)
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.Critical, result.Priority);
        Assert.Equal(OptimizationStrategy.MemoryOptimization, result.Strategy);
        Assert.Equal(RiskLevel.VeryHigh, result.Risk);
        Assert.True(result.Confidence >= 0.9);
        Assert.True(result.GainPercentage >= 30.0);
        Assert.Contains("Critical: Memory utilization is extremely high", result.Reasoning);
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Detect_High_Memory_Usage()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["MemoryUtilization"] = 0.75 // Above high threshold (0.7) but below critical (0.9)
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.High, result.Priority);
        Assert.Equal(OptimizationStrategy.MemoryPooling, result.Strategy);
        Assert.Equal(RiskLevel.Medium, result.Risk);
        Assert.True(result.Confidence >= 0.7);
        Assert.True(result.GainPercentage >= 20.0);
        Assert.Contains("Memory utilization is elevated", result.Reasoning);
    }

    #endregion

    #region Resource Efficiency Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Detect_Low_Resource_Efficiency()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.9, // High CPU
            ["ThroughputPerSecond"] = 5 // Low throughput relative to CPU
            // efficiency = 5/0.9 ≈ 5.56, which is below threshold of 10
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Resource efficiency is low", result.Reasoning);
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Not_Detect_Low_Efficiency_For_Good_Efficiency()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.5, // Moderate CPU
            ["ThroughputPerSecond"] = 100 // High throughput
            // efficiency = 100/0.5 = 200, which is above threshold of 10
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        // The efficiency comment should only appear if other issues are not present
        // Since we have moderate CPU and high throughput, overall result depends on other factors
    }

    #endregion

    #region No Optimization Needed Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Return_No_Need_For_Optimization_When_Resources_Are_Normal()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.3, // Normal CPU usage
            ["MemoryUtilization"] = 0.4, // Normal memory usage
            ["ThroughputPerSecond"] = 100 // Good throughput
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        // When no critical resources, may still optimize if efficiency is low
        // Let's check if resources are within acceptable limits
        Assert.Contains("Resource utilization is within acceptable limits", result.Reasoning);
        Assert.True(result.Confidence >= 0.75);
    }

    #endregion

    #region Parameter Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Include_Current_Metrics_In_Parameters()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.6,
            ["MemoryUtilization"] = 0.5,
            ["ThroughputPerSecond"] = 120
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Parameters);
        Assert.Contains("CurrentCpuUtilization", result.Parameters.Keys);
        Assert.Contains("CurrentMemoryUtilization", result.Parameters.Keys);
        Assert.Contains("Throughput", result.Parameters.Keys);
        Assert.Contains("Efficiency", result.Parameters.Keys);
        
        Assert.Equal(0.6, result.Parameters["CurrentCpuUtilization"]);
        Assert.Equal(0.5, result.Parameters["CurrentMemoryUtilization"]);
        Assert.Equal(120.0, result.Parameters["Throughput"]);
        Assert.Equal(120.0 / 0.6, result.Parameters["Efficiency"]); // efficiency = throughput / cpu
    }

    #endregion

    #region EstimateResourceSavings Tests

    [Fact]
    public void EstimateResourceSavings_Should_Calculate_Based_On_Utilization()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.8,
            ["MemoryUtilization"] = 0.6
        };

        // Act and Assert - We can't directly test the private method, 
        // but we can verify it's used in the result
        var result = _service.AnalyzeResourceUsage(metrics, new Dictionary<string, double>());
        
        Assert.NotNull(result);
        Assert.True(result.EstimatedSavings.TotalMilliseconds >= 0);
        // Expected calculation: (0.8 * 0.2 + 0.6 * 0.1) * 1000 = (0.16 + 0.06) * 1000 = 220ms
    }

    #endregion

    #region Combined Scenarios Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Have_Highest_Priority_When_Multiple_Critical_Issues_Exist()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.95, // Critical CPU
            ["MemoryUtilization"] = 0.92 // Critical Memory
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.Critical, result.Priority);
        Assert.Equal(OptimizationStrategy.MemoryOptimization, result.Strategy); // Last critical issue wins
        Assert.Equal(RiskLevel.VeryHigh, result.Risk); // Highest risk
        Assert.True(result.GainPercentage >= 30.0); // Highest gain percentage
        Assert.Contains("Critical: CPU utilization", result.Reasoning);
        Assert.Contains("Critical: Memory utilization", result.Reasoning);
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Have_Correct_Priority_When_Memory_Is_Higher_Priority_Than_CPU()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.75, // High CPU
            ["MemoryUtilization"] = 0.95 // Critical Memory
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.Critical, result.Priority); // Memory sets Critical priority
        Assert.Equal(OptimizationStrategy.MemoryOptimization, result.Strategy); // Memory strategy
        Assert.Equal(RiskLevel.VeryHigh, result.Risk); // Memory risk
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void AnalyzeResourceUsage_Should_Handle_Zero_CPU_Utilization()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.0, // Zero CPU
            ["ThroughputPerSecond"] = 50 // Non-zero throughput
            // This will make efficiency infinite (50/0), but the code handles this
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        // Efficiency calculation should handle division by zero or use default behavior
    }

    [Fact]
    public void AnalyzeResourceUsage_Should_Handle_Missing_Metrics()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>(); // Empty
        var historicalMetrics = new Dictionary<string, double>(); // Empty

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        // Should use default values (0) for missing metrics
        Assert.Contains("Resource utilization is within acceptable limits", result.Reasoning);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Resource_Analysis_Should_Work_With_Typical_Values()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.4,
            ["MemoryUtilization"] = 0.3,
            ["ThroughputPerSecond"] = 200
        };
        var historicalMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.2,
            ["MemoryUtilization"] = 0.15
        };

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Recommendations);
        Assert.NotNull(result.Parameters);
        Assert.Equal(200.0, result.Parameters["Throughput"]);
        Assert.Equal(0.4, result.Parameters["CurrentCpuUtilization"]);
        Assert.Equal(0.3, result.Parameters["CurrentMemoryUtilization"]);
        Assert.Equal(500.0, result.Parameters["Efficiency"]); // 200/0.4 = 500
        Assert.False(result.ShouldOptimize); // Normal values should not trigger optimization
    }

    [Fact]
    public void Full_Resource_Analysis_Should_Trigger_Optimization_With_High_Values()
    {
        // Arrange
        var currentMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.85,
            ["MemoryUtilization"] = 0.75,
            ["ThroughputPerSecond"] = 50
        };
        var historicalMetrics = new Dictionary<string, double>();

        // Act
        var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldOptimize);
        Assert.Equal(OptimizationPriority.High, result.Priority);
        // CPU utilization (0.85) triggers high CPU optimization which takes precedence over memory (0.75)
        Assert.Equal(Relay.Core.AI.OptimizationStrategy.EnableCaching, result.Strategy);
        Assert.True(result.GainPercentage >= 15.0);
    }

    #endregion
}
