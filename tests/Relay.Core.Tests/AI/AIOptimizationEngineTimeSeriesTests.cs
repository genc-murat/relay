using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineTimeSeriesTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void UpdateTimeSeriesForecastingModels_Should_Execute_Without_Errors()
        {
            // Arrange - Create metrics for testing
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.85,
                ["LearningRate"] = 0.1,
                ["OptimizationEffectiveness"] = 0.75,
                ["SystemStability"] = 0.9
            };

            // Act - Call UpdateTimeSeriesForecastingModels directly using reflection
            var method = _engine.GetType().GetMethod("UpdateTimeSeriesForecastingModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task UpdateTimeSeriesForecastingModels_Should_Handle_Insufficient_Data()
        {
            // Arrange - Create metrics but ensure insufficient time-series data
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.85,
                ["LearningRate"] = 0.1
            };

            // Clear any existing time-series data
            var timeSeriesDataField = _engine.GetType().GetField("_metricTimeSeriesData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeSeriesQueue = timeSeriesDataField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<object>;
            if (timeSeriesQueue != null)
            {
                while (timeSeriesQueue.TryDequeue(out _)) { }
            }

            // Act - Call UpdateTimeSeriesForecastingModels with insufficient data
            var method = _engine.GetType().GetMethod("UpdateTimeSeriesForecastingModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics });

            // Assert - Engine should remain functional even with insufficient data
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify we can still get insights
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task UpdateTimeSeriesForecastingModels_Should_Handle_Exception_Gracefully()
        {
            // Arrange - Create metrics that might cause exceptions in time-series operations
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.85,
                ["LearningRate"] = 0.1
            };

            // Act - Call UpdateTimeSeriesForecastingModels multiple times to exercise error handling
            var method = _engine.GetType().GetMethod("UpdateTimeSeriesForecastingModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 3; i++)
            {
                method?.Invoke(_engine, new object[] { metrics });
            }

            // Assert - Engine should remain functional even if time-series operations fail
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }
    }
}
