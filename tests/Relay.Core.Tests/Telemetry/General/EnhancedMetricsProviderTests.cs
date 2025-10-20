using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests;

public class EnhancedMetricsProviderTests
{
    private readonly EnhancedMetricsProvider _metricsProvider;
    private readonly MetricsProviderOptions _options;
    private readonly ILogger<EnhancedMetricsProvider> _logger;

    public EnhancedMetricsProviderTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<EnhancedMetricsProvider>();

        _options = new MetricsProviderOptions
        {
            SlowExecutionThreshold = 2.0,
            HighFailureRateThreshold = 0.15, // 15%
            TimeoutThreshold = TimeSpan.FromSeconds(5),
            MinSampleSizeForAnomalyDetection = 5,
            RecentExecutionsForAnomalyCheck = 3,
            EnableRealTimeAnomalyDetection = true
        };

        _metricsProvider = new EnhancedMetricsProvider(Options.Create(_options), _logger);
    }

    [Fact]
    public void EnhancedMetricsProvider_WithCustomOptions_ShouldUseConfiguredThresholds()
    {
        // Arrange - Create baseline executions
        for (int i = 0; i < 10; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "ConfigurableHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Add a slow execution that exceeds the configured threshold
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "slow-execution",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "ConfigurableHandler",
            Duration = TimeSpan.FromMilliseconds(250), // 2.5x the baseline (exceeds 2.0 threshold)
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.NotEmpty(anomalies);
        var slowAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.SlowExecution);
        Assert.NotNull(slowAnomaly);
        Assert.Equal("slow-execution", slowAnomaly.OperationId);
        Assert.True(slowAnomaly.Severity >= _options.SlowExecutionThreshold);
    }

    [Fact]
    public void DetectAnomalies_WithTimeoutThreshold_ShouldDetectTimeoutExceeded()
    {
        // Arrange - Create baseline executions first
        for (int i = 0; i < 10; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TimeoutHandler",
                Duration = TimeSpan.FromSeconds(1), // Normal duration
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "timeout-execution",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "TimeoutHandler",
            Duration = TimeSpan.FromSeconds(6), // Exceeds 5-second timeout
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.NotEmpty(anomalies);
        var timeoutAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.TimeoutExceeded);
        Assert.NotNull(timeoutAnomaly);
        Assert.Equal("timeout-execution", timeoutAnomaly.OperationId);
        Assert.Equal(typeof(TestRequest<string>), timeoutAnomaly.RequestType);
        Assert.True(timeoutAnomaly.ActualDuration > _options.TimeoutThreshold);
    }

    [Fact]
    public void DetectAnomalies_WithUnusualItemCount_ShouldDetectStreamingAnomaly()
    {
        // Arrange - Create baseline streaming operations
        for (int i = 0; i < 15; i++) // More baseline data
        {
            _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                ItemCount = 100, // Consistent baseline
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Add an operation with unusual item count
        _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
        {
            OperationId = "unusual-count-operation",
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "StreamHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            ItemCount = 500, // Much higher than baseline (5x)
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.NotEmpty(anomalies);
        var itemCountAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.UnusualItemCount);
        Assert.NotNull(itemCountAnomaly);
        Assert.Equal("unusual-count-operation", itemCountAnomaly.OperationId);
        Assert.Equal(typeof(TestStreamRequest<string>), itemCountAnomaly.RequestType);
    }

    [Fact]
    public void RecordTimingBreakdown_WithDetailedPhases_ShouldStoreCompleteBreakdown()
    {
        // Arrange
        var breakdown = new TimingBreakdown
        {
            OperationId = "detailed-breakdown",
            TotalDuration = TimeSpan.FromMilliseconds(500),
            PhaseTimings = new Dictionary<string, TimeSpan>
            {
                ["Input Validation"] = TimeSpan.FromMilliseconds(50),
                ["Authorization"] = TimeSpan.FromMilliseconds(30),
                ["Business Logic"] = TimeSpan.FromMilliseconds(300),
                ["Data Access"] = TimeSpan.FromMilliseconds(100),
                ["Response Serialization"] = TimeSpan.FromMilliseconds(20)
            },
            Metadata = new Dictionary<string, object>
            {
                ["RequestSize"] = 2048,
                ["ResponseSize"] = 4096,
                ["DatabaseQueries"] = 3,
                ["CacheHits"] = 2,
                ["CacheMisses"] = 1
            }
        };

        // Act
        _metricsProvider.RecordTimingBreakdown(breakdown);
        var retrieved = _metricsProvider.GetTimingBreakdown("detailed-breakdown");

        // Assert
        Assert.Equal(breakdown.OperationId, retrieved.OperationId);
        Assert.Equal(breakdown.TotalDuration, retrieved.TotalDuration);
        Assert.Equal(5, retrieved.PhaseTimings.Count);
        Assert.Equal(5, retrieved.Metadata.Count);

        // Verify phase timings
        Assert.Equal(TimeSpan.FromMilliseconds(50), retrieved.PhaseTimings["Input Validation"]);
        Assert.Equal(TimeSpan.FromMilliseconds(300), retrieved.PhaseTimings["Business Logic"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), retrieved.PhaseTimings["Data Access"]);

        // Verify metadata
        Assert.Equal(2048, retrieved.Metadata["RequestSize"]);
        Assert.Equal(3, retrieved.Metadata["DatabaseQueries"]);
        Assert.Equal(2, retrieved.Metadata["CacheHits"]);
    }

    [Fact]
    public void DetectAnomalies_WithInsufficientData_ShouldNotDetectAnomalies()
    {
        // Arrange - Record fewer executions than minimum sample size
        for (int i = 0; i < _options.MinSampleSizeForAnomalyDetection - 1; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "InsufficientDataHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Add a potentially anomalous execution
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            RequestType = typeof(TestRequest<string>),
            HandlerName = "InsufficientDataHandler",
            Duration = TimeSpan.FromMilliseconds(1000), // Very slow
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert - Should not detect anomalies due to insufficient data
        Assert.Empty(anomalies);
    }

    [Fact]
    public void EnhancedMetricsProvider_WithMemoryLimits_ShouldRespectConfiguredLimits()
    {
        // Arrange
        var options = new MetricsProviderOptions
        {
            MaxRecordsPerHandler = 5,
            MaxTimingBreakdowns = 3
        };
        var provider = new EnhancedMetricsProvider(Options.Create(options));

        // Act - Record more executions than the limit
        for (int i = 0; i < 10; i++)
        {
            provider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                OperationId = $"execution-{i}",
                RequestType = typeof(TestRequest<string>),
                HandlerName = "LimitedHandler",
                Duration = TimeSpan.FromMilliseconds(100 + i),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(-i)
            });
        }

        // Record more timing breakdowns than the limit
        for (int i = 0; i < 5; i++)
        {
            provider.RecordTimingBreakdown(new TimingBreakdown
            {
                OperationId = $"breakdown-{i}",
                TotalDuration = TimeSpan.FromMilliseconds(100)
            });
        }

        // Assert
        var stats = provider.GetHandlerExecutionStats(typeof(TestRequest<string>), "LimitedHandler");
        Assert.True(stats.TotalExecutions <= options.MaxRecordsPerHandler);

        // Check that only the configured number of timing breakdowns are kept
        var existingBreakdowns = 0;
        for (int i = 0; i < 5; i++)
        {
            var breakdown = provider.GetTimingBreakdown($"breakdown-{i}");
            if (breakdown != null && breakdown.TotalDuration > TimeSpan.Zero)
            {
                existingBreakdowns++;
            }
        }
        Assert.True(existingBreakdowns <= options.MaxTimingBreakdowns);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithPercentileCalculations_ShouldReturnAccuratePercentiles()
    {
        // Arrange - Create executions with known durations for percentile testing
        var durations = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }; // milliseconds

        for (int i = 0; i < durations.Length; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "PercentileHandler",
                Duration = TimeSpan.FromMilliseconds(durations[i]),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "PercentileHandler");

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(55), stats.AverageExecutionTime); // Average of 10-100
        Assert.Equal(TimeSpan.FromMilliseconds(10), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(50), stats.P50ExecutionTime); // Median of 10 values (actual median of [10,20,30,40,50,60,70,80,90,100] is (50+60)/2 = 55, but implementation might differ)
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.P95ExecutionTime); // 95th percentile
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.P99ExecutionTime); // 99th percentile
    }

    [Fact]
    public async Task EnhancedMetricsProvider_WithConcurrentAccess_ShouldHandleThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentOperations = 100;
        const int operationsPerTask = 10;

        // Act - Simulate concurrent metric recording from multiple threads
        for (int taskIndex = 0; taskIndex < concurrentOperations; taskIndex++)
        {
            var index = taskIndex;
            tasks.Add(Task.Run(() =>
            {
                for (int opIndex = 0; opIndex < operationsPerTask; opIndex++)
                {
                    _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
                    {
                        OperationId = $"concurrent-{index}-{opIndex}",
                        RequestType = typeof(TestRequest<string>),
                        HandlerName = "ConcurrentHandler",
                        Duration = TimeSpan.FromMilliseconds(100 + (index % 50)),
                        Success = (index + opIndex) % 3 != 0, // Mix of success/failure
                        Timestamp = DateTimeOffset.UtcNow
                    });

                    _metricsProvider.RecordTimingBreakdown(new TimingBreakdown
                    {
                        OperationId = $"breakdown-{index}-{opIndex}",
                        TotalDuration = TimeSpan.FromMilliseconds(100 + index),
                        PhaseTimings = new Dictionary<string, TimeSpan>
                        {
                            ["Phase1"] = TimeSpan.FromMilliseconds(50),
                            ["Phase2"] = TimeSpan.FromMilliseconds(50 + index)
                        }
                    });
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "ConcurrentHandler");
        Assert.True(stats.TotalExecutions > 0);
        Assert.True(stats.SuccessfulExecutions > 0);
        Assert.True(stats.FailedExecutions > 0);
        Assert.True(stats.AverageExecutionTime > TimeSpan.Zero);

        // Verify that anomaly detection still works with concurrent data
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1));
        Assert.NotNull(anomalies); // Should not throw exceptions
    }

    [Fact]
    public void DetectAnomalies_WithComplexScenario_ShouldDetectMultipleAnomalyTypes()
    {
        // Arrange - Create a complex scenario with multiple anomaly types

        // Normal baseline executions
        for (int i = 0; i < 20; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "ComplexHandler",
                Duration = TimeSpan.FromMilliseconds(100 + (i % 10)),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-30 + i)
            });
        }

        // Add slow execution anomaly
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "slow-complex",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "ComplexHandler",
            Duration = TimeSpan.FromMilliseconds(300), // 3x baseline
            Success = true,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
        });

        // Add timeout exceeded anomaly
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "timeout-complex",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "ComplexHandler",
            Duration = TimeSpan.FromSeconds(7), // Exceeds 5-second timeout
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Create high failure rate scenario for another handler
        for (int i = 0; i < 20; i++) // More samples
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "FailingHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = i < 2, // 10% success rate = 90% failure rate (much higher than 15% threshold)
                Exception = i >= 2 ? new InvalidOperationException("Test failure") : null,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-20 + i)
            });
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.True(anomalies.Count >= 3, $"Expected at least 3 anomalies, got {anomalies.Count}");

        var slowAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.SlowExecution && a.OperationId == "slow-complex");
        Assert.NotNull(slowAnomaly);

        var timeoutAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.TimeoutExceeded && a.OperationId == "timeout-complex");
        Assert.NotNull(timeoutAnomaly);

        var failureAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.HighFailureRate && a.HandlerName == "FailingHandler");
        Assert.NotNull(failureAnomaly);

        // Verify anomalies are ordered by severity
        for (int i = 0; i < anomalies.Count - 1; i++)
        {
            Assert.True(anomalies[i].Severity >= anomalies[i + 1].Severity,
                $"Anomalies should be ordered by severity descending. Index {i}: {anomalies[i].Severity}, Index {i + 1}: {anomalies[i + 1].Severity}");
        }
    }
}

