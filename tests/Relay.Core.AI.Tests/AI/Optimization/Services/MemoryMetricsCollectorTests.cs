using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class MemoryMetricsCollectorTests
{
    private readonly ILogger _logger;
    private readonly MemoryMetricsCollector _collector;

    public MemoryMetricsCollectorTests()
    {
        _logger = NullLogger.Instance;
        _collector = new MemoryMetricsCollector(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new MemoryMetricsCollector(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var collector = new MemoryMetricsCollector(logger);

        // Assert
        Assert.NotNull(collector);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Name_Should_Return_Correct_Value()
    {
        // Act
        var name = _collector.Name;

        // Assert
        Assert.Equal("MemoryMetricsCollector", name);
    }

    [Fact]
    public void SupportedTypes_Should_Return_Gauge_Type()
    {
        // Act
        var supportedTypes = _collector.SupportedTypes;

        // Assert
        Assert.NotNull(supportedTypes);
        Assert.Contains(Relay.Core.AI.Optimization.Services.MetricType.Gauge, supportedTypes);
        Assert.Single(supportedTypes);
    }

    [Fact]
    public void CollectionInterval_Should_Return_Five_Seconds()
    {
        // Act
        var interval = _collector.CollectionInterval;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), interval);
    }

    #endregion

    #region CollectMetricsCore Tests

    [Fact]
    public void CollectMetricsCore_Should_Return_Memory_Utilization_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        Assert.NotNull(metrics);
        var metricsList = metrics.ToList();
        Assert.Equal(3, metricsList.Count);

        // Check MemoryUtilization metric
        var utilizationMetric = metricsList[0];
        Assert.Equal("MemoryUtilization", utilizationMetric.Name);
        Assert.Equal("ratio", utilizationMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, utilizationMetric.Type);
        Assert.True(utilizationMetric.Value >= 0.0);
        Assert.True(utilizationMetric.Value <= 1.0);

        // Check MemoryUsageMB metric
        var usageMetric = metricsList[1];
        Assert.Equal("MemoryUsageMB", usageMetric.Name);
        Assert.Equal("MB", usageMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, usageMetric.Type);
        Assert.True(usageMetric.Value >= 0.0);

        // Check AvailableMemoryMB metric
        var availableMetric = metricsList[2];
        Assert.Equal("AvailableMemoryMB", availableMetric.Name);
        Assert.Equal("MB", availableMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, availableMetric.Type);
        Assert.True(availableMetric.Value >= 0.0);
    }

    [Fact]
    public void CollectMetricsCore_Should_Maintain_Consistency_Between_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var utilization = metrics.First(m => m.Name == "MemoryUtilization").Value;
        var usageMB = metrics.First(m => m.Name == "MemoryUsageMB").Value;
        var availableMB = metrics.First(m => m.Name == "AvailableMemoryMB").Value;

        // Assert
        // Verify that utilization is consistent with usage vs assumed total (8192MB)
        var calculatedUtilization = usageMB / 8192.0;
        Assert.Equal(calculatedUtilization, utilization, 3); // Allow for rounding differences
        
        // Verify that usage + available is less than or equal to total
        Assert.True(usageMB + availableMB <= 8192.0);
    }

    #endregion

    #region GetMemoryInfo Calculation Tests

    [Fact]
    public void GetMemoryInfo_Should_Return_Valid_Values()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var utilization = metrics.First(m => m.Name == "MemoryUtilization").Value;
        var usageMB = metrics.First(m => m.Name == "MemoryUsageMB").Value;
        var availableMB = metrics.First(m => m.Name == "AvailableMemoryMB").Value;

        // Assert
        Assert.True(utilization >= 0.0);
        Assert.True(utilization <= 1.0);
        Assert.True(usageMB >= 0.0);
        Assert.True(availableMB >= 0.0);
    }

    [Fact]
    public void AvailableMemory_Should_Not_Be_Negative()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var availableMB = metrics.First(m => m.Name == "AvailableMemoryMB").Value;

        // Assert
        Assert.True(availableMB >= 0.0); // Should be clamped to 0 or higher
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void CollectMetrics_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<System.Threading.Tasks.Task>();
        var results = new List<IEnumerable<Relay.Core.AI.Optimization.Services.MetricValue>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                var metrics = _collector.CollectMetrics();
                lock (results)
                {
                    results.Add(metrics);
                }
            });
            tasks.Add(task);
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(5, results.Count);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            var metricsList = result.ToList();
            Assert.Equal(3, metricsList.Count);
            Assert.Equal("MemoryUtilization", metricsList[0].Name);
            Assert.Equal("MemoryUsageMB", metricsList[1].Name);
            Assert.Equal("AvailableMemoryMB", metricsList[2].Name);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void CollectMetrics_Should_Handle_Exceptions_Gracefully()
    {
        // Act & Assert - Should not throw
        var metrics = _collector.CollectMetrics();
        Assert.NotNull(metrics);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Collect_Lifecycle_Should_Work()
    {
        // Arrange - Collect metrics multiple times 
        var collectedMetrics = new List<IEnumerable<Relay.Core.AI.Optimization.Services.MetricValue>>();

        // Act - Collect several times
        for (int i = 0; i < 3; i++)
        {
            var metrics = _collector.CollectMetrics();
            collectedMetrics.Add(metrics);
            System.Threading.Thread.Sleep(10); // Small delay
        }

        // Assert
        Assert.Equal(3, collectedMetrics.Count);
        foreach (var metrics in collectedMetrics)
        {
            var metricsList = metrics.ToList();
            Assert.Equal(3, metricsList.Count);
            Assert.Equal("MemoryUtilization", metricsList[0].Name);
            Assert.Equal("MemoryUsageMB", metricsList[1].Name);
            Assert.Equal("AvailableMemoryMB", metricsList[2].Name);
            
            // Validate values are within expected ranges
            var utilization = metricsList[0].Value;
            var usageMB = metricsList[1].Value;
            var availableMB = metricsList[2].Value;
            
            Assert.True(utilization >= 0.0 && utilization <= 1.0);
            Assert.True(usageMB >= 0.0);
            Assert.True(availableMB >= 0.0);
        }
    }

    #endregion
}
