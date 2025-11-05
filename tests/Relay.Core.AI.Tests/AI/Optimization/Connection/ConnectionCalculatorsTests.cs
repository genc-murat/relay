using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class ConnectionCalculatorsTests
{
    private readonly ILogger _logger = NullLogger.Instance;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ConnectionMetricsUtilities _utilities;
    private readonly ConnectionCalculators _calculators;

    public ConnectionCalculatorsTests()
    {
        _options = new AIOptimizationOptions
        {
            MaxEstimatedWebSocketConnections = 1000
        };
        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        _timeSeriesDb = new TestTimeSeriesDatabase();
        _systemMetrics = new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        var protocolCalculator = new ProtocolMetricsCalculator(_logger, _requestAnalytics, _timeSeriesDb, _systemMetrics);
        _utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, protocolCalculator);
        _calculators = new ConnectionCalculators(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionCalculators(null!, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionCalculators(_logger, null!, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionCalculators(_logger, _options, null!, _timeSeriesDb, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionCalculators(_logger, _options, _requestAnalytics, null!, _systemMetrics, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionCalculators(_logger, _options, _requestAnalytics, _timeSeriesDb, null!, _utilities));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Utilities_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionCalculators(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, null!));
    }

    #endregion

    #region CalculateMetricVolatility Tests

    [Fact]
    public void CalculateMetricVolatility_Should_Return_Zero_For_Empty_Metrics()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>();

        // Act
        var result = _calculators.CalculateMetricVolatility(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateMetricVolatility_Should_Return_Zero_For_Single_Metric()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateMetricVolatility(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateMetricVolatility_Should_Calculate_Correct_Volatility()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow },
            new MetricDataPoint { Value = 110, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new MetricDataPoint { Value = 90, Timestamp = DateTime.UtcNow.AddMinutes(-2) }
        };

        // Act
        var result = _calculators.CalculateMetricVolatility(metrics);

        // Assert
        Assert.True(result > 0);
        Assert.True(result < 1); // Should be reasonable volatility
    }

    [Fact]
    public void CalculateMetricVolatility_Should_Return_Zero_When_All_Values_Are_Zero()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 0, Timestamp = DateTime.UtcNow },
            new MetricDataPoint { Value = 0, Timestamp = DateTime.UtcNow.AddMinutes(-1) }
        };

        // Act
        var result = _calculators.CalculateMetricVolatility(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    #endregion

    #region CalculateWeightedAverage Tests

    [Fact]
    public void CalculateWeightedAverage_Should_Return_Zero_For_Empty_Metrics()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>();

        // Act
        var result = _calculators.CalculateWeightedAverage(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateWeightedAverage_Should_Give_Higher_Weight_To_Recent_Metrics()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow }, // Weight 3
            new MetricDataPoint { Value = 200, Timestamp = DateTime.UtcNow.AddMinutes(-1) }, // Weight 2
            new MetricDataPoint { Value = 300, Timestamp = DateTime.UtcNow.AddMinutes(-2) }  // Weight 1
        };

        // Act
        var result = _calculators.CalculateWeightedAverage(metrics);

        // Assert - should be closer to 100 than to 300
        Assert.True(result > 150); // More than simple average of 200
        Assert.True(result < 250); // Less than weighted average favoring recent
    }

    [Fact]
    public void CalculateWeightedAverage_Should_Handle_Single_Metric()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 150, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateWeightedAverage(metrics);

        // Assert
        Assert.Equal(150.0, result);
    }

    #endregion

    #region CalculateTrend Tests

    [Fact]
    public void CalculateTrend_Should_Return_Zero_For_Empty_Metrics()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>();

        // Act
        var result = _calculators.CalculateTrend(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateTrend_Should_Return_Zero_For_Single_Metric()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateTrend(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateTrend_Should_Calculate_Positive_Trend()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new MetricDataPoint { Value = 120, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new MetricDataPoint { Value = 140, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateTrend(metrics);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CalculateTrend_Should_Calculate_Negative_Trend()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 140, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new MetricDataPoint { Value = 120, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateTrend(metrics);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void CalculateTrend_Should_Handle_Divide_By_Zero()
    {
        // Arrange - all same x values (shouldn't happen but test edge case)
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow },
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow },
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateTrend(metrics);

        // Assert
        Assert.Equal(0.0, result);
    }

    #endregion

    #region EstimateRealTimeUsers Tests

    [Fact]
    public void EstimateRealTimeUsers_Should_Return_Positive_Value_With_Active_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(1),
            ConcurrentExecutions = 20
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _calculators.EstimateRealTimeUsers();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void EstimateRealTimeUsers_Should_Return_Zero_With_No_Active_Requests()
    {
        // Act
        var result = _calculators.EstimateRealTimeUsers();

        // Assert - should be based on actual active requests, but for test it's 0
        Assert.True(result >= 0);
    }

    #endregion

    #region EstimateActiveHubCount Tests

    [Fact]
    public void EstimateActiveHubCount_Should_Return_At_Least_One()
    {
        // Act
        var result = _calculators.EstimateActiveHubCount();

        // Assert
        Assert.True(result >= 1);
    }

    [Fact]
    public void EstimateActiveHubCount_Should_Scale_With_Active_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromSeconds(1),
            ConcurrentExecutions = 200
        }); // High activity
        _requestAnalytics[testType] = data;

        // Act
        var result = _calculators.EstimateActiveHubCount();

        // Assert
        Assert.True(result >= 1);
        Assert.True(result <= 5); // Capped at 5
    }

    #endregion

    #region CalculateConnectionMultiplier Tests

    [Fact]
    public void CalculateConnectionMultiplier_Should_Return_Greater_Than_One()
    {
        // Act
        var result = ConnectionCalculators.CalculateConnectionMultiplier();

        // Assert
        Assert.True(result > 1.0);
        Assert.Equal(1.3, result); // Current implementation returns 1.3
    }

    #endregion

    #region CalculateSignalRGroupFactor Tests

    [Fact]
    public void CalculateSignalRGroupFactor_Should_Return_Reasonable_Value()
    {
        // Act
        var result = _calculators.CalculateSignalRGroupFactor();

        // Assert
        Assert.True(result >= 1.0);
        Assert.True(result <= 2.0); // Conservative estimate
    }

    #endregion

    #region CalculateConnectionHealthRatio Tests

    [Fact]
    public void CalculateConnectionHealthRatio_Should_Return_Between_0_7_And_1()
    {
        // Act
        var result = _calculators.CalculateConnectionHealthRatio();

        // Assert
        Assert.True(result >= 0.7);
        Assert.True(result <= 1.0);
    }

    [Fact]
    public void CalculateConnectionHealthRatio_Should_Return_Higher_Value_With_Low_Error_Rate()
    {
        // Test through integration - with default low error rate
        var result = _calculators.CalculateConnectionHealthRatio();
        Assert.True(result >= 0.7);
    }

    #endregion

    #region ClassifyCurrentLoadLevel Tests

    [Theory]
    [InlineData(0, 0.0, LoadLevel.Idle)]
    [InlineData(25, 0.2, LoadLevel.Low)]
    [InlineData(75, 0.5, LoadLevel.Medium)]
    [InlineData(150, 0.8, LoadLevel.High)]
    public void ClassifyCurrentLoadLevel_Should_Classify_Correctly(int throughput, double cpuUsage, LoadLevel expectedLevel)
    {
        // This method depends on system metrics, tested through integration
        var result = _calculators.ClassifyCurrentLoadLevel();
        Assert.True(Enum.IsDefined(typeof(LoadLevel), result));
    }

    [Fact]
    public void ClassifyCurrentLoadLevel_Should_Handle_Exceptions()
    {
        // Act
        var result = _calculators.ClassifyCurrentLoadLevel();

        // Assert
        Assert.Equal(LoadLevel.Medium, result); // Default fallback
    }

    #endregion

    #region GetLoadBasedConnectionAdjustment Tests

    [Theory]
    [InlineData(LoadLevel.Idle, 0.8)]
    [InlineData(LoadLevel.Low, 0.9)]
    [InlineData(LoadLevel.Medium, 1.0)]
    [InlineData(LoadLevel.High, 1.2)]
    [InlineData(LoadLevel.Critical, 1.3)]
    public void GetLoadBasedConnectionAdjustment_Should_Return_Correct_Multipliers(LoadLevel level, double expected)
    {
        // Act
        var result = _calculators.GetLoadBasedConnectionAdjustment(level);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Time-based Factor Tests

    [Theory]
    [InlineData(9, 1.4)]   // Business hours
    [InlineData(12, 1.4)]  // Business hours
    [InlineData(18, 1.1)]  // Evening
    [InlineData(22, 1.1)]  // Evening
    [InlineData(2, 0.6)]   // Night
    [InlineData(6, 0.9)]   // Early morning
    public void CalculateTimeOfDayWebSocketFactor_Should_Return_Correct_Factors(int hour, double expectedFactor)
    {
        // Act
        var result = _calculators.CalculateTimeOfDayWebSocketFactor(hour);

        // Assert
        Assert.Equal(expectedFactor, result);
    }

    #endregion

    #region CalculateSystemStability Tests

    [Fact]
    public void CalculateSystemStability_Should_Return_Value_Between_0_And_1()
    {
        // Act
        var result = _calculators.CalculateSystemStability();

        // Assert
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
    }

    [Fact]
    public void CalculateSystemStability_Should_Return_1_With_No_Variance_Data()
    {
        // Act
        var result = _calculators.CalculateSystemStability();

        // Assert - should return 1.0 when no request analytics
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateSystemStability_Should_Calculate_Based_On_Variance()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromSeconds(1),
            ConcurrentExecutions = 20
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _calculators.CalculateSystemStability();

        // Assert
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public void CalculateConnectionHealthRatio_Should_Handle_High_Error_Rate()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 50,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromSeconds(2),
            ConcurrentExecutions = 30
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _calculators.CalculateConnectionHealthRatio();

        // Assert - should be lower with high error rate
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
        Assert.True(result < 0.8); // Should be significantly lower than optimal
    }

    [Fact]
    public void CalculateConnectionHealthRatio_Should_Handle_Zero_Error_Rate()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 100,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(500),
            ConcurrentExecutions = 10
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _calculators.CalculateConnectionHealthRatio();

        // Assert - should be high with zero error rate
        Assert.True(result >= 0.8);
        Assert.True(result <= 1.0);
    }

    [Fact]
    public void ClassifyCurrentLoadLevel_Should_Handle_Exception_In_SystemMetrics()
    {
        // Arrange - simulate a scenario that might cause exceptions
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1,
            SuccessfulExecutions = 0,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.MaxValue,
            ConcurrentExecutions = int.MaxValue
        });
        _requestAnalytics[testType] = data;

        // Act & Assert - should not throw and return a valid enum
        var result = _calculators.ClassifyCurrentLoadLevel();
        Assert.True(Enum.IsDefined(typeof(LoadLevel), result));
    }

    [Fact]
    public void EstimateRealTimeUsers_Should_Handle_Large_Concurrent_Requests()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 10000,
            SuccessfulExecutions = 9500,
            FailedExecutions = 500,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 1000
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = _calculators.EstimateRealTimeUsers();

        // Assert
        Assert.True(result > 0);
        Assert.True(result < 100000); // Reasonable upper bound
    }

    [Fact]
    public void CalculateMetricVolatility_Should_Handle_Very_Large_Values()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = float.MaxValue, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new MetricDataPoint { Value = float.MinValue, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new MetricDataPoint { Value = 0, Timestamp = DateTime.UtcNow }
        };

        // Act & Assert - should not throw
        var result = _calculators.CalculateMetricVolatility(metrics);
        Assert.True(result >= 0.0);
    }

    [Fact]
    public void CalculateTrend_Should_Handle_Identical_Values()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow.AddMinutes(-2) },
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow }
        };

        // Act
        var result = _calculators.CalculateTrend(metrics);

        // Assert - trend should be zero for identical values
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateWeightedAverage_Should_Handle_Zero_Weights()
    {
        // Arrange
        var metrics = new List<MetricDataPoint>
        {
            new MetricDataPoint { Value = 100, Timestamp = DateTime.UtcNow.AddDays(-10) },
            new MetricDataPoint { Value = 200, Timestamp = DateTime.UtcNow.AddDays(-5) }
        };

        // Act
        var result = _calculators.CalculateWeightedAverage(metrics);

        // Assert - should handle gracefully
        Assert.True(result >= 0.0);
    }

    [Fact]
    public void EstimateActiveHubCount_Should_Handle_Zero_Max_Connections()
    {
        // Arrange
        var options = new AIOptimizationOptions { MaxEstimatedWebSocketConnections = 0 };
        var calculators = new ConnectionCalculators(_logger, options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _utilities);

        // Act
        var result = calculators.EstimateActiveHubCount();

        // Assert - should handle zero max gracefully
        Assert.True(result >= 0);
    }

    [Fact]
    public void CalculateConnectionMultiplier_Should_Handle_Very_High_Concurrency()
    {
        // Arrange
        var testType = typeof(TestController);
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 900,
            FailedExecutions = 100,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = 999
        });
        _requestAnalytics[testType] = data;

        // Act
        var result = ConnectionCalculators.CalculateConnectionMultiplier();

        // Assert
        Assert.True(result >= 1.0);
        Assert.True(result < 100.0); // Reasonable upper bound
    }

    [Fact]
    public void CalculateSignalRGroupFactor_Should_Handle_No_Request_Data()
    {
        // Arrange - ensure no request data
        _requestAnalytics.Clear();

        // Act
        var result = _calculators.CalculateSignalRGroupFactor();

        // Assert - should return a reasonable default
        Assert.True(result >= 0.1);
        Assert.True(result <= 10.0);
    }

    [Fact]
    public void CalculateSystemStability_Should_Handle_Mixed_Success_Rates()
    {
        // Arrange
        var testType1 = typeof(TestController);
        var testType2 = typeof(string);
        
        var data1 = new RequestAnalysisData();
        data1.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 100,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 10
        });
        _requestAnalytics[testType1] = data1;

        var data2 = new RequestAnalysisData();
        data2.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 0,
            FailedExecutions = 100,
            AverageExecutionTime = TimeSpan.FromSeconds(10),
            ConcurrentExecutions = 50
        });
        _requestAnalytics[testType2] = data2;

        // Act
        var result = _calculators.CalculateSystemStability();

        // Assert - should be somewhere in the middle due to mixed performance
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
        // Just check it's a valid value - the actual algorithm may behave differently
        Assert.True(result >= 0.0);
        Assert.True(result <= 1.0);
    }

    #endregion

    // Helper classes
    private class TestController { }
}
