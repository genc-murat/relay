using Relay.Core.AI;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIOptimizationEngineInsightsTests : AIOptimizationEngineTestBase
{
    [Fact]
    public async Task GetSystemInsightsAsync_Should_Throw_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var timeWindow = TimeSpan.FromHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.GetSystemInsightsAsync(timeWindow));
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Return_Insights()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(1);

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights);
        Assert.Equal(timeWindow, insights.AnalysisPeriod);
        Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Handle_Zero_Time_Window()
    {
        // Arrange
        var timeWindow = TimeSpan.Zero;

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights);
        Assert.Equal(timeWindow, insights.AnalysisPeriod);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Handle_Large_Time_Window()
    {
        // Arrange
        var timeWindow = TimeSpan.FromDays(365);

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights);
        Assert.Equal(timeWindow, insights.AnalysisPeriod);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Handle_Negative_Time_Window()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(-1);

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Provide_Consistent_Results()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(1);

        // Act
        var insights1 = await _engine.GetSystemInsightsAsync(timeWindow);
        var insights2 = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights1);
        Assert.NotNull(insights2);
        Assert.Equal(insights1.AnalysisPeriod, insights2.AnalysisPeriod);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Include_All_Required_Components()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(2);

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights);
        Assert.NotNull(insights.Bottlenecks);
        Assert.NotNull(insights.Opportunities);
        Assert.NotNull(insights.Predictions);
        Assert.NotNull(insights.KeyMetrics);
        Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        Assert.True(insights.PerformanceGrade >= 'A' && insights.PerformanceGrade <= 'F');
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Return_No_Bottlenecks_In_Normal_Conditions()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(1);

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights.Bottlenecks);
        // In test environment with low system load, no bottlenecks should be detected
        Assert.Empty(insights.Bottlenecks);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Detect_CPU_Bottleneck_When_High_Utilization()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(1);

        // Use reflection to access the internal SystemMetricsService and set high CPU
        var engineType = typeof(AIOptimizationEngine);
        var systemMetricsField = engineType.GetField("_systemMetricsService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var systemMetricsService = systemMetricsField?.GetValue(_engine) as SystemMetricsService;

        // Set high CPU utilization
        systemMetricsService?.SetTestMetrics(new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.95 // Above 0.8 threshold
        });

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights.Bottlenecks);
        Assert.Single(insights.Bottlenecks);
        var bottleneck = insights.Bottlenecks[0];
        Assert.Equal("CPU", bottleneck.Component);
        Assert.Equal(BottleneckSeverity.Critical, bottleneck.Severity);
        Assert.Contains("High CPU utilization", bottleneck.Description);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Detect_Memory_Bottleneck_When_High_Utilization()
    {
        // Arrange
        var timeWindow = TimeSpan.FromHours(1);

        // Use reflection to access the internal SystemMetricsService and set high memory
        var engineType = typeof(AIOptimizationEngine);
        var systemMetricsField = engineType.GetField("_systemMetricsService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var systemMetricsService = systemMetricsField?.GetValue(_engine) as SystemMetricsService;

            // Set high memory utilization
            systemMetricsService?.SetTestMetrics(new Dictionary<string, double>
            {
                ["MemoryUtilization"] = 0.96 // Above 0.95 threshold for Critical
            });

        // Act
        var insights = await _engine.GetSystemInsightsAsync(timeWindow);

        // Assert
        Assert.NotNull(insights.Bottlenecks);
        Assert.Single(insights.Bottlenecks);
        var bottleneck = insights.Bottlenecks[0];
        Assert.Equal("Memory", bottleneck.Component);
        Assert.Equal(BottleneckSeverity.Critical, bottleneck.Severity);
        Assert.Contains("High memory utilization", bottleneck.Description);
    }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Detect_Error_Rate_Bottleneck_When_High_Rate()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Use reflection to access the internal SystemMetricsService and set high error rate
            var engineType = typeof(AIOptimizationEngine);
            var systemMetricsField = engineType.GetField("_systemMetricsService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var systemMetricsService = systemMetricsField?.GetValue(_engine) as SystemMetricsService;

            // Set high error rate
            systemMetricsService?.SetTestMetrics(new Dictionary<string, double>
            {
                ["ErrorRate"] = 0.15 // Above 0.05 threshold
            });

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights.Bottlenecks);
            Assert.Single(insights.Bottlenecks);
            var bottleneck = insights.Bottlenecks[0];
            Assert.Equal("Application", bottleneck.Component);
            Assert.Equal(BottleneckSeverity.Critical, bottleneck.Severity);
            Assert.Contains("High error rate", bottleneck.Description);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Detect_Caching_Opportunity_When_High_Repeat_Rate()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Use reflection to access the internal SystemMetricsService and set high repeat rate
            var engineType = typeof(AIOptimizationEngine);
            var systemMetricsField = engineType.GetField("_systemMetricsService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var systemMetricsService = systemMetricsField?.GetValue(_engine) as SystemMetricsService;

            // Set high repeat rate, and ensure other opportunities are not triggered
            systemMetricsService?.SetTestMetrics(new Dictionary<string, double>
            {
                ["AverageRepeatRate"] = 0.4, // Above 0.3 threshold
                ["AverageBatchSize"] = 6.0, // Above 5 to avoid batching opportunity
                ["DatabasePoolUtilization"] = 0.8 // Below 0.9 to avoid connection opportunity
            });

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights.Opportunities);
            Assert.Single(insights.Opportunities);
            var opportunity = insights.Opportunities[0];
            Assert.Equal("Implement Response Caching", opportunity.Title);
            Assert.Contains("caching opportunity", opportunity.Description);
            Assert.Equal(OptimizationPriority.High, opportunity.Priority);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Detect_Batching_Opportunity_When_Low_Batch_Size()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Use reflection to access the internal SystemMetricsService and set low batch size
            var engineType = typeof(AIOptimizationEngine);
            var systemMetricsField = engineType.GetField("_systemMetricsService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var systemMetricsService = systemMetricsField?.GetValue(_engine) as SystemMetricsService;

            // Set low batch size, and ensure other opportunities are not triggered
            systemMetricsService?.SetTestMetrics(new Dictionary<string, double>
            {
                ["AverageBatchSize"] = 3.0, // Below 5 threshold
                ["AverageRepeatRate"] = 0.2, // Below 0.3 to avoid caching opportunity
                ["DatabasePoolUtilization"] = 0.8 // Below 0.9 to avoid connection opportunity
            });

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights.Opportunities);
            Assert.Single(insights.Opportunities);
            var opportunity = insights.Opportunities[0];
            Assert.Equal("Implement Request Batching", opportunity.Title);
            Assert.Contains("batching optimization", opportunity.Description);
            Assert.Equal(OptimizationPriority.Medium, opportunity.Priority);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Detect_Connection_Pooling_Opportunity_When_High_Utilization()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Use reflection to access the internal SystemMetricsService and set high connection utilization
            var engineType = typeof(AIOptimizationEngine);
            var systemMetricsField = engineType.GetField("_systemMetricsService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var systemMetricsService = systemMetricsField?.GetValue(_engine) as SystemMetricsService;

            // Set high connection utilization, and ensure other opportunities are not triggered
            systemMetricsService?.SetTestMetrics(new Dictionary<string, double>
            {
                ["DatabasePoolUtilization"] = 0.95, // Above 0.9 threshold
                ["AverageBatchSize"] = 6.0, // Above 5 to avoid batching opportunity
                ["AverageRepeatRate"] = 0.2 // Below 0.3 to avoid caching opportunity
            });

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights.Opportunities);
            Assert.Single(insights.Opportunities);
            var opportunity = insights.Opportunities[0];
            Assert.Equal("Optimize Database Connection Pooling", opportunity.Title);
            Assert.Contains("connection utilization", opportunity.Description);
            Assert.Equal(OptimizationPriority.Medium, opportunity.Priority);
        }
}