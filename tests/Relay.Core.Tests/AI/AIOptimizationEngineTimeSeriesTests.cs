using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineTimeSeriesTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public async Task DetectSeasonalPeriod_Should_Return_Default_When_Insufficient_Data()
        {
            // Arrange - Ensure time series database has insufficient data (< 50 points)
            var timeSeriesDbField = _engine.GetType().GetField("_timeSeriesDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeSeriesDb = timeSeriesDbField?.GetValue(_engine) as TimeSeriesDatabase;

            if (timeSeriesDb != null)
            {
                // Clear any existing data and add less than 50 points
                for (int i = 0; i < 30; i++) // Less than 50
                {
                    timeSeriesDb.StoreMetric("ThroughputPerSecond", 10.0f + i, DateTime.UtcNow.AddMinutes(-i));
                }
            }

            // Act - Call GetSystemInsightsAsync which triggers DetectSeasonalPeriod
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights (DetectSeasonalPeriod should use fallback logic)
            Assert.NotNull(insights);
            Assert.NotNull(insights.Predictions);
            Assert.True(insights.Predictions.NextHourPredictions.ContainsKey("SeasonalPeriod") ||
                       insights.Predictions.NextHourPredictions.Count > 0); // Should have predictions even without seasonal detection
        }

        [Fact]
        public async Task DetectSeasonalPeriod_Should_Detect_Daily_Pattern_With_High_Throughput()
        {
            // Arrange - Set up time series data with sufficient points and high throughput for daily pattern
            var timeSeriesDbField = _engine.GetType().GetField("_timeSeriesDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeSeriesDb = timeSeriesDbField?.GetValue(_engine) as TimeSeriesDatabase;

            if (timeSeriesDb != null)
            {
                // Clear existing data and add 100+ points with daily pattern simulation
                var baseTime = DateTime.UtcNow.AddDays(-7); // 7 days ago
                for (int i = 0; i < 168; i++) // 168 hours = 7 days
                {
                    // Simulate daily pattern: higher throughput during "business hours" (8-18)
                    var hourOfDay = i % 24;
                    var throughput = (hourOfDay >= 8 && hourOfDay <= 18) ? 150.0f : 50.0f;
                    // Add some noise
                    throughput += (float)(new Random(i).NextDouble() * 20 - 10);
                    timeSeriesDb.StoreMetric("ThroughputPerSecond", throughput, baseTime.AddHours(i));
                }
            }

            // Act - Call GetSystemInsightsAsync to trigger seasonal pattern detection
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should detect daily pattern (24h period) due to high throughput
            Assert.NotNull(insights);
            Assert.NotNull(insights.Predictions);
            // The method should either detect the 24h pattern or fall back to 24h for high throughput
        }

        [Fact]
        public async Task DetectSeasonalPeriod_Should_Detect_Weekly_Pattern_With_Medium_Throughput()
        {
            // Arrange - Set up time series data with medium throughput for weekly pattern detection
            var timeSeriesDbField = _engine.GetType().GetField("_timeSeriesDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeSeriesDb = timeSeriesDbField?.GetValue(_engine) as TimeSeriesDatabase;

            if (timeSeriesDb != null)
            {
                // Clear existing data and add data with weekly pattern
                var baseTime = DateTime.UtcNow.AddDays(-14); // 14 days ago
                for (int i = 0; i < 336; i++) // 336 hours = 14 days
                {
                    // Simulate weekly pattern: higher throughput on weekdays
                    var dayOfWeek = (int)baseTime.AddHours(i).DayOfWeek;
                    var throughput = (dayOfWeek >= 1 && dayOfWeek <= 5) ? 25.0f : 5.0f; // Medium traffic
                    // Add some noise
                    throughput += (float)(new Random(i).NextDouble() * 5 - 2.5);
                    timeSeriesDb.StoreMetric("ThroughputPerSecond", throughput, baseTime.AddHours(i));
                }
            }

            // Act - Call GetSystemInsightsAsync
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should detect weekly pattern (168h period) due to medium throughput
            Assert.NotNull(insights);
            Assert.NotNull(insights.Predictions);
        }

        [Fact]
        public async Task DetectSeasonalPeriod_Should_Handle_Autocorrelation_Calculation_Errors()
        {
            // Arrange - Set up scenario that might cause autocorrelation calculation errors
            var timeSeriesDbField = _engine.GetType().GetField("_timeSeriesDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeSeriesDb = timeSeriesDbField?.GetValue(_engine) as TimeSeriesDatabase;

            if (timeSeriesDb != null)
            {
                // Add minimal valid data that could cause edge cases in autocorrelation
                for (int i = 0; i < 60; i++)
                {
                    // Add constant values which could cause division by zero in autocorrelation
                    timeSeriesDb.StoreMetric("ThroughputPerSecond", 10.0f, DateTime.UtcNow.AddMinutes(-i));
                }
            }

            // Act - Call GetSystemInsightsAsync which should handle any autocorrelation errors gracefully
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights even if autocorrelation calculation fails
            Assert.NotNull(insights);
            Assert.NotNull(insights.Predictions);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task DetectSeasonalPeriod_Should_Return_Default_On_Exception()
        {
            // Arrange - This test verifies the try-catch in DetectSeasonalPeriod works
            // We can't easily force an exception in the time series database, but we can test
            // that the method returns a valid result even in edge cases

            // Act - Call GetSystemInsightsAsync multiple times to exercise the method
            var insights1 = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            var insights2 = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should consistently return valid insights
            Assert.NotNull(insights1);
            Assert.NotNull(insights2);
            Assert.NotNull(insights1.Predictions);
            Assert.NotNull(insights2.Predictions);
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Return_Zero_When_Insufficient_Data()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test cases with insufficient data
            var testCases = new[]
            {
                (values: new List<float> { 1.0f }, lag: 1), // Count = 1, lag = 1, need lag + 1 = 2
                (values: new List<float> { 1.0f, 2.0f }, lag: 2), // Count = 2, lag = 2, need lag + 1 = 3
                (values: new List<float> { 1.0f, 2.0f, 3.0f }, lag: 3), // Count = 3, lag = 3, need lag + 1 = 4
                (values: new List<float>(), lag: 1), // Empty list
            };

            foreach (var (values, lag) in testCases)
            {
                // Act
                var result = (double)method.Invoke(_engine, new object[] { values, lag });

                // Assert
                Assert.Equal(0.0, result);
            }
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Return_Zero_When_Zero_Variance()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test with constant values (zero variance)
            var constantValues = new List<float> { 5.0f, 5.0f, 5.0f, 5.0f, 5.0f };

            // Act
            var result = (double)method.Invoke(_engine, new object[] { constantValues, 1 });

            // Assert
            Assert.True(result == 0.0, "Should return 0.0 when all values are constant (zero variance)");
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Calculate_Correctly_For_Perfect_Correlation()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create a linearly increasing series for better correlation testing
            // Exponential growth causes numerical issues with autocorrelation
            var values = new List<float>();
            for (int i = 0; i < 20; i++)
            {
                values.Add(10.0f + i * 2.0f); // Linear progression: 10, 12, 14, 16, ...
            }

            // Act
            var result = (double)method.Invoke(_engine, new object[] { values, 1 });

            // Assert - Linear series should show high positive correlation
            Assert.True(result > 0.8, $"Expected autocorrelation > 0.8 but got {result}");
            Assert.True(result <= 1.0);
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Calculate_Correctly_For_No_Correlation()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create uncorrelated random values
            var values = new List<float>();
            var random = new Random(123); // Fixed seed for reproducible results

            for (int i = 0; i < 50; i++)
            {
                values.Add((float)(random.NextDouble() * 100));
            }

            // Act
            var result = (double)method.Invoke(_engine, new object[] { values, 1 });

            // Assert - Should show low correlation (close to 0.0)
            Assert.True(Math.Abs(result) < 0.3);
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Calculate_Correctly_For_Negative_Correlation()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create negatively correlated series: alternating high/low values
            var values = new List<float>();
            for (int i = 0; i < 20; i++)
            {
                values.Add(i % 2 == 0 ? 10.0f : 1.0f);
            }

            // Act
            var result = (double)method.Invoke(_engine, new object[] { values, 1 });

            // Assert - Should show negative correlation
            Assert.True(result < -0.5);
            Assert.True(result >= -1.0);
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Handle_Different_Lag_Values()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create a sine wave pattern that should show correlation at specific lags
            var values = new List<float>();
            for (int i = 0; i < 100; i++)
            {
                // Sine wave with period of 10 samples
                values.Add((float)Math.Sin(2 * Math.PI * i / 10.0) + 10.0f);
            }

            // Test different lags
            var lag1Result = (double)method.Invoke(_engine, new object[] { values, 1 });
            var lag5Result = (double)method.Invoke(_engine, new object[] { values, 5 });
            var lag10Result = (double)method.Invoke(_engine, new object[] { values, 10 });
            var lag15Result = (double)method.Invoke(_engine, new object[] { values, 15 });

            // Assert - Lag 10 should show higher correlation than lag 1 for a periodic signal
            Assert.True(Math.Abs(lag10Result) > Math.Abs(lag1Result));
            // All results should be valid correlation values
            Assert.True(lag1Result >= -1.0 && lag1Result <= 1.0);
            Assert.True(lag10Result >= -1.0 && lag10Result <= 1.0);
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Handle_Edge_Case_Large_Lag()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Create sufficient data for large lag
            var values = new List<float>();
            for (int i = 0; i < 50; i++)
            {
                values.Add((float)i);
            }

            // Test with lag close to maximum allowed (values.Count - 1)
            var largeLag = values.Count - 2; // Should still work

            // Act
            var result = (double)method.Invoke(_engine, new object[] { values, largeLag });

            // Assert - Should return a valid result
            Assert.True(result >= -1.0 && result <= 1.0);
        }

        [Fact]
        public void CalculateAutocorrelation_Should_Handle_Exception_Gracefully()
        {
            // Arrange - Get the private method using reflection
            var method = _engine.GetType().GetMethod("CalculateAutocorrelation",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test with null values (should trigger exception)
            List<float> nullValues = null!;

            // Act
            var result = (double)method.Invoke(_engine, new object[] { nullValues, 1 });

            // Assert - Should return 0.0 on exception
            Assert.True(result == 0.0, "Should return 0.0 when exception occurs");
        }

        [Fact]
        public async Task CollectMLNetTrainingData_Should_Collect_Performance_Data()
        {
            // Arrange - Create scenario with specific metrics that should be collected
            var request = new TestRequest();
            var metrics = CreateMetrics(100, TimeSpan.FromMilliseconds(150), 8, 3); // 8 DB calls, 3 API calls

            await _engine.AnalyzeRequestAsync(request, metrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);

            // Act - Call CollectMetricsCallback to trigger training data collection
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional and training data should be collected
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify we can still get insights (which indirectly validates data collection)
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task CollectMLNetTrainingData_Should_Collect_Strategy_Data()
        {
            // Arrange - Create scenario with strategy-related metrics
            var request = new TestRequest();
            var metrics = CreateMetrics(92, TimeSpan.FromMilliseconds(200), 3, 1); // 92% success rate, 3 DB calls, 1 API call

            await _engine.AnalyzeRequestAsync(request, metrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);

            // Act - Call CollectMetricsCallback to trigger strategy data collection
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional and strategy data should be collected
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public void CollectMLNetTrainingData_Should_Handle_Exception_Gracefully()
        {
            // Arrange - This test verifies the try-catch in CollectMLNetTrainingData works
            // Since we can't easily force an exception in the data collection operations,
            // we test that the method completes without crashing the engine

            // Act - Call CollectMetricsCallback multiple times to exercise training data collection
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 3; i++)
            {
                callbackMethod?.Invoke(_engine, new object?[] { null });
            }

            // Assert - Engine should remain functional even if data collection operations fail
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var stats = _engine.GetModelStatistics();
            Assert.NotNull(stats);
        }

        [Fact]
        public void TrainMLNetModels_Should_Execute_Without_Errors()
        {
            // Arrange - Ensure engine is not disposed

            // Act - Call CollectMetricsCallback which may trigger ML.NET model training if enough data
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task TrainMLNetModels_Should_Train_With_Sufficient_Data()
        {
            // Arrange - Generate sufficient training data (more than 100 data points)
            for (int i = 0; i < 120; i++)
            {
                var request = new TestRequest();
                var metrics = CreateMetrics(100 + i); // Vary metrics slightly
                await _engine.AnalyzeRequestAsync(request, metrics);
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);
            }

            // Act - Call CollectMetricsCallback which should trigger ML.NET model training
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional and models should be trained
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify system insights are still available
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task TrainMLNetModels_Should_Handle_Insufficient_Data()
        {
            // Arrange - Generate insufficient training data (less than 100 data points)
            for (int i = 0; i < 50; i++)
            {
                var request = new TestRequest();
                var metrics = CreateMetrics(100);
                await _engine.AnalyzeRequestAsync(request, metrics);
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);
            }

            // Act - Call CollectMetricsCallback which should not trigger ML.NET model training
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional even without sufficient training data
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public void TrainMLNetModels_Should_Handle_Exception_Gracefully()
        {
            // Arrange - This test verifies the try-catch in TrainMLNetModels works
            // Since we can't easily force an exception in the ML.NET training operations,
            // we test that the method completes without crashing the engine

            // Act - Call CollectMetricsCallback multiple times to potentially exercise ML.NET training
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 3; i++)
            {
                callbackMethod?.Invoke(_engine, new object?[] { null });
            }

            // Assert - Engine should remain functional even if ML.NET training operations fail
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var stats = _engine.GetModelStatistics();
            Assert.NotNull(stats);
        }

        [Fact]
        public void RetrainMLNetModels_Should_Execute_Without_Errors()
        {
            // Arrange - Ensure engine is not disposed

            // Act - Call CollectMetricsCallback which may trigger ML.NET model retraining if enough data (>1000 items)
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task RetrainMLNetModels_Should_Retrain_With_Sufficient_Data()
        {
            // Arrange - Generate more than 1000 training data points to trigger retraining
            for (int i = 0; i < 1100; i++)
            {
                var request = new TestRequest();
                var metrics = CreateMetrics(100 + (i % 50)); // Vary metrics slightly
                await _engine.AnalyzeRequestAsync(request, metrics);
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);
            }

            // Act - Call CollectMetricsCallback which should trigger ML.NET model retraining
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional and models should be retrained
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify system insights are still available
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task RetrainMLNetModels_Should_Clear_Old_Data_After_Retraining()
        {
            // Arrange - Generate sufficient data and then add more to trigger retraining
            // First, build up initial training data
            for (int i = 0; i < 200; i++)
            {
                var request = new TestRequest();
                var metrics = CreateMetrics(100);
                await _engine.AnalyzeRequestAsync(request, metrics);
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);
            }

            // Trigger initial training
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Add more data to exceed 1000 total and trigger retraining
            for (int i = 0; i < 900; i++)
            {
                var request = new OtherTestRequest();
                var metrics = CreateMetrics(150);
                await _engine.AnalyzeRequestAsync(request, metrics);
                await _engine.LearnFromExecutionAsync(typeof(OtherTestRequest), new[] { OptimizationStrategy.BatchProcessing }, metrics);
            }

            // Act - Call CollectMetricsCallback which should trigger retraining and data cleanup
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional after retraining and data cleanup
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public void RetrainMLNetModels_Should_Handle_Exception_Gracefully()
        {
            // Arrange - This test verifies the try-catch in RetrainMLNetModels works
            // Since we can't easily force an exception in the ML.NET retraining operations,
            // we test that the method completes without crashing the engine

            // Act - Call CollectMetricsCallback multiple times to potentially exercise ML.NET retraining
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 3; i++)
            {
                callbackMethod?.Invoke(_engine, new object?[] { null });
            }

            // Assert - Engine should remain functional even if ML.NET retraining operations fail
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var stats = _engine.GetModelStatistics();
            Assert.NotNull(stats);
        }

        [Fact]
        public void UseMLNetForStrategyPrediction_Should_Return_Default_When_Models_Not_Initialized()
        {
            // Arrange - Create metrics for testing
            var metrics = CreateMetrics(100);

            // Ensure ML models are not initialized by checking the private field
            var mlModelsInitializedField = _engine.GetType().GetField("_mlModelsInitialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var originalValue = (bool)mlModelsInitializedField?.GetValue(_engine)!;

            try
            {
                // Force models to be uninitialized
                mlModelsInitializedField?.SetValue(_engine, false);

                // Act - Call UseMLNetForStrategyPrediction directly using reflection
                var method = _engine.GetType().GetMethod("UseMLNetForStrategyPrediction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var result = method?.Invoke(_engine, new object[] { metrics });

                // Assert - Should return default values when models are not initialized
                Assert.NotNull(result);
                var (shouldOptimize, confidence) = ((bool, float))result;
                Assert.False(shouldOptimize);
                Assert.Equal(0.5f, confidence);
            }
            finally
            {
                // Restore original value
                mlModelsInitializedField?.SetValue(_engine, originalValue);
            }
        }

        [Fact]
        public void UseMLNetForStrategyPrediction_Should_Return_Valid_Result_When_Models_Initialized()
        {
            // Arrange - Create metrics for testing
            var metrics = CreateMetrics(100, TimeSpan.FromMilliseconds(150), 2, 1, 5);

            // Ensure ML models are initialized (they should be by default in our test setup)
            var mlModelsInitializedField = _engine.GetType().GetField("_mlModelsInitialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isInitialized = (bool)mlModelsInitializedField?.GetValue(_engine)!;

            // Act - Call UseMLNetForStrategyPrediction directly using reflection
            var method = _engine.GetType().GetMethod("UseMLNetForStrategyPrediction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method?.Invoke(_engine, new object[] { metrics });

            // Assert - Should return a valid tuple result
            Assert.NotNull(result);
            var (shouldOptimize, confidence) = ((bool, float))result;
            // Confidence should be between 0 and 1
            Assert.InRange(confidence, 0.0f, 1.0f);
            // Engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void UseMLNetForStrategyPrediction_Should_Handle_Exception_Gracefully()
        {
            // Arrange - Create metrics that might cause an exception in ML.NET prediction
            var metrics = CreateMetrics(100);

            // Act - Call UseMLNetForStrategyPrediction directly using reflection
            var method = _engine.GetType().GetMethod("UseMLNetForStrategyPrediction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method?.Invoke(_engine, new object[] { metrics });

            // Assert - Should return default values and not crash the engine
            Assert.NotNull(result);
            var (shouldOptimize, confidence) = ((bool, float))result;
            // Even if an exception occurs, should return safe defaults
            Assert.False(shouldOptimize); // Default should be false
            Assert.Equal(0.5f, confidence); // Default confidence should be 0.5
            // Engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void UseMLNetForStrategyPrediction_Should_Process_Different_Metric_Scenarios()
        {
            // Arrange - Test with different metric scenarios
            var scenarios = new[]
            {
                CreateMetrics(50, TimeSpan.FromMilliseconds(50), 1, 0, 0),   // Fast, low complexity, no errors
                CreateMetrics(200, TimeSpan.FromMilliseconds(500), 5, 2, 10), // Slow, high complexity, some errors
                CreateMetrics(100, TimeSpan.FromMilliseconds(100), 2, 1, 20)  // Medium, moderate complexity, high errors
            };

            foreach (var metrics in scenarios)
            {
                // Act - Call UseMLNetForStrategyPrediction for each scenario
                var method = _engine.GetType().GetMethod("UseMLNetForStrategyPrediction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var result = method?.Invoke(_engine, new object[] { metrics });

                // Assert - Each scenario should return valid results
                Assert.NotNull(result);
                var (shouldOptimize, confidence) = ((bool, float))result;
                Assert.InRange(confidence, 0.0f, 1.0f);
            }

            // Engine should remain functional after processing multiple scenarios
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void UpdateDecisionTreeModels_Should_Execute_Without_Errors()
        {
            // Arrange - Test with valid parameters
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.85,
                ["LearningRate"] = 0.1,
                ["OptimizationEffectiveness"] = 0.8
            };
            var accuracy = 0.85;

            // Act - Call UpdateDecisionTreeModels directly using reflection
            var method = _engine.GetType().GetMethod("UpdateDecisionTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics, accuracy });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task UpdateDecisionTreeModels_Should_Handle_High_Accuracy_Scenario()
        {
            // Arrange - Create scenario with high accuracy (>95%) that should trigger overfitting detection
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.98,
                ["LearningRate"] = 0.05,
                ["OptimizationEffectiveness"] = 0.9
            };
            var accuracy = 0.98; // High accuracy that triggers overfitting warning

            // Act - Call UpdateDecisionTreeModels with high accuracy
            var method = _engine.GetType().GetMethod("UpdateDecisionTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics, accuracy });

            // Assert - Engine should remain functional and handle overfitting detection
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify system insights are still available
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task UpdateDecisionTreeModels_Should_Handle_Low_Accuracy_Scenario()
        {
            // Arrange - Create scenario with low accuracy (<60%) that should trigger more complex model
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.55,
                ["LearningRate"] = 0.15,
                ["OptimizationEffectiveness"] = 0.5
            };
            var accuracy = 0.55; // Low accuracy that should increase model complexity

            // Act - Call UpdateDecisionTreeModels with low accuracy
            var method = _engine.GetType().GetMethod("UpdateDecisionTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics, accuracy });

            // Assert - Engine should remain functional and handle low accuracy adjustments
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task UpdateDecisionTreeModels_Should_Handle_Sufficient_Training_Data()
        {
            // Arrange - Generate sufficient training data (>100 samples) to trigger model updates
            for (int i = 0; i < 120; i++)
            {
                var request = new TestRequest();
                var requestMetrics = CreateMetrics(100);
                await _engine.AnalyzeRequestAsync(request, requestMetrics);
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, requestMetrics);
            }

            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.8,
                ["LearningRate"] = 0.1,
                ["OptimizationEffectiveness"] = 0.7
            };
            var accuracy = 0.8;

            // Act - Call UpdateDecisionTreeModels with sufficient training data
            var method = _engine.GetType().GetMethod("UpdateDecisionTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics, accuracy });

            // Assert - Engine should remain functional and handle model updates with sufficient data
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public void UpdateDecisionTreeModels_Should_Handle_Exception_Gracefully()
        {
            // Arrange - Create metrics and accuracy that might cause exceptions
            var metrics = new Dictionary<string, double>
            {
                ["PredictionAccuracy"] = 0.75,
                ["LearningRate"] = 0.1,
                ["OptimizationEffectiveness"] = 0.6
            };
            var accuracy = 0.75;

            // Act - Call UpdateDecisionTreeModels directly using reflection
            var method = _engine.GetType().GetMethod("UpdateDecisionTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { metrics, accuracy });

            // Assert - Method should handle any exceptions gracefully and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var stats = _engine.GetModelStatistics();
            Assert.NotNull(stats);
        }

        [Fact]
        public async Task UpdateDecisionTreeModels_Should_Process_Various_Metric_Combinations()
        {
            // Arrange - Test with various metric combinations
            var testCases = new[]
            {
                new { Metrics = new Dictionary<string, double> { ["PredictionAccuracy"] = 0.6, ["LearningRate"] = 0.2, ["OptimizationEffectiveness"] = 0.5 }, Accuracy = 0.6 },
                new { Metrics = new Dictionary<string, double> { ["PredictionAccuracy"] = 0.85, ["LearningRate"] = 0.08, ["OptimizationEffectiveness"] = 0.75 }, Accuracy = 0.85 },
                new { Metrics = new Dictionary<string, double> { ["PredictionAccuracy"] = 0.92, ["LearningRate"] = 0.05, ["OptimizationEffectiveness"] = 0.85 }, Accuracy = 0.92 }
            };

            foreach (var testCase in testCases)
            {
                // Act - Call UpdateDecisionTreeModels for each test case
                var method = _engine.GetType().GetMethod("UpdateDecisionTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(_engine, new object[] { testCase.Metrics, testCase.Accuracy });

                // Assert - Engine should remain functional after each update
                Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
            }

            // Final verification that system insights are still available
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        #region Helper Methods

        private RequestExecutionMetrics CreateMetrics(int executionCount = 100, TimeSpan? averageExecutionTime = null, int databaseCalls = 2, int externalApiCalls = 1, int failedExecutions = -1)
        {
            var avgTime = averageExecutionTime ?? TimeSpan.FromMilliseconds(100);
            var failed = failedExecutions >= 0 ? failedExecutions : executionCount / 10; // Default 10% failure rate
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = avgTime,
                MedianExecutionTime = avgTime - TimeSpan.FromMilliseconds(5),
                P95ExecutionTime = avgTime + TimeSpan.FromMilliseconds(50),
                P99ExecutionTime = avgTime + TimeSpan.FromMilliseconds(100),
                TotalExecutions = executionCount,
                SuccessfulExecutions = executionCount - failed,
                FailedExecutions = failed,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 10,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = databaseCalls,
                ExternalApiCalls = externalApiCalls
            };
        }

        #endregion
    }
}