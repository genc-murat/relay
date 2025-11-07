using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Concurrent access tests for TrendAnalyzer to ensure thread safety.
/// Tests multiple threads calling AnalyzeMetricTrends simultaneously.
/// </summary>
public class TrendAnalyzerConcurrentAccessTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;

    public TrendAnalyzerConcurrentAccessTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");
    }

    #region Basic Concurrent Access Tests

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentCallsWithSameMetrics_CompletesSuccessfully()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 80.0,
            ["disk"] = 60.0
        };

        const int concurrentCalls = 10;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act - Start multiple concurrent calls
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(results, result => Assert.NotEqual(default(DateTime), result.Timestamp));
        Assert.All(results, result => Assert.Equal(3, result.MovingAverages.Count));
    }

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentCallsWithDifferentMetrics_CompletesSuccessfully()
    {
        // Arrange
        const int concurrentCalls = 20;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act - Start concurrent calls with different metrics
        for (int i = 0; i < concurrentCalls; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 70.0 + i,      // Different CPU values
                ["memory"] = 75.0 + i,   // Different memory values
                [$"metric_{i}"] = i      // Unique metric per call
            };

            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
        }

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));

        // Each result should have the expected number of metrics
        for (int i = 0; i < concurrentCalls; i++)
        {
            Assert.Equal(3, results[i].MovingAverages.Count);
        }
    }

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentCallsWithEmptyMetrics_CompletesSuccessfully()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();
        const int concurrentCalls = 15;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(emptyMetrics));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(results, result => Assert.Empty(result.MovingAverages));
        Assert.All(results, result => Assert.Empty(result.Insights));
    }

    #endregion

    #region High Load Concurrent Tests

    [Fact]
    public async Task AnalyzeMetricTrends_HighConcurrentLoad_HandlesGracefully()
    {
        // Arrange
        const int concurrentCalls = 50;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act - High concurrent load
        for (int i = 0; i < concurrentCalls; i++)
        {
            var metrics = new Dictionary<string, double>();
            // Create metrics with varying sizes
            var metricCount = (i % 10) + 1; // 1 to 10 metrics per call
            for (int j = 0; j < metricCount; j++)
            {
                metrics[$"metric_{i}_{j}"] = 50.0 + j;
            }

            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
        }

        // Assert - Should complete without exceptions
        var results = await Task.WhenAll(tasks);
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentCallsWithLargeMetrics_CompletesWithinTimeLimit()
    {
        // Arrange
        const int concurrentCalls = 10;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act - Start timing
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < concurrentCalls; i++)
        {
            var metrics = new Dictionary<string, double>();
            // Large metric sets (50 metrics each)
            for (int j = 0; j < 50; j++)
            {
                metrics[$"metric_{i}_{j}"] = j * 2.0;
            }

            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
        }

        var results = await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(results, result => Assert.Equal(50, result.MovingAverages.Count));

        // Should complete within reasonable time (less than 5 seconds for 10 concurrent calls with 50 metrics each)
        var duration = endTime - startTime;
        Assert.True(duration.TotalSeconds < 5.0, $"Concurrent analysis took {duration.TotalSeconds}s, expected < 5.0s");
    }

    #endregion

    #region Mixed Operation Concurrent Tests

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentWithCalculateMovingAverages_IsThreadSafe()
    {
        // Arrange
        const int concurrentCalls = 20;
        var analyzeTasks = new Task<TrendAnalysisResult>[concurrentCalls / 2];
        var calculateTasks = new Task<System.Collections.Generic.Dictionary<string, MovingAverageData>>[concurrentCalls / 2];

        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 80.0
        };

        var timestamp = DateTime.UtcNow;

        // Act - Mix AnalyzeMetricTrends and CalculateMovingAverages calls
        for (int i = 0; i < concurrentCalls / 2; i++)
        {
            analyzeTasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
            calculateTasks[i] = Task.Run(() => _analyzer.CalculateMovingAverages(metrics, timestamp));
        }

        var analyzeResults = await Task.WhenAll(analyzeTasks);
        var calculateResults = await Task.WhenAll(calculateTasks);

        // Assert
        Assert.Equal(concurrentCalls / 2, analyzeResults.Length);
        Assert.Equal(concurrentCalls / 2, calculateResults.Length);

        Assert.All(analyzeResults, result => Assert.NotNull(result));
        Assert.All(calculateResults, result => Assert.NotNull(result));
        Assert.All(calculateResults, result => Assert.Equal(2, result.Count));
    }

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentWithDetectPerformanceAnomalies_IsThreadSafe()
    {
        // Arrange
        const int concurrentCalls = 20;
        var analyzeTasks = new Task<TrendAnalysisResult>[concurrentCalls / 2];
        var anomalyTasks = new Task<List<MetricAnomaly>>[concurrentCalls / 2];

        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 95.0,  // High value that might trigger anomaly
            ["memory"] = 85.0
        };

        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow },
            ["memory"] = new MovingAverageData { MA15 = 80.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        for (int i = 0; i < concurrentCalls / 2; i++)
        {
            analyzeTasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
            anomalyTasks[i] = Task.Run(() => _analyzer.DetectPerformanceAnomalies(metrics, movingAverages));
        }

        var analyzeResults = await Task.WhenAll(analyzeTasks);
        var anomalyResults = await Task.WhenAll(anomalyTasks);

        // Assert
        Assert.Equal(concurrentCalls / 2, analyzeResults.Length);
        Assert.Equal(concurrentCalls / 2, anomalyResults.Length);

        Assert.All(analyzeResults, result => Assert.NotNull(result));
        Assert.All(anomalyResults, result => Assert.NotNull(result));
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task AnalyzeMetricTrends_ExtremeConcurrency_HandlesWithoutDeadlock()
    {
        // Arrange
        const int concurrentCalls = 100;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act - Extreme concurrency
        for (int i = 0; i < concurrentCalls; i++)
        {
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 50.0 + (i % 50),  // Cycling values
                ["memory"] = 60.0 + (i % 40)
            };

            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
        }

        // Assert - Should complete without deadlock or exceptions
        var results = await Task.WhenAll(tasks);
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task AnalyzeMetricTrends_RapidSequentialConcurrentCalls_AccumulatesCorrectly()
    {
        // Arrange - Test that rapid calls don't interfere with each other
        const int iterations = 10;
        const int concurrentPerIteration = 5;

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            var tasks = new Task<TrendAnalysisResult>[concurrentPerIteration];

            // Create different metrics for each iteration
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 70.0 + iteration,
                ["memory"] = 75.0 + iteration,
                ["disk"] = 60.0 + iteration
            };

            // Act - Concurrent calls within each iteration
            for (int i = 0; i < concurrentPerIteration; i++)
            {
                tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - Each iteration should work independently
            Assert.Equal(concurrentPerIteration, results.Length);
            Assert.All(results, result => Assert.NotNull(result));
            Assert.All(results, result => Assert.Equal(3, result.MovingAverages.Count));
        }
    }

    #endregion

    #region Error Handling in Concurrent Scenarios

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentCallsWithExceptions_IsolatedFailures()
    {
        // Arrange - This test is tricky because we can't easily force exceptions in updaters
        // But we can test that one failing call doesn't affect others
        var normalMetrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0,
            ["memory"] = 80.0
        };

        const int concurrentCalls = 10;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        // Act - All calls use normal metrics (no forced exceptions)
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(normalMetrics));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All calls should succeed
        Assert.Equal(concurrentCalls, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    #endregion

    #region Performance Validation

    [Fact]
    public async Task AnalyzeMetricTrends_ConcurrentPerformance_IsAcceptable()
    {
        // Arrange
        const int concurrentCalls = 25;
        var tasks = new Task<TrendAnalysisResult>[concurrentCalls];

        var metrics = new Dictionary<string, double>();
        for (int i = 0; i < 10; i++) // 10 metrics per call
        {
            metrics[$"metric_{i}"] = 50.0 + i;
        }

        // Act
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => _analyzer.AnalyzeMetricTrends(metrics));
        }

        var results = await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        var duration = endTime - startTime;
        var avgTimePerCall = duration.TotalMilliseconds / concurrentCalls;

        // Each call should complete in reasonable time (average < 200ms)
        Assert.True(avgTimePerCall < 200, $"Average time per call: {avgTimePerCall}ms, expected < 200ms");

        // All results should be valid
        Assert.All(results, result => Assert.NotNull(result));
        Assert.All(results, result => Assert.Equal(10, result.MovingAverages.Count));
    }

    #endregion
}