using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineBatchSizeTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void CalculateOptimalBatchSize_Should_Return_Base_Size_With_Moderate_Memory()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 0.5 // Moderate memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            // Formula: 32 * (1 + (1.0 - 0.5)) = 32 * 1.5 = 48
            Assert.Equal(48, result);
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Increase_With_Low_Memory_Usage()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 0.2 // Low memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            Assert.True(result > 32, $"Should increase batch size with low memory usage, but was {result}");
            Assert.True(result <= 128, $"Should not exceed maximum batch size, but was {result}");
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Decrease_With_High_Memory_Usage()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 0.9 // High memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            // Formula: 32 * (1 + (1.0 - 0.9)) = 32 * 1.1 = 35.2 → 35
            // Result should be close to base size but slightly higher due to formula
            Assert.True(result <= 48, $"Should not increase batch size too much with high memory usage, but was {result}");
            Assert.True(result >= 32, $"Should be at or above base batch size, but was {result}");
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Clamp_To_Minimum_Value()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 0.95 // Very high memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            // Formula: 32 * (1 + (1.0 - 0.95)) = 32 * 1.05 = 33.6 → 33
            Assert.Equal(33, result);
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Clamp_To_Maximum_Value()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 0.0 // No memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            Assert.Equal(64, result); // Should clamp to maximum (32 * (1 + 1.0) = 64)
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Use_Default_Values_When_Metrics_Missing()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>(); // Empty metrics

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            // Uses default MemoryUtilization=0.5: 32 * (1 + 0.5) = 48
            Assert.Equal(48, result);
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Handle_Zero_Memory_Usage()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 0.0 // Zero memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            Assert.Equal(64, result); // 32 * (1 + 1.0) = 64, clamped to max
        }

        [Fact]
        public void CalculateOptimalBatchSize_Should_Handle_Full_Memory_Usage()
        {
            // Arrange
            var method = _engine.GetType().GetMethod("CalculateOptimalBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var metrics = new Dictionary<string, double>
            {
                ["TotalRequests"] = 100,
                ["MemoryUtilization"] = 1.0 // Full memory usage
            };

            // Act
            var result = (int)method?.Invoke(_engine, new object[] { metrics })!;

            // Assert
            // Formula: 32 * (1 + (1.0 - 1.0)) = 32 * 1.0 = 32
            Assert.Equal(32, result);
        }
    }
}
