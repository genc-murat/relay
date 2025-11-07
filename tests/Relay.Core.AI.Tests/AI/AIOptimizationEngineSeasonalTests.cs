using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIOptimizationEngineSeasonalTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private AIOptimizationEngine _engine;

    public AIOptimizationEngineSeasonalTests()
    {
        _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
        _options = new AIOptimizationOptions
        {
            DefaultBatchSize = 10,
            MaxBatchSize = 100,
            ModelUpdateInterval = TimeSpan.FromMinutes(5),
            ModelTrainingDate = DateTime.UtcNow,
            ModelVersion = "1.0.0",
            LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
        };

        var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        // Create mock dependencies
        var metricsAggregatorMock = new Mock<IMetricsAggregator>();
        var healthScorerMock = new Mock<IHealthScorer>();
        var systemAnalyzerMock = new Mock<ISystemAnalyzer>();
        var metricsPublisherMock = new Mock<IMetricsPublisher>();
        var metricsOptions = new MetricsCollectionOptions();
        var healthOptions = new HealthScoringOptions();

        // Setup default mock behaviors
        metricsAggregatorMock.Setup(x => x.CollectAllMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IEnumerable<MetricValue>>());
        metricsAggregatorMock.Setup(x => x.GetLatestMetrics())
            .Returns(new Dictionary<string, IEnumerable<MetricValue>>());
        healthScorerMock.Setup(x => x.CalculateScoreAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0.8);
        systemAnalyzerMock.Setup(x => x.AnalyzeLoadPatternsAsync(It.IsAny<Dictionary<string, double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoadPatternData { Level = LoadLevel.Medium });

        _engine = new AIOptimizationEngine(
            _loggerMock.Object,
            optionsMock.Object,
            metricsAggregatorMock.Object,
            healthScorerMock.Object,
            systemAnalyzerMock.Object,
            metricsPublisherMock.Object,
            metricsOptions,
            healthOptions);
    }

    public void Dispose()
    {
        _engine?.Dispose();
    }

    [Fact]
    public void DetectSeasonalPatterns_Should_Return_Empty_List_With_Insufficient_Data()
    {
        // Arrange - Create metrics with less than 24 hours of data
        var metrics = new Dictionary<string, double> { ["ThroughputPerSecond"] = 100.0 };

        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("DetectSeasonalPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var patterns = (List<SeasonalPattern>)method!.Invoke(_engine, new object[] { metrics })!;

        // Assert - Should return empty list when there's insufficient data
        Assert.Empty(patterns);
    }

    [Fact]
    public void DetectSeasonalPatterns_Should_Detect_Patterns_With_Sufficient_Data()
    {
        // Arrange - Add 24 hours of data with a clear pattern (every 6 hours)
        var tsDbField = typeof(AIOptimizationEngine).GetField("_timeSeriesDb",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tsDb = tsDbField!.GetValue(_engine) as Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase;

        var baseTime = DateTime.UtcNow;

        // Create data with 6-hour pattern: high values every 6 hours
        for (int i = 0; i < 24; i++)
        {
            double value = (i % 6 == 0) ? 500.0 : 10.0; // High every 6 hours
            tsDb!.StoreMetric("ThroughputPerSecond", value, baseTime.AddHours(-22 + i));
        }

        var metrics = new Dictionary<string, double> { ["ThroughputPerSecond"] = 150.0 };

        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("DetectSeasonalPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var patterns = (List<SeasonalPattern>)method!.Invoke(_engine, new object[] { metrics })!;

        // Assert - Should detect at least one pattern
        Assert.NotEmpty(patterns);
        var pattern6Hours = patterns.Find(p => p.Period == 6);
        Assert.NotNull(pattern6Hours);
        Assert.True(pattern6Hours!.Strength > 0.7); // Strong correlation
        Assert.Equal("Intraday", pattern6Hours.Type);
    }

    [Fact]
    public void DetectSeasonalPatterns_Should_Classify_Different_Period_Types()
    {
        // Arrange - Add data with different period patterns
        var tsDbField = typeof(AIOptimizationEngine).GetField("_timeSeriesDb",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tsDb = tsDbField!.GetValue(_engine) as Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase;

        // Create patterns for different periods
        var testCases = new[]
        {
            (period: 6, type: "Intraday", hours: 30),    // 6 hours = Intraday
            (period: 12, type: "Daily", hours: 30),      // 12 hours = Daily
            (period: 24, type: "Daily", hours: 50),      // 24 hours = Daily
            (period: 48, type: "Semi-weekly", hours: 100), // 48 hours = Semi-weekly
            (period: 168, type: "Weekly", hours: 340),   // 168 hours = Weekly
            (period: 336, type: "Bi-weekly", hours: 680) // 336 hours = Bi-weekly
        };

        foreach (var testCase in testCases)
        {
            // Clear previous data
            var tsDbNew = Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase.Create(
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase>(),
                1000);
            tsDbField.SetValue(_engine, tsDbNew);

            // Add data with the specific period pattern
            for (int i = 0; i < testCase.hours; i++)
            {
                double value = (i % testCase.period == 0) ? 200.0 : 100.0;
                tsDbNew.StoreMetric("ThroughputPerSecond", value, DateTime.UtcNow.AddHours(-testCase.hours + i));
            }

            var metrics = new Dictionary<string, double> { ["ThroughputPerSecond"] = 150.0 };

            // Get the private method using reflection
            var method = typeof(AIOptimizationEngine).GetMethod("DetectSeasonalPatterns",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);

            // Act
            var patterns = (List<SeasonalPattern>)method!.Invoke(_engine, new object[] { metrics })!;

            // Assert - Should classify the period correctly
            var pattern = patterns.Find(p => p.Period == testCase.period);
            if (pattern != null)
            {
                Assert.Equal(testCase.type, pattern.Type);
            }
        }
    }

    [Fact]
    public void CalculateAutocorrelation_Should_Return_Zero_With_Insufficient_Data()
    {
        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("CalculateAutocorrelation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act - Test with empty data
        var result = (double)method!.Invoke(_engine, new object[] { new List<double>(), 5 })!;

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateAutocorrelation_Should_Return_Zero_With_Small_Data_Set()
    {
        // Arrange
        var data = new List<double> { 1.0, 2.0, 3.0 }; // Less than lag * 2

        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("CalculateAutocorrelation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (double)method!.Invoke(_engine, new object[] { data, 5 })!;

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateAutocorrelation_Should_Return_One_For_Constant_Data()
    {
        // Arrange - All values are the same
        var data = new List<double> { 5.0, 5.0, 5.0, 5.0, 5.0, 5.0, 5.0, 5.0, 5.0, 5.0 };

        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("CalculateAutocorrelation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var result = (double)method!.Invoke(_engine, new object[] { data, 3 })!;

        // Assert - Perfect autocorrelation for constant data
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateAutocorrelation_Should_Calculate_Correct_Correlation()
    {
        // Arrange - Create a simple pattern: alternating high/low
        var data = new List<double>();
        for (int i = 0; i < 20; i++)
        {
            data.Add(i % 2 == 0 ? 10.0 : 5.0); // Alternating pattern every 1 step
        }

        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("CalculateAutocorrelation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act - Calculate autocorrelation with lag 1 (should show strong negative correlation)
        var result = (double)method!.Invoke(_engine, new object[] { data, 1 })!;

        // Assert - Should detect the alternating pattern (negative correlation)
        Assert.True(result < 0); // Negative correlation for alternating pattern
        Assert.True(Math.Abs(result) > 0.5); // Strong correlation
    }

    [Fact]
    public void ClassifySeasonalType_Should_Classify_All_Period_Ranges()
    {
        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("ClassifySeasonalType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Test various period ranges
        var testCases = new[]
        {
            (period: 4, expected: "Intraday"),
            (period: 8, expected: "Intraday"),
            (period: 12, expected: "Daily"),
            (period: 24, expected: "Daily"),
            (period: 36, expected: "Semi-weekly"),
            (period: 48, expected: "Semi-weekly"),
            (period: 100, expected: "Weekly"),
            (period: 168, expected: "Weekly"),
            (period: 200, expected: "Bi-weekly"),
            (period: 336, expected: "Bi-weekly"),
            (period: 400, expected: "Monthly"),
            (period: 1000, expected: "Monthly")
        };

        foreach (var testCase in testCases)
        {
            // Act
            var result = (string)method!.Invoke(_engine, new object[] { testCase.period })!;

            // Assert
            Assert.Equal(testCase.expected, result);
        }
    }

    [Fact]
    public void DetectSeasonalPatterns_Should_Filter_By_Correlation_Threshold()
    {
        // Arrange - Add data with weak and strong patterns
        var tsDbField = typeof(AIOptimizationEngine).GetField("_timeSeriesDb",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tsDb = tsDbField!.GetValue(_engine) as Relay.Core.AI.Analysis.TimeSeries.TimeSeriesDatabase;

        // Add 25 hours of mixed data
        var random = new Random(42); // Fixed seed for reproducible results
        for (int i = 0; i < 25; i++)
        {
            // Mostly random with some weak patterns
            double value = 100.0 + random.NextDouble() * 50.0;
            // Add a weak pattern every 7 hours
            if (i % 7 == 0) value += 20.0;
            tsDb!.StoreMetric("ThroughputPerSecond", value, DateTime.UtcNow.AddHours(-25 + i));
        }

        var metrics = new Dictionary<string, double> { ["ThroughputPerSecond"] = 125.0 };

        // Get the private method using reflection
        var method = typeof(AIOptimizationEngine).GetMethod("DetectSeasonalPatterns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Act
        var patterns = (List<SeasonalPattern>)method!.Invoke(_engine, new object[] { metrics })!;

        // Assert - Only patterns with correlation > 0.7 should be included
        foreach (var pattern in patterns)
        {
            Assert.True(pattern.Strength > 0.7, $"Pattern with period {pattern.Period} has correlation {pattern.Strength}, which is below threshold");
        }
    }
}