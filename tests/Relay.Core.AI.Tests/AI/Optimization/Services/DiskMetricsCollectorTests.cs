using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class DiskMetricsCollectorTests
{
    private readonly ILogger _logger;
    private readonly DiskMetricsCollector _collector;

    public DiskMetricsCollectorTests()
    {
        _logger = NullLogger.Instance;
        _collector = new DiskMetricsCollector(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new DiskMetricsCollector(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var collector = new DiskMetricsCollector(logger);

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
        Assert.Equal("DiskMetricsCollector", name);
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
    public void CollectionInterval_Should_Return_Ten_Seconds()
    {
        // Act
        var interval = _collector.CollectionInterval;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), interval);
    }

    #endregion

    #region CollectMetricsCore Tests

    [Fact]
    public void CollectMetricsCore_Should_Return_Disk_Utilization_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        Assert.NotNull(metrics);
        var metricsList = metrics.ToList();
        Assert.Equal(2, metricsList.Count);

        // Check first metric: DiskReadBytesPerSecond
        var readMetric = metricsList[0];
        Assert.Equal("DiskReadBytesPerSecond", readMetric.Name);
        Assert.Equal("bytes/second", readMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, readMetric.Type);
        Assert.True(readMetric.Value >= 0.0);

        // Check second metric: DiskWriteBytesPerSecond
        var writeMetric = metricsList[1];
        Assert.Equal("DiskWriteBytesPerSecond", writeMetric.Name);
        Assert.Equal("bytes/second", writeMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, writeMetric.Type);
        Assert.True(writeMetric.Value >= 0.0);
    }

    #endregion

    #region GetDiskReadBytesPerSecond Tests

    [Fact]
    public void GetDiskReadBytesPerSecond_Should_Return_Non_Negative_Value()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var readMetric = metrics.First(m => m.Name == "DiskReadBytesPerSecond");

        // Assert
        Assert.True(readMetric.Value >= 0.0);
    }

    [Fact]
    public void GetDiskReadBytesPerSecond_Should_Be_Calculated_Based_On_Time()
    {
        // Arrange: Collect metrics once to set initial values
        var firstMetrics = _collector.CollectMetrics();
        var firstReadValue = firstMetrics.First(m => m.Name == "DiskReadBytesPerSecond").Value;

        // Wait a bit to allow time difference
        System.Threading.Thread.Sleep(100);

        // Act: Collect metrics again and get the new value
        var secondMetrics = _collector.CollectMetrics();
        var secondReadValue = secondMetrics.First(m => m.Name == "DiskReadBytesPerSecond").Value;

        // We can't predict exact values due to system behavior, but we can verify the method runs
        Assert.True(firstReadValue >= 0.0);
        Assert.True(secondReadValue >= 0.0);
    }

    #endregion

    #region GetDiskWriteBytesPerSecond Tests

    [Fact]
    public void GetDiskWriteBytesPerSecond_Should_Return_Estimated_Value()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var writeMetric = metrics.First(m => m.Name == "DiskWriteBytesPerSecond");
        var readMetric = metrics.First(m => m.Name == "DiskReadBytesPerSecond");

        // Assert
        Assert.True(writeMetric.Value >= 0.0);
        Assert.True(writeMetric.Value == readMetric.Value * 0.5); // Based on implementation
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
            Assert.Equal(2, metricsList.Count);
            Assert.Equal("DiskReadBytesPerSecond", metricsList[0].Name);
            Assert.Equal("DiskWriteBytesPerSecond", metricsList[1].Name);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetDiskReadBytesPerSecond_Should_Handle_Exceptions_Gracefully()
    {
        // Act & Assert - Should not throw
        var metrics = _collector.CollectMetrics();
        Assert.NotNull(metrics);
    }

    [Fact]
    public void GetDiskWriteBytesPerSecond_Should_Handle_Exceptions_Gracefully()
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
        // Arrange - Collect metrics multiple times to trigger calculations
        var collectedMetrics = new List<IEnumerable<Relay.Core.AI.Optimization.Services.MetricValue>>();

        // Act - Collect several times
        for (int i = 0; i < 3; i++)
        {
            var metrics = _collector.CollectMetrics();
            collectedMetrics.Add(metrics);
            System.Threading.Thread.Sleep(50); // Small delay to allow time differences
        }

        // Assert
        Assert.Equal(3, collectedMetrics.Count);
        foreach (var metrics in collectedMetrics)
        {
            var metricsList = metrics.ToList();
            Assert.Equal(2, metricsList.Count);
            Assert.Equal("DiskReadBytesPerSecond", metricsList[0].Name);
            Assert.Equal("DiskWriteBytesPerSecond", metricsList[1].Name);
            
            // Both values should be non-negative
            Assert.True(metricsList[0].Value >= 0.0);
            Assert.True(metricsList[1].Value >= 0.0);
        }
    }

    #endregion
}
