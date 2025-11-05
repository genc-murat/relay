using System;
using System.Collections.Generic;
using Relay.Core.AI.Optimization.Services;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services
{
    public class MetricsCollectionOptionsTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var options = new MetricsCollectionOptions();

            // Assert
            Assert.NotNull(options.EnabledCollectors);
            Assert.Contains("CpuMetricsCollector", options.EnabledCollectors);
            Assert.Contains("MemoryMetricsCollector", options.EnabledCollectors);
            Assert.Contains("NetworkMetricsCollector", options.EnabledCollectors);
            Assert.Contains("DiskMetricsCollector", options.EnabledCollectors);
            Assert.Contains("SystemLoadMetricsCollector", options.EnabledCollectors);

            Assert.Equal(TimeSpan.FromSeconds(5), options.DefaultCollectionInterval);
            Assert.NotNull(options.CollectorIntervals);
            Assert.Empty(options.CollectorIntervals);
            Assert.Equal(1000, options.MaxHistorySize);
            Assert.True(options.EnableRealTimePublishing);
            Assert.True(options.EnableAggregation);
            Assert.Equal(TimeSpan.FromMinutes(1), options.AggregationWindow);
            Assert.True(options.EnableHealthScoring);
            Assert.Equal(TimeSpan.FromSeconds(30), options.HealthScoreInterval);
            Assert.True(options.EnablePredictiveAnalysis);
            Assert.Equal(TimeSpan.FromMinutes(5), options.PredictionAnalysisInterval);
        }

        [Fact]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var options = new MetricsCollectionOptions();
            var customCollectors = new HashSet<string> { "CustomCollector1", "CustomCollector2" };
            var customInterval = TimeSpan.FromSeconds(10);
            var customCollectorIntervals = new Dictionary<string, TimeSpan>
            {
                ["CustomCollector1"] = TimeSpan.FromSeconds(15)
            };
            const int customHistorySize = 500;
            const bool customRealTimePublishing = false;
            const bool customAggregation = false;
            var customAggregationWindow = TimeSpan.FromMinutes(2);
            const bool customHealthScoring = false;
            var customHealthScoreInterval = TimeSpan.FromMinutes(1);
            const bool customPredictiveAnalysis = false;
            var customPredictionAnalysisInterval = TimeSpan.FromMinutes(10);

            // Act
            options.EnabledCollectors = customCollectors;
            options.DefaultCollectionInterval = customInterval;
            options.CollectorIntervals = customCollectorIntervals;
            options.MaxHistorySize = customHistorySize;
            options.EnableRealTimePublishing = customRealTimePublishing;
            options.EnableAggregation = customAggregation;
            options.AggregationWindow = customAggregationWindow;
            options.EnableHealthScoring = customHealthScoring;
            options.HealthScoreInterval = customHealthScoreInterval;
            options.EnablePredictiveAnalysis = customPredictiveAnalysis;
            options.PredictionAnalysisInterval = customPredictionAnalysisInterval;

            // Assert
            Assert.Same(customCollectors, options.EnabledCollectors);
            Assert.Equal(customInterval, options.DefaultCollectionInterval);
            Assert.Same(customCollectorIntervals, options.CollectorIntervals);
            Assert.Equal(customHistorySize, options.MaxHistorySize);
            Assert.Equal(customRealTimePublishing, options.EnableRealTimePublishing);
            Assert.Equal(customAggregation, options.EnableAggregation);
            Assert.Equal(customAggregationWindow, options.AggregationWindow);
            Assert.Equal(customHealthScoring, options.EnableHealthScoring);
            Assert.Equal(customHealthScoreInterval, options.HealthScoreInterval);
            Assert.Equal(customPredictiveAnalysis, options.EnablePredictiveAnalysis);
            Assert.Equal(customPredictionAnalysisInterval, options.PredictionAnalysisInterval);
        }

        [Fact]
        public void HealthScoringOptions_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var options = new HealthScoringOptions();

            // Assert
            Assert.NotNull(options.Weights);
            Assert.NotNull(options.Thresholds);
        }

        [Fact]
        public void HealthScoringOptions_Properties_ShouldBeSettable()
        {
            // Arrange
            var options = new HealthScoringOptions();
            var customWeights = new HealthWeights();
            var customThresholds = new HealthThresholds();

            // Act
            options.Weights = customWeights;
            options.Thresholds = customThresholds;

            // Assert
            Assert.Same(customWeights, options.Weights);
            Assert.Same(customThresholds, options.Thresholds);
        }

        [Fact]
        public void HealthWeights_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var weights = new HealthWeights();

            // Assert
            Assert.Equal(0.25, weights.Performance);
            Assert.Equal(0.25, weights.Reliability);
            Assert.Equal(0.20, weights.Scalability);
            Assert.Equal(0.20, weights.Security);
            Assert.Equal(0.10, weights.Maintainability);
        }

        [Fact]
        public void HealthWeights_Properties_ShouldBeSettable()
        {
            // Arrange
            var weights = new HealthWeights();
            const double customPerformance = 0.30;
            const double customReliability = 0.20;
            const double customScalability = 0.25;
            const double customSecurity = 0.15;
            const double customMaintainability = 0.10;

            // Act
            weights.Performance = customPerformance;
            weights.Reliability = customReliability;
            weights.Scalability = customScalability;
            weights.Security = customSecurity;
            weights.Maintainability = customMaintainability;

            // Assert
            Assert.Equal(customPerformance, weights.Performance);
            Assert.Equal(customReliability, weights.Reliability);
            Assert.Equal(customScalability, weights.Scalability);
            Assert.Equal(customSecurity, weights.Security);
            Assert.Equal(customMaintainability, weights.Maintainability);
        }

        [Fact]
        public void HealthThresholds_Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var thresholds = new HealthThresholds();

            // Assert
            Assert.Equal(0.9, thresholds.Excellent);
            Assert.Equal(0.8, thresholds.Good);
            Assert.Equal(0.7, thresholds.Fair);
            Assert.Equal(0.6, thresholds.Poor);
        }

        [Fact]
        public void HealthThresholds_Properties_ShouldBeSettable()
        {
            // Arrange
            var thresholds = new HealthThresholds();
            const double customExcellent = 0.95;
            const double customGood = 0.85;
            const double customFair = 0.75;
            const double customPoor = 0.65;

            // Act
            thresholds.Excellent = customExcellent;
            thresholds.Good = customGood;
            thresholds.Fair = customFair;
            thresholds.Poor = customPoor;

            // Assert
            Assert.Equal(customExcellent, thresholds.Excellent);
            Assert.Equal(customGood, thresholds.Good);
            Assert.Equal(customFair, thresholds.Fair);
            Assert.Equal(customPoor, thresholds.Poor);
        }
    }
}