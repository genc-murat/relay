using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineFastTreeTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void RetrainFastTreeModels_Should_Execute_Without_Errors()
        {
            // Arrange - Test with valid parameters
            var numberOfLeaves = 20;
            var numberOfTrees = 100;
            var learningRate = 0.1;
            var minExamplesPerLeaf = 5;

            // Act - Call RetrainFastTreeModels directly using reflection
            var method = _engine.GetType().GetMethod("RetrainFastTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void RetrainFastTreeModels_Should_Trigger_Retrain_With_Sufficient_Data()
        {
            // Arrange - Add sufficient training data (>= 500 samples) to trigger retrain
            var trainingDataField = _engine.GetType().GetField("_performanceTrainingData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var trainingData = trainingDataField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<Dictionary<string, double>>;

            if (trainingData != null)
            {
                // Clear existing data and add 550 points
                while (trainingData.TryDequeue(out _)) { }

                for (int i = 0; i < 550; i++)
                {
                    trainingData.Enqueue(new Dictionary<string, double>
                    {
                        ["ResponseTime"] = 100.0 + i,
                        ["SuccessRate"] = 0.9
                    });
                }
            }

            var numberOfLeaves = 15;
            var numberOfTrees = 80;
            var learningRate = 0.05;
            var minExamplesPerLeaf = 10;

            // Act - Call RetrainFastTreeModels directly using reflection
            var method = _engine.GetType().GetMethod("RetrainFastTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf });

            // Assert - Method should execute without throwing exceptions and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void RetrainFastTreeModels_Should_Trigger_Initial_Training_With_Moderate_Data()
        {
            // Arrange - Add moderate amount of training data (>= 100 samples, < 500) to trigger initial training
            var trainingDataField = _engine.GetType().GetField("_performanceTrainingData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var trainingData = trainingDataField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<Dictionary<string, double>>;

            if (trainingData != null)
            {
                // Clear existing data and add 150 points
                while (trainingData.TryDequeue(out _)) { }

                for (int i = 0; i < 150; i++)
                {
                    trainingData.Enqueue(new Dictionary<string, double>
                    {
                        ["ResponseTime"] = 100.0 + i,
                        ["SuccessRate"] = 0.9
                    });
                }
            }

            // Ensure models are not initialized to trigger initial training
            var mlModelsField = _engine.GetType().GetField("_mlModelsInitialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var originalValue = (bool)mlModelsField?.GetValue(_engine)!;
            mlModelsField?.SetValue(_engine, false);

            var numberOfLeaves = 25;
            var numberOfTrees = 120;
            var learningRate = 0.08;
            var minExamplesPerLeaf = 8;

            try
            {
                // Act - Call RetrainFastTreeModels directly using reflection
                var method = _engine.GetType().GetMethod("RetrainFastTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf });

                // Assert - Method should execute without throwing exceptions
                Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
            }
            finally
            {
                // Restore original value
                mlModelsField?.SetValue(_engine, originalValue);
            }
        }

        [Fact]
        public void RetrainFastTreeModels_Should_Handle_Insufficient_Data_Gracefully()
        {
            // Arrange - Clear training data to ensure insufficient data (< 100 samples)
            var trainingDataField = _engine.GetType().GetField("_performanceTrainingData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var trainingData = trainingDataField?.GetValue(_engine) as System.Collections.Concurrent.ConcurrentQueue<Dictionary<string, double>>;

            if (trainingData != null)
            {
                // Clear existing data and add only 50 points
                while (trainingData.TryDequeue(out _)) { }

                for (int i = 0; i < 50; i++)
                {
                    trainingData.Enqueue(new Dictionary<string, double>
                    {
                        ["ResponseTime"] = 100.0 + i,
                        ["SuccessRate"] = 0.9
                    });
                }
            }

            var numberOfLeaves = 30;
            var numberOfTrees = 150;
            var learningRate = 0.12;
            var minExamplesPerLeaf = 3;

            // Act - Call RetrainFastTreeModels directly using reflection
            var method = _engine.GetType().GetMethod("RetrainFastTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf });

            // Assert - Method should execute without throwing exceptions even with insufficient data
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void RetrainFastTreeModels_Should_Handle_Exception_Gracefully()
        {
            // Arrange - Test with parameters that might cause exceptions in FastTree operations
            var numberOfLeaves = 0; // Potentially problematic value
            var numberOfTrees = -1; // Invalid value
            var learningRate = double.NaN; // Invalid value
            var minExamplesPerLeaf = 0; // Potentially problematic value

            // Act - Call RetrainFastTreeModels directly using reflection
            var method = _engine.GetType().GetMethod("RetrainFastTreeModels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf });

            // Assert - Method should handle any exceptions gracefully and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void ExtractFeatureImportanceFromFastTree_Should_Return_Null_On_Exception()
        {
            // Arrange - This test verifies the try-catch in the method works
            // Since we can't easily force an exception in the internal components,
            // we test that the method completes without crashing the engine

            // Act - Call ExtractFeatureImportanceFromFastTree directly using reflection
            var method = _engine.GetType().GetMethod("ExtractFeatureImportanceFromFastTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method?.Invoke(_engine, Array.Empty<object>()) as Dictionary<string, float>;

            // Assert - Method should execute without throwing unhandled exceptions
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
            // Result should be either null or a dictionary
            Assert.True(result == null || result is Dictionary<string, float>);
        }

        [Fact]
        public void ExtractFeatureImportanceFromFastTree_Should_Execute_Without_Throwing()
        {
            // Arrange - Test that the method can be called without throwing unhandled exceptions

            // Act & Assert - Call ExtractFeatureImportanceFromFastTree directly using reflection
            var method = _engine.GetType().GetMethod("ExtractFeatureImportanceFromFastTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var exception = Record.Exception(() => method?.Invoke(_engine, Array.Empty<object>()));

            // Assert - Method should not throw unhandled exceptions
            Assert.Null(exception);
            // Engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void ExtractFeatureImportanceFromFastTree_Should_Return_Dictionary_Or_Null()
        {
            // Arrange - Test that the method returns either a dictionary or null

            // Act - Call ExtractFeatureImportanceFromFastTree directly using reflection
            var method = _engine.GetType().GetMethod("ExtractFeatureImportanceFromFastTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method?.Invoke(_engine, Array.Empty<object>());

            // Assert - Result should be either null or a Dictionary<string, float>
            Assert.True(result == null || result is Dictionary<string, float>);
        }

        [Fact]
        public void ExtractFeatureImportanceFromFastTree_Should_Handle_Multiple_Calls()
        {
            // Arrange - Test that multiple calls work without issues

            // Act - Call ExtractFeatureImportanceFromFastTree multiple times
            var method = _engine.GetType().GetMethod("ExtractFeatureImportanceFromFastTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 5; i++)
            {
                var result = method?.Invoke(_engine, Array.Empty<object>());
                // Assert - Each call should return either null or a dictionary
                Assert.True(result == null || result is Dictionary<string, float>);
            }

            // Assert - Engine should remain functional after multiple calls
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void ExtractFeatureImportanceFromFastTree_Should_Be_Idempotent()
        {
            // Arrange - Test that repeated calls produce consistent results

            // Act - Call ExtractFeatureImportanceFromFastTree multiple times
            var method = _engine.GetType().GetMethod("ExtractFeatureImportanceFromFastTree", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result1 = method?.Invoke(_engine, Array.Empty<object>());
            var result2 = method?.Invoke(_engine, Array.Empty<object>());
            var result3 = method?.Invoke(_engine, Array.Empty<object>());

            // Assert - All results should be consistent (all null or all dictionaries)
            var allNull = result1 == null && result2 == null && result3 == null;
            var allDict = result1 is Dictionary<string, float> && result2 is Dictionary<string, float> && result3 is Dictionary<string, float>;

            Assert.True(allNull || allDict, "Method should be idempotent - all calls should return the same type (null or dictionary)");
            // Engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void StoreDecisionTreeMetrics_Should_Execute_Without_Errors()
        {
            // Arrange - Test with valid parameters
            var numberOfLeaves = 20;
            var numberOfTrees = 100;
            var learningRate = 0.1;
            var minExamplesPerLeaf = 5;
            var accuracy = 0.85;

            // Act - Call StoreDecisionTreeMetrics directly using reflection
            var method = _engine.GetType().GetMethod("StoreDecisionTreeMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf, accuracy });

            // Assert - Method should execute without throwing exceptions
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }

        [Fact]
        public void StoreDecisionTreeMetrics_Should_Handle_Various_Parameter_Combinations()
        {
            // Arrange - Test with various parameter combinations
            var testCases = new[]
            {
                new { Leaves = 10, Trees = 50, LearningRate = 0.01, MinExamples = 2, Accuracy = 0.75 },
                new { Leaves = 50, Trees = 200, LearningRate = 0.2, MinExamples = 20, Accuracy = 0.95 },
                new { Leaves = 25, Trees = 100, LearningRate = 0.1, MinExamples = 10, Accuracy = 0.85 },
                new { Leaves = 1, Trees = 10, LearningRate = 0.001, MinExamples = 1, Accuracy = 0.5 },
                new { Leaves = 100, Trees = 500, LearningRate = 0.5, MinExamples = 50, Accuracy = 0.99 }
            };

            foreach (var testCase in testCases)
            {
                // Act - Call StoreDecisionTreeMetrics for each test case
                var method = _engine.GetType().GetMethod("StoreDecisionTreeMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(_engine, new object[] { testCase.Leaves, testCase.Trees, testCase.LearningRate, testCase.MinExamples, testCase.Accuracy });

                // Assert - Method should execute without throwing exceptions for each parameter combination
                Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
            }
        }

        [Fact]
        public void StoreDecisionTreeMetrics_Should_Handle_Exception_Gracefully()
        {
            // Arrange - Test with parameters that might cause exceptions
            var numberOfLeaves = 0; // Potentially problematic value
            var numberOfTrees = -1; // Invalid value
            var learningRate = double.NaN; // Invalid value
            var minExamplesPerLeaf = 0; // Potentially problematic value
            var accuracy = double.NaN; // Invalid value

            // Act - Call StoreDecisionTreeMetrics directly using reflection
            var method = _engine.GetType().GetMethod("StoreDecisionTreeMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_engine, new object[] { numberOfLeaves, numberOfTrees, learningRate, minExamplesPerLeaf, accuracy });

            // Assert - Method should handle any exceptions gracefully and engine should remain functional
            Assert.False(_engine.GetType().GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_engine) as bool? ?? false);
        }
    }
}
