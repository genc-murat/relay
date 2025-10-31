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

public class ErrorMetricsCollectorTests
{
    private readonly ILogger _logger;
    private readonly ErrorMetricsCollector _collector;

    public ErrorMetricsCollectorTests()
    {
        _logger = NullLogger.Instance;
        _collector = new ErrorMetricsCollector(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ErrorMetricsCollector(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var collector = new ErrorMetricsCollector(logger);

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
        Assert.Equal("ErrorMetricsCollector", name);
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
    public void CollectMetricsCore_Should_Return_Error_Metrics()
    {
        // Act
        var metrics = _collector.CollectMetrics();

        // Assert
        Assert.NotNull(metrics);
        var metricsList = metrics.ToList();
        Assert.Equal(3, metricsList.Count);

        // Check TotalErrors metric
        var totalErrorsMetric = metricsList[0];
        Assert.Equal("TotalErrors", totalErrorsMetric.Name);
        Assert.Equal("count", totalErrorsMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Counter, totalErrorsMetric.Type);
        Assert.True(totalErrorsMetric.Value >= 0.0);

        // Check TotalExceptions metric
        var totalExceptionsMetric = metricsList[1];
        Assert.Equal("TotalExceptions", totalExceptionsMetric.Name);
        Assert.Equal("count", totalExceptionsMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Counter, totalExceptionsMetric.Type);
        Assert.True(totalExceptionsMetric.Value >= 0.0);

        // Check ErrorRate metric
        var errorRateMetric = metricsList[2];
        Assert.Equal("ErrorRate", errorRateMetric.Name);
        Assert.Equal("ratio", errorRateMetric.Unit);
        Assert.Equal(Relay.Core.AI.Optimization.Services.MetricType.Gauge, errorRateMetric.Type);
        Assert.True(errorRateMetric.Value >= 0.0);
        Assert.True(errorRateMetric.Value <= 1.0);
    }

    #endregion

    #region RecordError Tests

    [Fact]
    public void RecordError_Should_Increment_TotalErrors()
    {
        // Arrange: Get initial count
        var initialMetrics = _collector.CollectMetrics();
        var initialErrorCount = initialMetrics.First(m => m.Name == "TotalErrors").Value;

        // Act
        _collector.RecordError();

        // Assert
        var updatedMetrics = _collector.CollectMetrics();
        var newErrorCount = updatedMetrics.First(m => m.Name == "TotalErrors").Value;
        Assert.Equal(initialErrorCount + 1, newErrorCount);
    }

    [Fact]
    public void RecordError_Should_Be_Thread_Safe()
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
                    _collector.RecordError();
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var finalMetrics = _collector.CollectMetrics();
        var finalErrorCount = finalMetrics.First(m => m.Name == "TotalErrors").Value;
        Assert.Equal(threadCount * iterations, finalErrorCount);
    }

    #endregion

    #region RecordException Tests

    [Fact]
    public void RecordException_Should_Increment_TotalExceptions()
    {
        // Arrange: Get initial count
        var initialMetrics = _collector.CollectMetrics();
        var initialExceptionCount = initialMetrics.First(m => m.Name == "TotalExceptions").Value;

        // Act
        _collector.RecordException();

        // Assert
        var updatedMetrics = _collector.CollectMetrics();
        var newExceptionCount = updatedMetrics.First(m => m.Name == "TotalExceptions").Value;
        Assert.Equal(initialExceptionCount + 1, newExceptionCount);
    }

    [Fact]
    public void RecordException_Should_Be_Thread_Safe()
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
                    _collector.RecordException();
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var finalMetrics = _collector.CollectMetrics();
        var finalExceptionCount = finalMetrics.First(m => m.Name == "TotalExceptions").Value;
        Assert.Equal(threadCount * iterations, finalExceptionCount);
    }

    #endregion

    #region GetErrorRate Tests

    [Fact]
    public void GetErrorRate_Should_Return_Zero_Initially()
    {
        // Act
        var metrics = _collector.CollectMetrics();
        var errorRate = metrics.First(m => m.Name == "ErrorRate").Value;

        // Assert
        Assert.Equal(0.0, errorRate);
    }

    [Fact]
    public void GetErrorRate_Should_Increase_With_More_Errors()
    {
        // Arrange: Record some errors
        for (int i = 0; i < 5; i++)
        {
            _collector.RecordError();
        }

        // Act
        var metrics = _collector.CollectMetrics();
        var errorRate = metrics.First(m => m.Name == "ErrorRate").Value;

        // Assert: Assuming default 100 requests, 5 errors should give 5/100 = 0.05
        Assert.Equal(0.05, errorRate, 6); // Allow for precision in floating point
    }

    [Fact]
    public void GetErrorRate_Should_Not_Exceed_One()
    {
        // Arrange: Record many errors (more than assumed request count of 100)
        for (int i = 0; i < 150; i++)
        {
            _collector.RecordError();
        }

        // Act
        var metrics = _collector.CollectMetrics();
        var errorRate = metrics.First(m => m.Name == "ErrorRate").Value;

        // Assert: Should be clamped to 1.0
        Assert.True(errorRate <= 1.0);
    }

    #endregion

    #region Combined Metrics Tests

    [Fact]
    public void Metrics_Should_Update_Correctly_After_Multiple_Operations()
    {
        // Arrange
        var initialMetrics = _collector.CollectMetrics();
        var initialErrorCount = initialMetrics.First(m => m.Name == "TotalErrors").Value;
        var initialExceptionCount = initialMetrics.First(m => m.Name == "TotalExceptions").Value;

        // Act
        _collector.RecordError();
        _collector.RecordException();
        _collector.RecordError(); // Add another error

        var finalMetrics = _collector.CollectMetrics();

        // Assert
        var finalErrorCount = finalMetrics.First(m => m.Name == "TotalErrors").Value;
        var finalExceptionCount = finalMetrics.First(m => m.Name == "TotalExceptions").Value;

        Assert.Equal(initialErrorCount + 2, finalErrorCount);
        Assert.Equal(initialExceptionCount + 1, finalExceptionCount);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void CollectMetrics_Should_Be_Thread_Safe_While_Recording_Errors()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new List<IEnumerable<Relay.Core.AI.Optimization.Services.MetricValue>>();

        // Act: Multiple threads collecting metrics while another records errors
        for (int i = 0; i < 3; i++)
        {
            var task = Task.Run(() =>
            {
                for (int j = 0; j < 5; j++)
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

        // Also have another thread recording errors
        var recordTask = Task.Run(() =>
        {
            for (int j = 0; j < 10; j++)
            {
                _collector.RecordError();
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
            Assert.Contains(metricsList, m => m.Name == "TotalErrors");
            Assert.Contains(metricsList, m => m.Name == "TotalExceptions");
            Assert.Contains(metricsList, m => m.Name == "ErrorRate");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Error_Monitoring_Workflow_Should_Work()
    {
        // Arrange - Start with clean state
        var initialMetrics = _collector.CollectMetrics();
        var initialErrorCount = initialMetrics.First(m => m.Name == "TotalErrors").Value;
        var initialExceptionCount = initialMetrics.First(m => m.Name == "TotalExceptions").Value;

        // Act - Perform a series of operations
        for (int i = 0; i < 7; i++)
        {
            if (i % 2 == 0)
            {
                _collector.RecordError();
            }
            else
            {
                _collector.RecordException();
            }
        }

        // Collect metrics to observe results
        var finalMetrics = _collector.CollectMetrics();

        // Assert
        var finalErrorCount = finalMetrics.First(m => m.Name == "TotalErrors").Value;
        var finalExceptionCount = finalMetrics.First(m => m.Name == "TotalExceptions").Value;
        var errorRate = finalMetrics.First(m => m.Name == "ErrorRate").Value;

        // 7 operations, 4 errors (0,2,4,6) and 3 exceptions (1,3,5)
        Assert.Equal(initialErrorCount + 4, finalErrorCount);
        Assert.Equal(initialExceptionCount + 3, finalExceptionCount);
        Assert.True(errorRate >= 0.0);
        Assert.True(errorRate <= 1.0);
    }

    #endregion
}
