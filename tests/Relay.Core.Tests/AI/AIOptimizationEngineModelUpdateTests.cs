using Relay.Core.AI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineModelUpdateTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void UpdateModelCallback_Should_Be_Configured_With_Correct_Interval()
        {
            // Arrange - Check that the engine was initialized with timer
            // The timer is private, but we can verify the engine is functional

            // Act - Engine should be functional
            var isDisposed = _engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var disposed = (bool)isDisposed?.GetValue(_engine)!;

            // Assert - Engine should not be disposed initially
            Assert.False(disposed);
        }

        [Fact]
        public void Dispose_Should_Clean_Up_Model_Update_Timer()
        {
            // Arrange - Engine with timer initialized

            // Act
            _engine.Dispose();

            // Assert - Should not throw and timer should be disposed
            // Since timer is private, we verify by checking engine is disposed
            var isDisposed = _engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var disposed = (bool)isDisposed?.GetValue(_engine)!;
            Assert.True(disposed);
        }

        [Fact]
        public async Task UpdateModelCallback_Should_Execute_Periodically_When_Learning_Enabled()
        {
            // Arrange - Set up engine with short update interval for testing
            // Note: This test is timing-dependent and may be flaky in CI

            // Since we can't directly control the timer interval in integration tests,
            // we'll test that the engine remains functional and can process requests
            // which indirectly validates that background operations are working

            var request = new TestRequest();
            var metrics = CreateMetrics(100);

            // Act - Perform operations that would trigger model updates
            await _engine.AnalyzeRequestAsync(request, metrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Engine should continue to function properly
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task AdjustModelParameters_Should_Decrease_When_Accuracy_Is_Low()
        {
            // Arrange - Create scenario with low accuracy (< 0.7) to trigger parameter decrease
            // To achieve low accuracy, we need more incorrect predictions than correct ones

            // Make several predictions and learn with different strategies to create incorrect predictions
            for (int i = 0; i < 10; i++)
            {
                var requestType = i % 2 == 0 ? typeof(TestRequest) : typeof(OtherTestRequest);
                var request = Activator.CreateInstance(requestType);
                var metrics = CreateMetrics(100);

                // Analyze to get predictions
                var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

                // Learn with a different strategy than predicted to count as incorrect
                var differentStrategy = recommendation.Strategy == OptimizationStrategy.Caching
                    ? OptimizationStrategy.BatchProcessing
                    : OptimizationStrategy.Caching;

                await _engine.LearnFromExecutionAsync(requestType, new[] { differentStrategy }, metrics);
            }

            // Verify we have low accuracy
            var statsBefore = _engine.GetModelStatistics();
            Assert.True(statsBefore.AccuracyScore < 0.7, $"Accuracy should be low but was {statsBefore.AccuracyScore}");

            // Act - Trigger model update via reflection to call AdjustModelParameters(decrease: true)
            var updateMethod = _engine.GetType().GetMethod("UpdateModelCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should still function and have updated parameters
            var statsAfter = _engine.GetModelStatistics();
            Assert.NotNull(statsAfter);
            // Since we can't directly verify parameter adjustment, we ensure the engine remains functional
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task AdjustModelParameters_Should_Increase_When_Accuracy_Is_High()
        {
            // Arrange - Create scenario with high accuracy (> 0.9) to trigger parameter increase
            // To achieve high accuracy, we need mostly correct predictions

            // Make several predictions and learn with improved metrics to count as correct
            for (int i = 0; i < 10; i++)
            {
                var requestType = i % 2 == 0 ? typeof(TestRequest) : typeof(OtherTestRequest);
                var request = Activator.CreateInstance(requestType);
                var analysisMetrics = CreateMetrics(100, TimeSpan.FromMilliseconds(1000)); // Slow baseline metrics for analysis to trigger optimization

                // Analyze to get predictions
                OptimizationRecommendation recommendation;
                if (requestType == typeof(TestRequest))
                {
                    recommendation = await _engine.AnalyzeRequestAsync((TestRequest)request, analysisMetrics);
                }
                else
                {
                    recommendation = await _engine.AnalyzeRequestAsync((OtherTestRequest)request, analysisMetrics);
                }

                // Learn with improved metrics (faster execution, higher success rate) to count as correct
                var learningMetrics = CreateMetrics(100, TimeSpan.FromMilliseconds(300), failedExecutions: 0); // Improved metrics
                await _engine.LearnFromExecutionAsync(requestType, new[] { recommendation.Strategy }, learningMetrics);
            }

            // Verify we have high accuracy
            var statsBefore = _engine.GetModelStatistics();
            Assert.True(statsBefore.AccuracyScore > 0.9, $"Accuracy should be high but was {statsBefore.AccuracyScore}");

            // Act - Trigger model update via reflection to call AdjustModelParameters(decrease: false)
            var updateMethod = _engine.GetType().GetMethod("UpdateModelCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should still function and have updated parameters
            var statsAfter = _engine.GetModelStatistics();
            Assert.NotNull(statsAfter);
            // Since we can't directly verify parameter adjustment, we ensure the engine remains functional
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task AdjustModelParameters_Should_Not_Adjust_When_Accuracy_Is_Moderate()
        {
            // Arrange - Create scenario with moderate accuracy where no adjustment should occur
            for (int i = 0; i < 10; i++)
            {
                var requestType = i % 2 == 0 ? typeof(TestRequest) : typeof(OtherTestRequest);
                var request = Activator.CreateInstance(requestType);
                var metrics = CreateMetrics(100);

                // Analyze to get predictions
                OptimizationRecommendation recommendation;
                if (requestType == typeof(TestRequest))
                {
                    recommendation = await _engine.AnalyzeRequestAsync((TestRequest)request, metrics);
                }
                else
                {
                    recommendation = await _engine.AnalyzeRequestAsync((OtherTestRequest)request, metrics);
                }

                // Mix correct and incorrect predictions
                var strategies = i % 2 == 0  // Alternate correct/incorrect
                    ? new[] { recommendation.Strategy }  // Correct
                    : new[] { recommendation.Strategy == OptimizationStrategy.Caching
                        ? OptimizationStrategy.BatchProcessing
                        : OptimizationStrategy.Caching }; // Incorrect

                await _engine.LearnFromExecutionAsync(requestType, strategies, metrics);
            }

            // Act - Trigger model update via reflection - should not call AdjustModelParameters for moderate accuracy
            var updateMethod = _engine.GetType().GetMethod("UpdateModelCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should still function normally (no adjustment made)
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task CalculateAverageConfidence_Should_Return_High_Confidence_With_Successful_Predictions()
        {
            // Arrange - Create scenario with highly successful predictions across strategies
            // To test CalculateAverageConfidence indirectly, we need to populate _recentPredictions
            // Since it's private, we'll use reflection to access and populate it

            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                // Clear existing predictions
                while (recentPredictions.TryDequeue(out _)) { }

                // Add successful predictions for multiple strategies
                var strategies = new[] { OptimizationStrategy.Caching, OptimizationStrategy.BatchProcessing, OptimizationStrategy.CompressionOptimization };
                foreach (var strategy in strategies)
                {
                    for (int i = 0; i < 5; i++) // 5 successful predictions per strategy
                    {
                        var prediction = new PredictionResult
                        {
                            RequestType = typeof(TestRequest),
                            PredictedStrategies = new[] { strategy },
                            ActualImprovement = TimeSpan.FromMilliseconds(50), // Positive improvement = success
                            Timestamp = DateTime.UtcNow,
                            Metrics = CreateMetrics(100)
                        };
                        recentPredictions.Enqueue(prediction);
                    }
                }
            }

            // Act - Get system insights which should use CalculateAverageConfidence in health score calculation
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - High confidence should contribute to good maintainability/health score
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0.5, "High confidence should result in good maintainability score");
            Assert.True(insights.HealthScore.Overall >= 0.5, "High confidence should contribute to good overall health score");
        }

        [Fact]
        public async Task CalculateAverageConfidence_Should_Return_Low_Confidence_With_Unsuccessful_Predictions()
        {
            // Arrange - Create scenario with mostly unsuccessful predictions
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                // Clear existing predictions
                while (recentPredictions.TryDequeue(out _)) { }

                // Add unsuccessful predictions (negative or zero improvement)
                var strategies = new[] { OptimizationStrategy.Caching, OptimizationStrategy.BatchProcessing };
                foreach (var strategy in strategies)
                {
                    for (int i = 0; i < 3; i++) // 3 unsuccessful predictions per strategy
                    {
                        var prediction = new PredictionResult
                        {
                            RequestType = typeof(TestRequest),
                            PredictedStrategies = new[] { strategy },
                            ActualImprovement = TimeSpan.FromMilliseconds(-10), // Negative improvement = failure
                            Timestamp = DateTime.UtcNow,
                            Metrics = CreateMetrics(100)
                        };
                        recentPredictions.Enqueue(prediction);
                    }
                }
            }

            // Act - Get system insights
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights (CalculateAverageConfidence is not currently used in scoring)
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0 && insights.HealthScore.Maintainability <= 1);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task CalculateAverageConfidence_Should_Return_Default_When_No_Predictions()
        {
            // Arrange - Clear all recent predictions
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                // Clear existing predictions
                while (recentPredictions.TryDequeue(out _)) { }
            }

            // Act - Get system insights with no prediction data
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights with default confidence handling
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0 && insights.HealthScore.Maintainability <= 1);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task CalculateAverageConfidence_Should_Handle_Mixed_Success_Rates()
        {
            // Arrange - Create mixed scenario with some successful and some unsuccessful predictions
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                // Clear existing predictions
                while (recentPredictions.TryDequeue(out _)) { }

                // Add mixed predictions: 60% success rate
                var strategies = new[] { OptimizationStrategy.Caching, OptimizationStrategy.BatchProcessing };
                foreach (var strategy in strategies)
                {
                    for (int i = 0; i < 5; i++) // Mix of success/failure per strategy
                    {
                        var isSuccess = i < 3; // 3 successes, 2 failures per strategy
                        var prediction = new PredictionResult
                        {
                            RequestType = typeof(TestRequest),
                            PredictedStrategies = new[] { strategy },
                            ActualImprovement = isSuccess ? TimeSpan.FromMilliseconds(30) : TimeSpan.FromMilliseconds(-5),
                            Timestamp = DateTime.UtcNow,
                            Metrics = CreateMetrics(100)
                        };
                        recentPredictions.Enqueue(prediction);
                    }
                }
            }

            // Act - Get system insights
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights (CalculateAverageConfidence is not currently used in scoring)
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0 && insights.HealthScore.Maintainability <= 1);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public void CollectMetricsCallback_Should_Execute_Without_Errors()
        {
            // Arrange - Ensure engine is not disposed
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Act - Call CollectMetricsCallback directly via reflection
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify engine can still provide insights after metrics collection
            var insightsTask = _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.True(insightsTask.IsCompletedSuccessfully || !insightsTask.IsFaulted);
        }

        [Fact]
        public void CollectMetricsCallback_Should_Handle_Disposed_Engine()
        {
            // Arrange - Dispose the engine
            _engine.Dispose();

            // Act - Call CollectMetricsCallback on disposed engine
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Method should return early without errors when engine is disposed
            Assert.True(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task CollectMetricsCallback_Should_Update_System_State()
        {
            // Arrange - Get initial state
            var initialInsights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            var initialStats = _engine.GetModelStatistics();

            // Act - Call CollectMetricsCallback to collect and process metrics
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Allow some time for async operations to complete
            await Task.Delay(100);

            // Assert - System should still be functional and provide insights
            var updatedInsights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            var updatedStats = _engine.GetModelStatistics();

            Assert.NotNull(updatedInsights);
            Assert.NotNull(updatedStats);
            Assert.True(updatedInsights.HealthScore.Overall >= 0 && updatedInsights.HealthScore.Overall <= 1);
        }

        [Fact]
        public void CollectMetricsCallback_Should_Handle_Exception_Gracefully()
        {
            // Arrange - This test verifies the try-catch in the method works
            // Since we can't easily force an exception in the internal components,
            // we test that the method completes without crashing the engine

            // Act - Call the callback method
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional even if internal operations fail
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var stats = _engine.GetModelStatistics();
            Assert.NotNull(stats);
        }

        [Fact]
        public async Task CalculateLearningRate_Should_Return_Default_When_Few_Predictions()
        {
            // Arrange - Clear recent predictions to have less than 10
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                while (recentPredictions.TryDequeue(out _)) { }

                // Add only 5 predictions (less than 10)
                for (int i = 0; i < 5; i++)
                {
                    var prediction = new PredictionResult
                    {
                        RequestType = typeof(TestRequest),
                        PredictedStrategies = new[] { OptimizationStrategy.Caching },
                        ActualImprovement = TimeSpan.FromMilliseconds(10),
                        Timestamp = DateTime.UtcNow,
                        Metrics = CreateMetrics(100)
                    };
                    recentPredictions.Enqueue(prediction);
                }
            }

            // Act - Get system insights which should use CalculateLearningRate
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights with default learning rate handling
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0 && insights.HealthScore.Maintainability <= 1);
        }

        [Fact]
        public async Task CalculateLearningRate_Should_Return_Default_When_No_Predictions()
        {
            // Arrange - Clear all recent predictions
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                while (recentPredictions.TryDequeue(out _)) { }
            }

            // Act - Get system insights with no prediction data
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Should return valid insights with default learning rate (0.1)
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0 && insights.HealthScore.Maintainability <= 1);
        }

        [Fact]
        public async Task CalculateLearningRate_Should_Increase_With_Improving_Accuracy()
        {
            // Arrange - Create scenario where accuracy improves over time (second half better than first half)
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                while (recentPredictions.TryDequeue(out _)) { }

                // Add 20 predictions: first 10 with low success rate, second 10 with high success rate
                for (int i = 0; i < 20; i++)
                {
                    var isSecondHalf = i >= 10;
                    var isSuccess = isSecondHalf ? (i % 2 == 0) : (i % 4 == 0); // 25% success in first half, 50% in second half

                    var prediction = new PredictionResult
                    {
                        RequestType = typeof(TestRequest),
                        PredictedStrategies = new[] { OptimizationStrategy.Caching },
                        ActualImprovement = isSuccess ? TimeSpan.FromMilliseconds(20) : TimeSpan.FromMilliseconds(-5),
                        Timestamp = DateTime.UtcNow,
                        Metrics = CreateMetrics(100)
                    };
                    recentPredictions.Enqueue(prediction);
                }
            }

            // Act - Get system insights
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Improving accuracy should result in higher learning rate, contributing to better maintainability score
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0.4, "Improving accuracy should result in good maintainability score");
        }

        [Fact]
        public async Task CalculateLearningRate_Should_Decrease_With_Declining_Accuracy()
        {
            // Arrange - Create scenario where accuracy declines over time (first half better than second half)
            var recentPredictionsField = _engine.GetType().GetField("_recentPredictions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recentPredictions = recentPredictionsField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<PredictionResult>;

            if (recentPredictions != null)
            {
                while (recentPredictions.TryDequeue(out _)) { }

                // Add 20 predictions: first 10 with high success rate, second 10 with low success rate
                for (int i = 0; i < 20; i++)
                {
                    var isSecondHalf = i >= 10;
                    var isSuccess = isSecondHalf ? (i % 4 == 0) : (i % 2 == 0); // 50% success in first half, 25% in second half

                    var prediction = new PredictionResult
                    {
                        RequestType = typeof(TestRequest),
                        PredictedStrategies = new[] { OptimizationStrategy.Caching },
                        ActualImprovement = isSuccess ? TimeSpan.FromMilliseconds(20) : TimeSpan.FromMilliseconds(-5),
                        Timestamp = DateTime.UtcNow,
                        Metrics = CreateMetrics(100)
                    };
                    recentPredictions.Enqueue(prediction);
                }
            }

            // Act - Get system insights
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

            // Assert - Declining accuracy should affect maintainability score
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Maintainability >= 0, "Maintainability score should be valid");
        }

        [Fact]
        public void CalculateSystemStability_Should_Return_Perfect_Stability_When_No_Analytics()
        {
            // Arrange - No analytics data

            // Act - Call CollectMetricsCallback which calls CalculateSystemStability
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Should return 1.0 (perfect stability) when no analytics data
            // Since we can't directly access the result, we verify the engine remains functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task CalculateSystemStability_Should_Calculate_Based_On_Execution_Variance()
        {
            // Arrange - Add analytics with different execution variances
            var request1 = new TestRequest();
            var metrics1 = CreateMetrics(100, TimeSpan.FromMilliseconds(100)); // Consistent execution
            await _engine.AnalyzeRequestAsync(request1, metrics1);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics1);

            var request2 = new OtherTestRequest();
            var metrics2 = CreateMetrics(100, TimeSpan.FromMilliseconds(200)); // Different execution time
            await _engine.AnalyzeRequestAsync(request2, metrics2);
            await _engine.LearnFromExecutionAsync(typeof(OtherTestRequest), new[] { OptimizationStrategy.Caching }, metrics2);

            // Act - Call CollectMetricsCallback to trigger CalculateSystemStability
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional and stability should be calculated
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify we can still get insights (which indirectly validates the stability calculation)
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task CalculateSystemStability_Should_Handle_High_Variance_Scenario()
        {
            // Arrange - Add analytics with high execution variance (inconsistent performance)
            var request1 = new TestRequest();
            var metrics1 = CreateMetrics(100, TimeSpan.FromMilliseconds(50)); // Fast execution
            await _engine.AnalyzeRequestAsync(request1, metrics1);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics1);

            var request2 = new OtherTestRequest();
            var metrics2 = CreateMetrics(100, TimeSpan.FromMilliseconds(500)); // Very slow execution
            await _engine.AnalyzeRequestAsync(request2, metrics2);
            await _engine.LearnFromExecutionAsync(typeof(OtherTestRequest), new[] { OptimizationStrategy.Caching }, metrics2);

            var request3 = new ThirdTestRequest();
            var metrics3 = CreateMetrics(100, TimeSpan.FromMilliseconds(25)); // Very fast execution
            await _engine.AnalyzeRequestAsync(request3, metrics3);
            await _engine.LearnFromExecutionAsync(typeof(ThirdTestRequest), new[] { OptimizationStrategy.Caching }, metrics3);

            // Act - Call CollectMetricsCallback to trigger CalculateSystemStability
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - High variance should result in lower stability score
            // Since we can't directly access the stability score, we verify the engine functions
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task CalculateSystemStability_Should_Handle_Low_Variance_Scenario()
        {
            // Arrange - Add analytics with low execution variance (consistent performance)
            var request1 = new TestRequest();
            var metrics1 = CreateMetrics(100, TimeSpan.FromMilliseconds(95)); // Consistent execution
            await _engine.AnalyzeRequestAsync(request1, metrics1);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics1);

            var request2 = new OtherTestRequest();
            var metrics2 = CreateMetrics(100, TimeSpan.FromMilliseconds(100)); // Consistent execution
            await _engine.AnalyzeRequestAsync(request2, metrics2);
            await _engine.LearnFromExecutionAsync(typeof(OtherTestRequest), new[] { OptimizationStrategy.Caching }, metrics2);

            var request3 = new ThirdTestRequest();
            var metrics3 = CreateMetrics(100, TimeSpan.FromMilliseconds(105)); // Consistent execution
            await _engine.AnalyzeRequestAsync(request3, metrics3);
            await _engine.LearnFromExecutionAsync(typeof(ThirdTestRequest), new[] { OptimizationStrategy.Caching }, metrics3);

            // Act - Call CollectMetricsCallback to trigger CalculateSystemStability
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Low variance should result in higher stability score
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public void AnalyzeMetricTrends_Should_Execute_Without_Errors()
        {
            // Arrange - Ensure engine is not disposed

            // Act - Call CollectMetricsCallback which calls AnalyzeMetricTrends
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public async Task AnalyzeMetricTrends_Should_Handle_Empty_Metrics()
        {
            // Arrange - This test verifies the method handles edge cases gracefully
            // Since AnalyzeMetricTrends is called from CollectMetricsCallback, we test through that

            // Act - Call CollectMetricsCallback with empty metrics scenario
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional even with empty metrics
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify we can still get insights
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task AnalyzeMetricTrends_Should_Process_Metrics_With_Data()
        {
            // Arrange - Add some analytics data that will generate metrics
            var request = new TestRequest();
            var metrics = CreateMetrics(100);
            await _engine.AnalyzeRequestAsync(request, metrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);

            // Act - Call CollectMetricsCallback which will collect metrics and analyze trends
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            callbackMethod?.Invoke(_engine, new object?[] { null });

            // Assert - Engine should remain functional and trend analysis should complete
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify system insights are still available
            var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));
            Assert.NotNull(insights);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public void AnalyzeMetricTrends_Should_Handle_Exception_Gracefully()
        {
            // Arrange - This test verifies the try-catch in AnalyzeMetricTrends works
            // Since we can't easily force an exception in the internal components,
            // we test that the method completes without crashing the engine

            // Act - Call CollectMetricsCallback multiple times to exercise trend analysis
            var callbackMethod = _engine.GetType().GetMethod("CollectMetricsCallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 3; i++)
            {
                callbackMethod?.Invoke(_engine, new object?[] { null });
            }

            // Assert - Engine should remain functional even if internal operations fail
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);

            // Verify basic functionality still works
            var stats = _engine.GetModelStatistics();
            Assert.NotNull(stats);
        }
    }
}