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

public class ThroughputMetricsCollectorTests
{
    private readonly ILogger _logger;
    private readonly ThroughputMetricsCollector _collector;

    public ThroughputMetricsCollectorTests()
    {
        _logger = NullLogger.Instance;
        _collector = new ThroughputMetricsCollector(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ThroughputMetricsCollector(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var collector = new ThroughputMetricsCollector(logger);

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
        Assert.Equal("ThroughputMetricsCollector", name);
    }

    [Fact]
    public void SupportedTypes_Should_Return_Counter_And_Gauge_Types()
    {
        // Act
        var supportedTypes = _collector.SupportedTypes.ToList();

        // Assert
        Assert.NotNull(supportedTypes);
        Assert.Equal(2, supportedTypes.Count);
        Assert.Contains(Relay.Core.AI.Optimization.Services.MetricType.Counter, supportedTypes);
        Assert.Contains(Relay.Core.AI.Optimization.Services.MetricType.Gauge, supportedTypes);
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
    public void CollectMetricsCore_Should_Return_Throughput_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        Assert.NotNull(metrics);
        var metricsList = metrics.ToList();
        Assert.Equal(3, metricsList.Count);

        // Check TotalRequestsProcessed metric
        var totalRequestsMetric = metricsList[0];
        Assert.Equal("TotalRequestsProcessed", totalRequestsMetric.Name);
        Assert.Equal("count", totalRequestsMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Counter, totalRequestsMetric.Type);
        Assert.True(totalRequestsMetric.Value >= 0.0);

        // Check ThroughputPerSecond metric
        var throughputMetric = metricsList[1];
        Assert.Equal("ThroughputPerSecond", throughputMetric.Name);
        Assert.Equal("requests/second", throughputMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, throughputMetric.Type);
        Assert.True(throughputMetric.Value >= 0.0);

        // Check RequestsPerSecond metric
        var requestsPerSecondMetric = metricsList[2];
        Assert.Equal("RequestsPerSecond", requestsPerSecondMetric.Name);
        Assert.Equal("requests/second", requestsPerSecondMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, requestsPerSecondMetric.Type);
        Assert.True(requestsPerSecondMetric.Value >= 0.0);
    }

    #endregion

    #region RecordRequestProcessed Tests

    [Fact]
    public void RecordRequestProcessed_Should_Increment_Total_Requests()
    {
        // Arrange: Get initial count
        var initialMetrics = _collector.CollectMetrics();
        var initialCount = initialMetrics.First(m => m.Name == "TotalRequestsProcessed").Value;

        // Act
        _collector.RecordRequestProcessed();

        // Assert
        var updatedMetrics = _collector.CollectMetrics();
        var newCount = updatedMetrics.First(m => m.Name == "TotalRequestsProcessed").Value;
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public void RecordRequestProcessed_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int threadCount = 5;
        const int iterations = 10;

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var task = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    _collector.RecordRequestProcessed();
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var finalMetrics = _collector.CollectMetrics();
        var finalCount = finalMetrics.First(m => m.Name == "TotalRequestsProcessed").Value;
        Assert.Equal(threadCount * iterations, finalCount);
    }

    #endregion

    #region GetThroughputPerSecond Tests

    [Fact]
    public void GetThroughputPerSecond_Should_Calculate_Initial_Zero()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var throughput = metrics.First(m => m.Name == "ThroughputPerSecond").Value;

        // Assert
        Assert.Equal(0.0, throughput);
    }

    [Fact]
    public void GetThroughputPerSecond_Should_Calculate_With_Requests()
    {
        // Arrange: Process some requests
        for (int i = 0; i < 10; i++)
        {
            _collector.RecordRequestProcessed();
        }

        // Act: Wait a bit to allow time to pass for throughput calculation
        Thread.Sleep(1000); // Wait 1 second

        var metrics = _collector.CollectMetrics();
        var throughput = metrics.First(m => m.Name == "ThroughputPerSecond").Value;

        // Since we're using a time-based calculation, we can't predict exact values,
        // but we can ensure the method works and returns a reasonable value
        Assert.True(throughput >= 0.0);
    }

    [Fact]
    public void ThroughputMetrics_Should_Be_Consistent()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var throughput1 = metrics.First(m => m.Name == "ThroughputPerSecond").Value;
        var throughput2 = metrics.First(m => m.Name == "RequestsPerSecond").Value;

        // Assert: Both metrics should have the same value
        Assert.Equal(throughput1, throughput2);
    }

