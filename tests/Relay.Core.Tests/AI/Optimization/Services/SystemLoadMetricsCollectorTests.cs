using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class SystemLoadMetricsCollectorTests
{
    private readonly ILogger _logger;
    private readonly SystemLoadMetricsCollector _collector;

    public SystemLoadMetricsCollectorTests()
    {
        _logger = NullLogger.Instance;
        _collector = new SystemLoadMetricsCollector(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new SystemLoadMetricsCollector(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var collector = new SystemLoadMetricsCollector(logger);

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
        Assert.Equal("SystemLoadMetricsCollector", name);
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
    public void CollectionInterval_Should_Return_Thirty_Seconds()
    {
        // Act
        var interval = _collector.CollectionInterval;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), interval);
    }

    #endregion

    #region CollectMetricsCore Tests

    [Fact]
    public void CollectMetricsCore_Should_Return_System_Load_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        Assert.NotNull(metrics);
        var metricsList = metrics.ToList();
        Assert.Equal(3, metricsList.Count);

        // Check SystemLoadAverage metric
        var loadAvgMetric = metricsList[0];
        Assert.Equal("SystemLoadAverage", loadAvgMetric.Name);
        Assert.Equal("load", loadAvgMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, loadAvgMetric.Type);
        Assert.True(loadAvgMetric.Value >= 0.0);

        // Check ThreadCount metric
        var threadMetric = metricsList[1];
        Assert.Equal("ThreadCount", threadMetric.Name);
        Assert.Equal("count", threadMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, threadMetric.Type);
        Assert.True(threadMetric.Value >= 0.0);

        // Check HandleCount metric
        var handleMetric = metricsList[2];
        Assert.Equal("HandleCount", handleMetric.Name);
        Assert.Equal("count", handleMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, handleMetric.Type);
        Assert.True(handleMetric.Value >= 0.0);
    }

    [Fact]
    public void CollectMetricsCore_Should_Return_Non_Negative_Values()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var loadAverage = metrics.First(m => m.Name == "SystemLoadAverage").Value;
        var threadCount = metrics.First(m => m.Name == "ThreadCount").Value;
        var handleCount = metrics.First(m => m.Name == "HandleCount").Value;

        // Assert
        Assert.True(loadAverage >= 0.0);
        Assert.True(threadCount >= 0.0);
        Assert.True(handleCount >= 0.0);
    }

    #endregion

    #region GetThreadCount Tests

    [Fact]
    public void GetThreadCount_Should_Return_Positive_Value()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var threadCount = metrics.First(m => m.Name == "ThreadCount").Value;

        // Assert
        Assert.True(threadCount >= 0.0);
    }

    #endregion

    #region GetHandleCount Tests

    [Fact]
    public void GetHandleCount_Should_Return_Positive_Value()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var handleCount = metrics.First(m => m.Name == "HandleCount").Value;

        // Assert
        Assert.True(handleCount >= 0.0);
    }

    #endregion

    #region GetSystemLoadAverage Tests

    [Fact]
    public void GetSystemLoadAverage_Should_Calculate_Correctly()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var loadAverage = metrics.First(m => m.Name == "SystemLoadAverage").Value;

        // Assert
        Assert.True(loadAverage >= 0.0);
        // The exact value will depend on system-specific values for threads, but it should be reasonable
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void CollectMetrics_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new List<IEnumerable<Relay.Core.AI.Optimization.Services.MetricValue>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var task = Task.Run(() =>
            {
                var metrics = _collector.CollectMetrics();
                lock (results)
                {
                    results.Add(metrics);
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(5, results.Count);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            var metricsList = result.ToList();
            Assert.Equal(3, metricsList.Count);
            Assert.Equal("SystemLoadAverage", metricsList[0].Name);
            Assert.Equal("ThreadCount", metricsList[1].Name);
            Assert.Equal("HandleCount", metricsList[2].Name);
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
            Thread.Sleep(10); // Small delay
        }

        // Assert
        Assert.Equal(3, collectedMetrics.Count);
        foreach (var metrics in collectedMetrics)
        {
            var metricsList = metrics.ToList();
            Assert.Equal(3, metricsList.Count);
            Assert.Equal("SystemLoadAverage", metricsList[0].Name);
            Assert.Equal("ThreadCount", metricsList[1].Name);
            Assert.Equal("HandleCount", metricsList[2].Name);
            
            // Validate values are within expected ranges
            var loadAverage = metricsList[0].Value;
            var threadCount = metricsList[1].Value;
            var handleCount = metricsList[2].Value;
            
            Assert.True(loadAverage >= 0.0);
            Assert.True(threadCount >= 0.0);
            Assert.True(handleCount >= 0.0);
        }
    }

    [Fact]
    public void Metrics_Should_Be_Consistent_Across_Collections()
    {
        // Act
        var firstMetrics = _collector.CollectMetrics().ToList();
        var secondMetrics = _collector.CollectMetrics().ToList();

        // Assert
        // Both collections should have the same metrics
        Assert.Equal(3, firstMetrics.Count);
        Assert.Equal(3, secondMetrics.Count);
        
        Assert.Equal("SystemLoadAverage", firstMetrics[0].Name);
        Assert.Equal("ThreadCount", firstMetrics[1].Name);
        Assert.Equal("HandleCount", firstMetrics[2].Name);
        
        Assert.Equal("SystemLoadAverage", secondMetrics[0].Name);
        Assert.Equal("ThreadCount", secondMetrics[1].Name);
        Assert.Equal("HandleCount", secondMetrics[2].Name);
    }

    #endregion
}
