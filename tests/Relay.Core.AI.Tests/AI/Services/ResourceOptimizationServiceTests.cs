using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using Xunit;
using Relay.Core.AI;

namespace Relay.Core.Tests.AI.Services
{
    public class ResourceOptimizationServiceTests
    {
        private readonly Mock<ILogger<ResourceOptimizationService>> _loggerMock;
        private readonly ResourceOptimizationService _service;

        public ResourceOptimizationServiceTests()
        {
            _loggerMock = new Mock<ILogger<ResourceOptimizationService>>();
            _service = new ResourceOptimizationService(_loggerMock.Object);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithNullCurrentMetrics_ThrowsArgumentNullException()
        {
            // Arrange
            var historicalMetrics = new Dictionary<string, double>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _service.AnalyzeResourceUsage(null!, historicalMetrics));
        }

        [Fact]
        public void AnalyzeResourceUsage_WithNullHistoricalMetrics_ThrowsArgumentNullException()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _service.AnalyzeResourceUsage(currentMetrics, null!));
        }

        [Fact]
        public void AnalyzeResourceUsage_WithNormalMetrics_ReturnsNoOptimization()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.3,
                ["MemoryUtilization"] = 0.4,
                ["ThroughputPerSecond"] = 100
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.25,
                ["MemoryUtilization"] = 0.35
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.False(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.None, result.Strategy);
            Assert.Equal(RiskLevel.Low, result.Risk);
            Assert.Equal(OptimizationPriority.Medium, result.Priority);
            Assert.True(result.Confidence >= 0.8);
            Assert.Contains("Resource utilization is within acceptable limits. No optimization needed.", result.Recommendations);
            Assert.True(result.EstimatedSavings >= TimeSpan.Zero);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithCriticalCpuUsage_ReturnsCriticalOptimization()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.95,
                ["MemoryUtilization"] = 0.3,
                ["ThroughputPerSecond"] = 50
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.9,
                ["MemoryUtilization"] = 0.25
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.ParallelProcessing, result.Strategy);
            Assert.Equal(RiskLevel.High, result.Risk);
            Assert.Equal(OptimizationPriority.Critical, result.Priority);
            Assert.Equal(0.9, result.Confidence);
            Assert.Equal(25.0, result.GainPercentage);
            Assert.Contains("Critical: CPU utilization is extremely high. Consider immediate scaling.", result.Recommendations);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithHighCpuUsage_ReturnsHighPriorityOptimization()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.8,
                ["MemoryUtilization"] = 0.3,
                ["ThroughputPerSecond"] = 75
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.75,
                ["MemoryUtilization"] = 0.25
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.EnableCaching, result.Strategy);
            Assert.Equal(RiskLevel.Medium, result.Risk);
            Assert.Equal(OptimizationPriority.High, result.Priority);
            Assert.Equal(0.7, result.Confidence);
            Assert.Equal(15.0, result.GainPercentage);
            Assert.Contains("CPU utilization is high. Monitor for potential bottlenecks.", result.Recommendations);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithCriticalMemoryUsage_ReturnsMemoryOptimization()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.3,
                ["MemoryUtilization"] = 0.95,
                ["ThroughputPerSecond"] = 50
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.25,
                ["MemoryUtilization"] = 0.9
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.MemoryOptimization, result.Strategy);
            Assert.Equal(RiskLevel.VeryHigh, result.Risk);
            Assert.Equal(OptimizationPriority.Critical, result.Priority);
            Assert.Equal(0.95, result.Confidence);
            Assert.True(result.GainPercentage >= 30.0);
            Assert.Contains("Critical: Memory utilization is extremely high. Check for memory leaks.", result.Recommendations);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithHighMemoryUsage_ReturnsMemoryPooling()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.3,
                ["MemoryUtilization"] = 0.8,
                ["ThroughputPerSecond"] = 75
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.25,
                ["MemoryUtilization"] = 0.75
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.MemoryPooling, result.Strategy);
            Assert.Equal(RiskLevel.Medium, result.Risk);
            Assert.Equal(OptimizationPriority.High, result.Priority);
            Assert.True(result.Confidence >= 0.75);
            Assert.True(result.GainPercentage >= 20.0);
            Assert.Contains("Memory utilization is elevated. Consider memory optimization.", result.Recommendations);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithLowEfficiency_ReturnsBatchProcessing()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.5, // Not high enough to trigger CPU optimization
                ["MemoryUtilization"] = 0.3,
                ["ThroughputPerSecond"] = 4 // Low efficiency: 4/0.5 = 8 < 10
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.45,
                ["MemoryUtilization"] = 0.25
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.BatchProcessing, result.Strategy);
            Assert.Equal(RiskLevel.Low, result.Risk);
            Assert.Equal(0.6, result.Confidence);
            Assert.Equal(10.0, result.GainPercentage);
            Assert.Contains("Resource efficiency is low. Consider optimizing request processing.", result.Recommendations);
        }

        [Fact]
        public void AnalyzeResourceUsage_ReturnsCorrectParameters()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.6,
                ["MemoryUtilization"] = 0.5,
                ["ThroughputPerSecond"] = 100
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.55,
                ["MemoryUtilization"] = 0.45
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            Assert.NotNull(result.Parameters);
            Assert.Equal(0.6, result.Parameters["CurrentCpuUtilization"]);
            Assert.Equal(0.5, result.Parameters["CurrentMemoryUtilization"]);
            Assert.Equal(100.0, result.Parameters["Throughput"]);
            Assert.True((double)result.Parameters["Efficiency"] > 0);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithZeroCpu_CalculatesEfficiencyCorrectly()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.0,
                ["MemoryUtilization"] = 0.3,
                ["ThroughputPerSecond"] = 100
            };
            var historicalMetrics = new Dictionary<string, double>
            {
                ["CpuUtilization"] = 0.0,
                ["MemoryUtilization"] = 0.25
            };

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            // When CPU is 0, efficiency is 0 which triggers low efficiency optimization
            Assert.True(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.BatchProcessing, result.Strategy);
            Assert.Equal(0.0, result.Parameters["Efficiency"]);
        }

        [Fact]
        public void AnalyzeResourceUsage_WithMissingMetrics_UsesDefaultValues()
        {
            // Arrange
            var currentMetrics = new Dictionary<string, double>(); // Empty metrics
            var historicalMetrics = new Dictionary<string, double>();

            // Act
            var result = _service.AnalyzeResourceUsage(currentMetrics, historicalMetrics);

            // Assert
            // With missing metrics (all 0), there's no activity so no optimization should be needed
            Assert.False(result.ShouldOptimize);
            Assert.Equal(OptimizationStrategy.None, result.Strategy);
            Assert.Equal(0.0, result.Parameters["CurrentCpuUtilization"]);
            Assert.Equal(0.0, result.Parameters["CurrentMemoryUtilization"]);
            Assert.Equal(0.0, result.Parameters["Throughput"]);
            Assert.Equal(0.0, result.Parameters["Efficiency"]);
            Assert.Contains("Resource utilization is within acceptable limits", result.Reasoning);
        }
    }
}