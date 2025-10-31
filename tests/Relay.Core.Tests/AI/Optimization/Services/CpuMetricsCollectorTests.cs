using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class CpuMetricsCollectorTests
{
    private readonly ILogger _logger;
    private readonly CpuMetricsCollector _collector;

    public CpuMetricsCollectorTests()
    {
        _logger = NullLogger.Instance;
        _collector = new CpuMetricsCollector(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new CpuMetricsCollector(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var collector = new CpuMetricsCollector(logger);

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
        Assert.Equal("CpuMetricsCollector", name);
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
    public void CollectionInterval_Should_Return_One_Second()
    {
        // Act
        var interval = _collector.CollectionInterval;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), interval);
    }

    #endregion

    #region CollectMetricsCore Tests

    [Fact]
    public void CollectMetricsCore_Should_Return_Cpu_Utilization_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        Assert.NotNull(metrics);
        var metricsList = metrics.ToList();
        Assert.Equal(2, metricsList.Count);

        // Check first metric: CpuUtilization
        var cpuUtilMetric = metricsList[0];
        Assert.Equal("CpuUtilization", cpuUtilMetric.Name);
        Assert.Equal("ratio", cpuUtilMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, cpuUtilMetric.Type);
        Assert.True(cpuUtilMetric.Value >= 0.0);
        Assert.True(cpuUtilMetric.Value <= 1.0);

        // Check second metric: CpuUsagePercent
        var cpuPercentMetric = metricsList[1];
        Assert.Equal("CpuUsagePercent", cpuPercentMetric.Name);
        Assert.Equal("percent", cpuPercentMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, cpuPercentMetric.Type);
        Assert.True(cpuPercentMetric.Value >= 0.0);
        Assert.True(cpuPercentMetric.Value <= 100.0);

        // Verify consistency between ratio and percentage
        Assert.Equal(cpuUtilMetric.Value * 100, cpuPercentMetric.Value, 6);
    }

    #endregion

    #region GetCpuUtilization Tests

    [Fact]
    public void GetCpuUtilization_Should_Return_Value_Between_Zero_And_One()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var cpuUtilMetric = metrics.First(m => m.Name == "CpuUtilization");

        // Assert
        Assert.True(cpuUtilMetric.Value >= 0.0);
        Assert.True(cpuUtilMetric.Value <= 1.0);
    }

    #endregion

    #region GetAverageCpuUtilization Tests

    [Fact]
    public void GetAverageCpuUtilization_Should_Return_Zero_When_History_Is_Empty()
    {
        // Act
        var average = _collector.GetAverageCpuUtilization();

        // Assert
        Assert.Equal(0.0, average);
    }

    [Fact]
    public void GetAverageCpuUtilization_Should_Calculate_Average_When_History_Has_Data()
    {
        // Arrange: Collect metrics multiple times to build history
        var metrics = _collector.CollectMetrics().ToList();
        System.Threading.Thread.Sleep(100); // Wait to allow time for CPU tracking
        var metrics2 = _collector.CollectMetrics().ToList();

        // Act
        var average = _collector.GetAverageCpuUtilization();

        // Assert
        Assert.True(average >= 0.0);
        Assert.True(average <= 1.0);
    }

    #endregion

    #region GetMaxCpuUtilization Tests

    [Fact]
    public void GetMaxCpuUtilization_Should_Return_Zero_When_History_Is_Empty()
    {
        // Act
        var max = _collector.GetMaxCpuUtilization();

        // Assert
        Assert.Equal(0.0, max);
    }

    [Fact]
    public void GetMaxCpuUtilization_Should_Return_Maximum_Value_When_History_Has_Data()
    {
        // Arrange: Collect metrics to build history
        var metrics = _collector.CollectMetrics().ToList();
        var metrics2 = _collector.CollectMetrics().ToList();

        // Act
        var max = _collector.GetMaxCpuUtilization();

        // Assert
        Assert.True(max >= 0.0);
        Assert.True(max <= 1.0);
    }

    #endregion

    #region GetMinCpuUtilization Tests

    [Fact]
    public void GetMinCpuUtilization_Should_Return_Zero_When_History_Is_Empty()
    {
        // Act
        var min = _collector.GetMinCpuUtilization();

        // Assert
        Assert.Equal(0.0, min);
    }

    [Fact]
    public void GetMinCpuUtilization_Should_Return_Minimum_Value_When_History_Has_Data()
    {
        // Arrange: Collect metrics to build history
        var metrics = _collector.CollectMetrics().ToList();
        var metrics2 = _collector.CollectMetrics().ToList();

        // Act
        var min = _collector.GetMinCpuUtilization();

        // Assert
        Assert.True(min >= 0.0);
        Assert.True(min <= 1.0);
    }

    #endregion

    #region GetCpuUtilizationStdDev Tests

    [Fact]
    public void GetCpuUtilizationStdDev_Should_Return_Zero_When_History_Has_Less_Than_Two_Values()
    {
        // Act
        var stdDev = _collector.GetCpuUtilizationStdDev();

        // Assert
        Assert.Equal(0.0, stdDev);
    }

    [Fact]
    public void GetCpuUtilizationStdDev_Should_Calculate_Standard_Deviation_When_History_Has_Multiple_Values()
    {
        // Arrange: Collect metrics multiple times to build history
        var metrics = _collector.CollectMetrics().ToList();
        System.Threading.Thread.Sleep(100); // Wait to ensure different values
        var metrics2 = _collector.CollectMetrics().ToList();

        // Act
        var stdDev = _collector.GetCpuUtilizationStdDev();

        // Assert
        Assert.True(stdDev >= 0.0);
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
            Assert.Equal("CpuUtilization", metricsList[0].Name);
            Assert.Equal("CpuUsagePercent", metricsList[1].Name);
        }
    }

    [Fact]
    public void GetAverageCpuUtilization_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<System.Threading.Tasks.Task>();
        var results = new List<double>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var task = System.Threading.Tasks.Task.Run(() =>
            {
                var average = _collector.GetAverageCpuUtilization();
                lock (results)
                {
                    results.Add(average);
                }
            });
            tasks.Add(task);
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(5, results.Count);
        foreach (var result in results)
        {
            Assert.True(result >= 0.0);
            Assert.True(result <= 1.0);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetCpuUtilization_Should_Handle_Exceptions_Gracefully()
    {
        // Note: It's difficult to force an exception in GetCpuUtilization
        // since it relies on Process.GetCurrentProcess().TotalProcessorTime
        // which is generally reliable. We'll ensure the method completes without throwing.
        
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

        // Act - Collect several times to build history
        for (int i = 0; i < 3; i++)
        {
            var metrics = _collector.CollectMetrics();
            collectedMetrics.Add(metrics);
            System.Threading.Thread.Sleep(100); // Wait between collections
        }

        // Check historical methods after collecting data
        var average = _collector.GetAverageCpuUtilization();
        var max = _collector.GetMaxCpuUtilization();
        var min = _collector.GetMinCpuUtilization();
        var stdDev = _collector.GetCpuUtilizationStdDev();

        // Assert
        Assert.Equal(3, collectedMetrics.Count);
        foreach (var metrics in collectedMetrics)
        {
            var metricsList = metrics.ToList();
            Assert.Equal(2, metricsList.Count);
            Assert.Equal("CpuUtilization", metricsList[0].Name);
            Assert.Equal("CpuUsagePercent", metricsList[1].Name);
        }

        Assert.True(average >= 0.0 && average <= 1.0);
        Assert.True(max >= 0.0 && max <= 1.0);
        Assert.True(min >= 0.0 && min <= 1.0);
        Assert.True(stdDev >= 0.0);
        if (collectedMetrics.Count > 1)
        {
            Assert.True(max >= min); // Max should be greater than or equal to min
        }
    }

    #endregion
}
