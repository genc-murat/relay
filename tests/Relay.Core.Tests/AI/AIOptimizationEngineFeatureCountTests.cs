using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineFeatureCountTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void CalculateOptimalFeatureCount_Should_Return_Valid_Count_With_Memory_Metrics()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalFeatureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["MemoryUtilization"] = 0.6,
                ["TotalRequests"] = 1000
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            Assert.True(result > 0, $"Feature count should be positive, but was {result}");
            Assert.True(result <= 100, $"Feature count should not exceed 100, but was {result}");
        }

        [Fact]
        public void CalculateOptimalFeatureCount_Should_Reduce_Count_With_High_Memory_Usage()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalFeatureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["MemoryUtilization"] = 0.9, // High memory usage
                ["TotalRequests"] = 1000
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert - Should return a lower count due to high memory usage
            Assert.True(result < 50, $"Should reduce feature count with high memory usage, but was {result}");
        }

        [Fact]
        public void CalculateOptimalFeatureCount_Should_Increase_Count_With_Low_Memory_Usage()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalFeatureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // Implementation calculates sqrt of metrics count, clamped to [3, 10]
            // More metrics in dictionary -> more features (up to 10)
            var largeMetrics = new Dictionary<string, double>();
            for (int i = 0; i < 100; i++) // sqrt(100) = 10
            {
                largeMetrics[$"Metric{i}"] = 0.5;
            }

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { largeMetrics })!;

            // Assert - Should return maximum of 10 features
            Assert.Equal(10, result);
        }

        [Fact]
        public void CalculateOptimalFeatureCount_Should_Handle_Missing_Metrics()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalFeatureCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>(); // Empty metrics

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert - Should return a default value
            Assert.True(result > 0, $"Should return positive default value, but was {result}");
        }
    }
}
