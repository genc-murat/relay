using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines
{
    public class TrendAnalyzerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TrendAnalyzer _analyzer;

        public TrendAnalyzerTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddTrendAnalysis();

            _serviceProvider = services.BuildServiceProvider();
            _analyzer = _serviceProvider.GetRequiredService<ITrendAnalyzer>() as TrendAnalyzer
                ?? throw new InvalidOperationException("Could not resolve TrendAnalyzer");
        }

        #region Constructor Tests

        [Fact]
        public void Service_Should_Be_Registered_In_DI_Container()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTrendAnalysis();
            var provider = services.BuildServiceProvider();

            // Act & Assert
            var analyzer = provider.GetService<ITrendAnalyzer>();
            Assert.NotNull(analyzer);
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


    }
}