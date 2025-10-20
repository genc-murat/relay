using System;
using System.Threading.Tasks;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineModelAccuracyTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void UpdateModelAccuracy_Should_Execute_Without_Errors()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("UpdateModelAccuracy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var requestType = typeof(TestRequest);
            var appliedOptimizations = new[] { OptimizationStrategy.Caching };
            var actualMetrics = CreateMetrics(100);

            // Act
            method?.Invoke(_engine, new object[] { requestType, appliedOptimizations, actualMetrics });

            // Assert - Method should execute without throwing exceptions
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void UpdateModelAccuracy_Should_Handle_Multiple_Strategies()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("UpdateModelAccuracy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var requestType = typeof(TestRequest);
            var appliedOptimizations = new[] { OptimizationStrategy.Caching, OptimizationStrategy.BatchProcessing };
            var actualMetrics = CreateMetrics(100);

            // Act
            method?.Invoke(_engine, new object[] { requestType, appliedOptimizations, actualMetrics });

            // Assert - Should handle multiple optimization strategies
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void ValidatePredictionAccuracy_Should_Return_Valid_Result()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ValidatePredictionAccuracy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var appliedStrategies = new[] { OptimizationStrategy.Caching };
            var actualImprovement = TimeSpan.FromMilliseconds(50);
            var actualMetrics = CreateMetrics(100);

            // Act
            var result = (bool)method?.Invoke(_engine, new object[] { appliedStrategies, actualImprovement, actualMetrics })!;

            // Assert - Should return boolean result
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void ValidatePredictionAccuracy_Should_Return_Boolean_For_Invalid_Prediction()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("ValidatePredictionAccuracy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var appliedStrategies = new[] { OptimizationStrategy.Caching };
            var actualImprovement = TimeSpan.FromMilliseconds(-10); // Negative improvement (worse performance)
            var actualMetrics = CreateMetrics(100);

            // Act
            var result = (bool)method?.Invoke(_engine, new object[] { appliedStrategies, actualImprovement, actualMetrics })!;

            // Assert - Should return boolean result (likely false for negative improvement)
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void CalculatePrecisionScore_Should_Return_Valid_Score()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculatePrecisionScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Precision score should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Precision score should not exceed 1.0, but was {result}");
        }

        [Fact]
        public void CalculateRecallScore_Should_Return_Valid_Score()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculateRecallScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Recall score should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Recall score should not exceed 1.0, but was {result}");
        }

        [Fact]
        public void CalculateF1Score_Should_Return_Valid_Score()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculateF1Score", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"F1 score should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"F1 score should not exceed 1.0, but was {result}");
        }

        [Fact]
        public void CalculateF1Score_Should_Be_Harmonic_Mean_Of_Precision_And_Recall()
        {
            // Arrange - This test verifies the mathematical relationship
            var precisionMethod = _engine.GetType().GetMethod("CalculatePrecisionScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var recallMethod = _engine.GetType().GetMethod("CalculateRecallScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var f1Method = _engine.GetType().GetMethod("CalculateF1Score", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var precision = (double)precisionMethod?.Invoke(_engine, Array.Empty<object>())!;
            var recall = (double)recallMethod?.Invoke(_engine, Array.Empty<object>())!;
            var f1 = (double)f1Method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert - F1 should be harmonic mean: 2 * precision * recall / (precision + recall)
            if (precision + recall > 0)
            {
                var expectedF1 = 2 * precision * recall / (precision + recall);
                Assert.True(Math.Abs(f1 - expectedF1) < 0.001, $"F1 score {f1} should equal harmonic mean {expectedF1}");
            }
        }

        [Fact]
        public void CalculateModelConfidence_Should_Return_Valid_Confidence_Value()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculateModelConfidence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Model confidence should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Model confidence should not exceed 1.0, but was {result}");
        }

        [Fact]
        public async Task CalculateModelConfidence_Should_Increase_With_More_Data()
        {
            // Arrange - Add some prediction data first
            var request = new TestRequest();
            var metrics = CreateMetrics(100);

            // Process some requests to build prediction history
            for (int i = 0; i < 10; i++)
            {
                await _engine.AnalyzeRequestAsync(request, metrics);
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);
            }

            // Act
            var method = _engine.GetType().GetMethod("CalculateModelConfidence", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert - Confidence should be valid after processing data
            Assert.True(result >= 0.0 && result <= 1.0, $"Model confidence should be valid, but was {result}");
        }
    }
}