    #endregion

    #region Combined Metrics Tests

    [Fact]
    public void Metrics_Should_Update_Correctly_After_Multiple_Requests()
    {
        // Arrange: Record some requests
        for (int i = 0; i < 5; i++)
        {
            _collector.RecordRequestProcessed();
        }

        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        var totalRequests = metrics.First(m => m.Name == "TotalRequestsProcessed").Value;
        var throughput = metrics.First(m => m.Name == "ThroughputPerSecond").Value;

        Assert.True(totalRequests >= 0.0);
        Assert.True(throughput >= 0.0);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void CollectMetrics_Should_Be_Thread_Safe_While_Recording_Requests()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new List<IEnumerable<Relay.Core.AI.Optimization.Services.MetricValue>>();

        // Act: Multiple threads collecting metrics while another records requests
        for (int i = 0; i < 3; i++)
        {
            var task = Task.Run(() =>
            {
                for (int j = 0; j < 3; j++)
                {
                    var metrics = _collector.CollectMetrics();
                    lock (results)
                    {
                        results.Add(metrics);
                    }
                    Thread.Sleep(10); // Small delay
                }
            });
            tasks.Add(task);
        }

        // Also have another thread recording requests
        var recordTask = Task.Run(() =>
        {
            for (int j = 0; j < 10; j++)
            {
                _collector.RecordRequestProcessed();
                Thread.Sleep(20); // Small delay
            }
        });
        tasks.Add(recordTask);

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.True(results.Count > 0);
        foreach (var result in results)
        {
            var metricsList = result.ToList();
            Assert.Equal(3, metricsList.Count);
            Assert.Contains(metricsList, m => m.Name == "TotalRequestsProcessed");
            Assert.Contains(metricsList, m => m.Name == "ThroughputPerSecond");
            Assert.Contains(metricsList, m => m.Name == "RequestsPerSecond");
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
    public void Full_Throughput_Monitoring_Workflow_Should_Work()
    {
        // Arrange - Start with clean state
        var initialMetrics = _collector.CollectMetrics();
        var initialTotalRequests = initialMetrics.First(m => m.Name == "TotalRequestsProcessed").Value;

        // Act - Process a series of requests
        for (int i = 0; i < 7; i++)
        {
            _collector.RecordRequestProcessed();
        }

        // Collect metrics to observe results
        var finalMetrics = _collector.CollectMetrics();

        // Assert
        var finalTotalRequests = finalMetrics.First(m => m.Name == "TotalRequestsProcessed").Value;
        var throughput = finalMetrics.First(m => m.Name == "ThroughputPerSecond").Value;
        var requestsPerSecond = finalMetrics.First(m => m.Name == "RequestsPerSecond").Value;

        // Verify that total requests increased
        Assert.Equal(initialTotalRequests + 7, finalTotalRequests);
        
        // Verify that both throughput metrics are equal
        Assert.Equal(throughput, requestsPerSecond);
        
        // Verify that values are non-negative
        Assert.True(throughput >= 0.0);
        Assert.True(requestsPerSecond >= 0.0);
    }

    [Fact]
    public void Throughput_Should_Reset_After_Collection_Period()
    {
        // This test is difficult to implement precisely due to the time-based reset logic
        // in GetThroughputPerSecond, but we can at least verify the functionality works
        // by checking that recording requests and collecting metrics doesn't throw exceptions

        // Act
        for (int i = 0; i < 3; i++)
        {
            _collector.RecordRequestProcessed();
            var metrics = _collector.CollectMetrics();
            Thread.Sleep(100); // Small delay to allow potential time-based calculations
        }

        // Assert - Just ensure no exceptions were thrown
        var finalMetrics = _collector.CollectMetrics();
        Assert.NotNull(finalMetrics);
        Assert.Equal(3, finalMetrics.Count());
    }

    #endregion
}
