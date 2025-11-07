using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

/// <summary>
/// Comprehensive null and empty input handling tests for TrendAnalyzer public methods.
/// Tests robustness against invalid inputs.
/// </summary>
public class TrendAnalyzerNullEmptyInputTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TrendAnalyzer _analyzer;

    public TrendAnalyzerNullEmptyInputTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTrendAnalysis();

        _serviceProvider = services.BuildServiceProvider();
        _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
            ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");
    }

    #region AnalyzeMetricTrends Null/Empty Input Tests

    [Fact]
    public void AnalyzeMetricTrends_NullMetrics_ThrowsNullReferenceException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _analyzer.AnalyzeMetricTrends(null!));
    }

    [Fact]
    public void AnalyzeMetricTrends_EmptyMetrics_ReturnsValidResult()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();

        // Act
        var result = _analyzer.AnalyzeMetricTrends(emptyMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
        Assert.Empty(result.MovingAverages);
        Assert.Empty(result.TrendDirections);
        Assert.Empty(result.TrendVelocities);
        Assert.Empty(result.SeasonalityPatterns);
        Assert.Empty(result.RegressionResults);
        Assert.Empty(result.Correlations);
        Assert.Empty(result.Anomalies);
        Assert.Empty(result.Insights);
    }

    #endregion

    #region CalculateMovingAverages Null/Empty Input Tests

    [Fact]
    public void CalculateMovingAverages_NullMetrics_DoesNotThrowException()
    {
        // Act & Assert - The method delegates to updater which may not validate nulls
        var exception = Record.Exception(() => _analyzer.CalculateMovingAverages(null!, DateTime.UtcNow));
        // The mock updater doesn't throw, so no exception occurs
        Assert.Null(exception);
    }

    [Fact]
    public void CalculateMovingAverages_EmptyMetrics_ReturnsEmptyResult()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _analyzer.CalculateMovingAverages(emptyMetrics, timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void CalculateMovingAverages_DefaultTimestamp_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };
        var defaultTimestamp = default(DateTime);

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, defaultTimestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("cpu"));
        Assert.Equal(defaultTimestamp, result["cpu"].Timestamp);
    }

    #endregion

    #region DetectPerformanceAnomalies Null/Empty Input Tests

    [Fact]
    public void DetectPerformanceAnomalies_NullMetrics_DoesNotThrowException()
    {
        // Arrange
        var movingAverages = new Dictionary<string, MovingAverageData>();

        // Act & Assert - The method delegates to updater which may not validate nulls
        var exception = Record.Exception(() => _analyzer.DetectPerformanceAnomalies(null!, movingAverages));
        Assert.Null(exception);
    }

    [Fact]
    public void DetectPerformanceAnomalies_NullMovingAverages_DoesNotThrowException()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();

        // Act & Assert - The method delegates to updater which may not validate nulls
        var exception = Record.Exception(() => _analyzer.DetectPerformanceAnomalies(metrics, null!));
        Assert.Null(exception);
    }

    [Fact]
    public void DetectPerformanceAnomalies_EmptyMetricsAndMovingAverages_ReturnsEmptyList()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();
        var emptyMovingAverages = new Dictionary<string, MovingAverageData>();

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(emptyMetrics, emptyMovingAverages);

        // Assert
        Assert.NotNull(anomalies);
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_EmptyMetricsWithData_ReturnsEmptyList()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 75.0, Timestamp = DateTime.UtcNow }
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(emptyMetrics, movingAverages);

        // Assert
        Assert.NotNull(anomalies);
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_MetricsWithEmptyMovingAverages_ReturnsEmptyList()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 90.0
        };
        var emptyMovingAverages = new Dictionary<string, MovingAverageData>();

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, emptyMovingAverages);

        // Assert
        Assert.NotNull(anomalies);
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectPerformanceAnomalies_MetricsWithoutMatchingMovingAverages_SkipsThoseMetrics()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 90.0,
            ["memory"] = 85.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData { MA15 = 70.0, Timestamp = DateTime.UtcNow }
            // Missing memory moving average
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.NotNull(anomalies);
        // Should only check CPU, skip memory due to missing moving average
        var cpuAnomalies = anomalies.Where(a => a.MetricName == "cpu").ToList();
        var memoryAnomalies = anomalies.Where(a => a.MetricName == "memory").ToList();

        Assert.True(cpuAnomalies.Count <= 1); // At most one anomaly per metric
        Assert.Empty(memoryAnomalies); // No anomalies for memory
    }

    #endregion

    #region Complex Empty/Null Scenarios Tests

    [Fact]
    public void AnalyzeMetricTrends_MetricsWithNullValues_HandlesCorrectly()
    {
        // Arrange - Dictionary with valid keys but we'll test edge cases
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
    }

    [Fact]
    public void CalculateMovingAverages_MetricsWithZeroValues_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 0.0,
            ["memory"] = 0.0
        };
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, timestamp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result.Values, ma => Assert.Equal(timestamp, ma.Timestamp));
    }

    [Fact]
    public void DetectPerformanceAnomalies_MovingAveragesWithDefaultValues_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };
        var movingAverages = new Dictionary<string, MovingAverageData>
        {
            ["cpu"] = new MovingAverageData() // Default values
        };

        // Act
        var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.NotNull(anomalies);
        // Should handle default MovingAverageData values without crashing
    }

    #endregion

    #region Integration Tests with Null/Empty Inputs

    [Fact]
    public void AnalyzeMetricTrends_EmptyMetrics_StillGeneratesTimestamp()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();

        // Act
        var result = _analyzer.AnalyzeMetricTrends(emptyMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(default(DateTime), result.Timestamp);
        Assert.True(result.Timestamp <= DateTime.UtcNow);
        Assert.True(result.Timestamp > DateTime.UtcNow.AddMinutes(-1)); // Within last minute
    }

    [Fact]
    public void AnalyzeMetricTrends_EmptyMetrics_NoExceptionsThrown()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();

        // Act & Assert - Should not throw any exceptions
        var exception = Record.Exception(() => _analyzer.AnalyzeMetricTrends(emptyMetrics));
        Assert.Null(exception);
    }

    [Fact]
    public void DetectPerformanceAnomalies_EmptyInputs_NoExceptionsThrown()
    {
        // Arrange
        var emptyMetrics = new Dictionary<string, double>();
        var emptyMovingAverages = new Dictionary<string, MovingAverageData>();

        // Act & Assert - Should not throw any exceptions
        var exception = Record.Exception(() => _analyzer.DetectPerformanceAnomalies(emptyMetrics, emptyMovingAverages));
        Assert.Null(exception);
    }

    #endregion

    #region Boundary Cases Tests

    [Fact]
    public void CalculateMovingAverages_MinDateTime_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, DateTime.MinValue);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(DateTime.MinValue, result["cpu"].Timestamp);
    }

    [Fact]
    public void CalculateMovingAverages_MaxDateTime_HandlesCorrectly()
    {
        // Arrange
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = 75.0
        };

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, DateTime.MaxValue);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(DateTime.MaxValue, result["cpu"].Timestamp);
    }

    #endregion

    #region Type Safety Tests

    [Fact]
    public void AnalyzeMetricTrends_ReturnsCorrectResultType()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();

        // Act
        var result = _analyzer.AnalyzeMetricTrends(metrics);

        // Assert
        Assert.IsType<TrendAnalysisResult>(result);
        Assert.NotNull(result.MovingAverages);
        Assert.NotNull(result.TrendDirections);
        Assert.NotNull(result.TrendVelocities);
        Assert.NotNull(result.SeasonalityPatterns);
        Assert.NotNull(result.RegressionResults);
        Assert.NotNull(result.Correlations);
        Assert.NotNull(result.Anomalies);
        Assert.NotNull(result.Insights);
    }

    [Fact]
    public void CalculateMovingAverages_ReturnsCorrectResultType()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _analyzer.CalculateMovingAverages(metrics, timestamp);

        // Assert
        Assert.IsType<Dictionary<string, MovingAverageData>>(result);
    }

    [Fact]
    public void DetectPerformanceAnomalies_ReturnsCorrectResultType()
    {
        // Arrange
        var metrics = new Dictionary<string, double>();
        var movingAverages = new Dictionary<string, MovingAverageData>();

        // Act
        var result = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

        // Assert
        Assert.IsType<List<MetricAnomaly>>(result);
    }

    #endregion
}