using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines
{
    public class TrendAnalyzerTests
    {
        private readonly ILogger<TrendAnalyzer> _logger;
        private readonly TrendAnalyzer _analyzer;

        public TrendAnalyzerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<TrendAnalyzer>();
            _analyzer = new TrendAnalyzer(_logger);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TrendAnalyzer(null!));
        }

        #endregion

        #region AnalyzeMetricTrends Tests

        [Fact]
        public void AnalyzeMetricTrends_Should_Return_Result_With_Timestamp_When_Metrics_Are_Empty()
        {
            // Arrange
            var metrics = new Dictionary<string, double>();

            // Act
            var result = _analyzer.AnalyzeMetricTrends(metrics);

            // Assert
            Assert.NotEqual(default(DateTime), result.Timestamp);
            Assert.Empty(result.MovingAverages);
            Assert.Empty(result.TrendDirections);
            Assert.Empty(result.TrendVelocities);
            Assert.Empty(result.SeasonalityPatterns);
            Assert.Empty(result.RegressionResults);
            Assert.Empty(result.Correlations);
            Assert.Empty(result.Anomalies);
        }

        [Fact]
        public void AnalyzeMetricTrends_Should_Return_Populated_Result_When_Metrics_Are_Provided()
        {
            // Arrange
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 75.5,
                ["memory"] = 85.2,
                ["latency"] = 120.0
            };

            // Act
            var result = _analyzer.AnalyzeMetricTrends(metrics);

            // Assert
            Assert.NotEqual(default(DateTime), result.Timestamp);
            Assert.Equal(3, result.MovingAverages.Count);
            Assert.Equal(3, result.TrendDirections.Count);
            Assert.Equal(3, result.TrendVelocities.Count);
            Assert.Equal(3, result.SeasonalityPatterns.Count);
            Assert.Equal(3, result.RegressionResults.Count);
            // Correlations might be empty if no correlations found
            // Anomalies might be empty
        }

        [Fact]
        public void AnalyzeMetricTrends_Should_Handle_Exception_And_Return_Basic_Result()
        {
            // Arrange - we can't easily force an exception, but the method has try-catch
            // This test ensures the structure is correct even if internals fail
            var metrics = new Dictionary<string, double>
            {
                ["test"] = double.NaN // This might cause issues in calculations
            };

            // Act
            var result = _analyzer.AnalyzeMetricTrends(metrics);

            // Assert
            Assert.NotEqual(default(DateTime), result.Timestamp);
            // Result should still be valid even if calculations fail
        }

        #endregion

        #region CalculateMovingAverages Tests

        [Fact]
        public void CalculateMovingAverages_Should_Return_Data_For_All_Metrics()
        {
            // Arrange
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 80.0,
                ["memory"] = 90.0
            };
            var timestamp = DateTime.UtcNow;

            // Act
            var result = _analyzer.CalculateMovingAverages(metrics, timestamp);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey("cpu"));
            Assert.True(result.ContainsKey("memory"));
            Assert.Equal(timestamp, result["cpu"].Timestamp);
            Assert.Equal(80.0, result["cpu"].CurrentValue);
            Assert.Equal(90.0, result["memory"].CurrentValue);
        }

        #endregion

        #region DetectPerformanceAnomalies Tests

        [Fact]
        public void DetectPerformanceAnomalies_Should_Return_Empty_List_When_No_Anomalies()
        {
            // Arrange
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 75.0
            };
            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["cpu"] = new MovingAverageData
                {
                    MA15 = 75.0, // Same value, no anomaly
                    Timestamp = DateTime.UtcNow
                }
            };

            // Act
            var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

            // Assert
            Assert.Empty(anomalies);
        }

        [Fact]
        public void DetectPerformanceAnomalies_Should_Detect_High_Anomaly()
        {
            // Arrange
            var metrics = new Dictionary<string, double>
            {
                ["cpu"] = 100.0
            };
            var movingAverages = new Dictionary<string, MovingAverageData>
            {
                ["cpu"] = new MovingAverageData
                {
                    MA15 = 50.0, // Large difference
                    Timestamp = DateTime.UtcNow
                }
            };

            // Act
            var anomalies = _analyzer.DetectPerformanceAnomalies(metrics, movingAverages);

            // Assert
            Assert.Single(anomalies);
            var anomaly = anomalies[0];
            Assert.Equal("cpu", anomaly.MetricName);
            Assert.Equal(100.0, anomaly.CurrentValue);
            Assert.Equal(50.0, anomaly.ExpectedValue);
            Assert.True(anomaly.ZScore > 3.0);
        }

        #endregion

        #region CalculateMovingAverage Tests

        [Fact]
        public void CalculateMovingAverage_Should_Return_Current_Value()
        {
            // Act
            var result = _analyzer.CalculateMovingAverage("test", 42.0, 5);

            // Assert
            Assert.Equal(42.0, result);
        }

        #endregion

        #region CalculateExponentialMovingAverage Tests

        [Fact]
        public void CalculateExponentialMovingAverage_Should_Return_Current_Value()
        {
            // Act
            var result = _analyzer.CalculateExponentialMovingAverage("test", 42.0, 0.3);

            // Assert
            Assert.Equal(42.0, result);
        }

        #endregion

        #region CalculateTrendStrength Tests

        [Fact]
        public void CalculateTrendStrength_Should_Calculate_Correctly()
        {
            // Act
            var result = _analyzer.CalculateTrendStrength(10.0, 8.0, 6.0);

            // Assert
            Assert.Equal(0.333, result, 3); // (8-6)/6 = 2/6 ≈ 0.333
        }

        [Fact]
        public void CalculateTrendStrength_Should_Return_Zero_When_MA15_Is_Zero()
        {
            // Act
            var result = _analyzer.CalculateTrendStrength(10.0, 8.0, 0.0);

            // Assert
            Assert.Equal(0.0, result);
        }

        #endregion

        #region IsWithinSeasonalExpectation Tests

        [Fact]
        public void IsWithinSeasonalExpectation_Should_Return_True()
        {
            // Act
            var result = _analyzer.IsWithinSeasonalExpectation(100.0, 1.5);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region CalculateLinearRegression Tests

        [Fact]
        public void CalculateLinearRegression_Should_Return_Default_Result()
        {
            // Act
            var result = _analyzer.CalculateLinearRegression("test", DateTime.UtcNow);

            // Assert
            Assert.Equal(0.0, result.Slope);
            Assert.Equal(0.0, result.Intercept);
            Assert.Equal(0.0, result.RSquared);
        }

        #endregion

        #region CalculateCorrelation Tests

        [Fact]
        public void CalculateCorrelation_Should_Return_Zero()
        {
            // Act
            var result = _analyzer.CalculateCorrelation("metric1", "metric2");

            // Assert
            Assert.Equal(0.0, result);
        }

        #endregion
    }
}