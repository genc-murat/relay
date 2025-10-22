using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization;

public class SystemInsightsStrategyTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Fact]
    public void SystemInsightsStrategy_ShouldHandleAnalyzeSystemInsightsOperation()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.7,
                MemoryUtilization = 0.5,
                ActiveConnections = 100,
                QueuedRequestCount = 5
            }
        };

        // Act
        var canHandle = strategy.CanHandle(context.Operation);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public async Task SystemInsightsStrategy_ShouldReturnSystemOptimizationRecommendation()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.9, // High CPU
                MemoryUtilization = 0.2,
                ActiveConnections = 200,
                QueuedRequestCount = 50
            }
        };

        // Act
        var result = await strategy.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        Assert.IsType<OptimizationRecommendation>(result.Data);
        var recommendation = (OptimizationRecommendation)result.Data!;
        Assert.Contains("insights", recommendation.Parameters.Keys);

        // Verify insights are TrendInsight objects
        var insights = recommendation.Parameters["insights"] as List<TrendInsight>;
        Assert.NotNull(insights);
        Assert.True(insights.Count > 0);
    }

    [Fact]
    public async Task SystemInsightsStrategy_ShouldGenerateCriticalInsightForHighCPU()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.96, // Critical CPU (>95%)
                MemoryUtilization = 0.2,
                ActiveConnections = 50,
                QueuedRequestCount = 5
            }
        };

        // Act
        var result = await strategy.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var recommendation = (OptimizationRecommendation)result.Data!;
        var insights = recommendation.Parameters["insights"] as List<TrendInsight>;
        Assert.NotNull(insights);

        var criticalInsights = insights.Where(i => i.Severity == InsightSeverity.Critical);
        Assert.True(criticalInsights.Any(), "Should generate critical insight for CPU > 95%");

        var cpuInsight = criticalInsights.FirstOrDefault(i => i.Message.Contains("CPU"));
        Assert.NotNull(cpuInsight);
        Assert.Contains("CPU Utilization is at critical level", cpuInsight.Message);
    }

    [Fact]
    public async Task SystemInsightsStrategy_ShouldGenerateWarningInsightForElevatedCPU()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.85, // Warning CPU (80-95%)
                MemoryUtilization = 0.2,
                ActiveConnections = 50,
                QueuedRequestCount = 5
            }
        };

        // Act
        var result = await strategy.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var recommendation = (OptimizationRecommendation)result.Data!;
        var insights = recommendation.Parameters["insights"] as List<TrendInsight>;
        Assert.NotNull(insights);

        var warningInsights = insights.Where(i => i.Severity == InsightSeverity.Warning);
        Assert.True(warningInsights.Any(), "Should generate warning insight for CPU 80-95%");

        var cpuInsight = warningInsights.FirstOrDefault(i => i.Message.Contains("CPU"));
        Assert.NotNull(cpuInsight);
        Assert.Contains("CPU Utilization is elevated", cpuInsight.Message);
    }

    [Fact]
    public async Task SystemInsightsStrategy_ShouldGenerateCriticalInsightForHighQueue()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.5,
                ActiveConnections = 50,
                QueuedRequestCount = 150 // Critical queue (>100)
            }
        };

        // Act
        var result = await strategy.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var recommendation = (OptimizationRecommendation)result.Data!;
        var insights = recommendation.Parameters["insights"] as List<TrendInsight>;
        Assert.NotNull(insights);

        var criticalInsights = insights.Where(i => i.Severity == InsightSeverity.Critical);
        Assert.True(criticalInsights.Any(), "Should generate critical insight for queue > 100");

        var queueInsight = criticalInsights.FirstOrDefault(i => i.Message.Contains("Queued"));
        Assert.NotNull(queueInsight);
        Assert.Contains("batching", queueInsight.RecommendedAction);
    }

    [Fact]
    public async Task SystemInsightsStrategy_ShouldGenerateInfoInsightForNormalLoad()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.3, // Normal CPU
                MemoryUtilization = 0.4, // Normal memory
                ActiveConnections = 50, // Normal connections
                QueuedRequestCount = 5 // Normal queue
            }
        };

        // Act
        var result = await strategy.ExecuteAsync(context);

        // Assert
        Assert.True(result.Success);
        var recommendation = (OptimizationRecommendation)result.Data!;
        var insights = recommendation.Parameters["insights"] as List<TrendInsight>;
        Assert.NotNull(insights);

        // Should have info-level insights for normal operation
        var infoInsights = insights.Where(i => i.Severity == InsightSeverity.Info);
        Assert.True(infoInsights.Any(), "Should generate info insights for normal operation");
    }

    [Fact]
    public async Task SystemInsightsStrategy_ShouldHandleNullSystemLoad()
    {
        // Arrange
        var strategy = new SystemInsightsStrategy(_logger);
        var context = new OptimizationContext
        {
            Operation = "AnalyzeSystemInsights",
            SystemLoad = null // Null system load
        };

        // Act
        var result = await strategy.ExecuteAsync(context);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("System load metrics are required", result.ErrorMessage);
    }
}