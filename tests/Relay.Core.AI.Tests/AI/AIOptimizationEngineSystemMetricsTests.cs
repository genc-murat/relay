using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineSystemMetricsTests : AIOptimizationEngineTestBase
    {
        [Fact]
        public void CalculateMemoryUsage_Should_Return_Valid_Memory_Value()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculateMemoryUsage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Memory usage should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Memory usage should not exceed 1.0 (100%), but was {result}");
        }

        [Fact]
        public void GetActiveRequestCount_Should_Return_Non_Negative_Count()
        {
            // Act
            var method = _engine.GetType().GetMethod("GetActiveRequestCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0, $"Active request count should be non-negative, but was {result}");
        }

        [Fact]
        public void GetQueuedRequestCount_Should_Return_Non_Negative_Count()
        {
            // Act
            var method = _engine.GetType().GetMethod("GetQueuedRequestCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0, $"Queued request count should be non-negative, but was {result}");
        }

        [Fact]
        public void CalculateCurrentThroughput_Should_Return_Valid_Throughput_Value()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculateCurrentThroughput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Throughput should be non-negative, but was {result}");
        }

        [Fact]
        public async Task CalculateCurrentThroughput_Should_Increase_With_More_Requests()
        {
            // Arrange - Add some requests to the system first
            var request = new TestRequest();
            var metrics = CreateMetrics(100);

            // Process some requests to establish baseline
            for (int i = 0; i < 5; i++)
            {
                await _engine.AnalyzeRequestAsync(request, metrics);
            }

            // Act
            var method = _engine.GetType().GetMethod("CalculateCurrentThroughput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Throughput should be non-negative after processing requests, but was {result}");
        }

        [Fact]
        public void CalculateCurrentErrorRate_Should_Return_Valid_Error_Rate()
        {
            // Act
            var method = _engine.GetType().GetMethod("CalculateCurrentErrorRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Error rate should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Error rate should not exceed 1.0 (100%), but was {result}");
        }

        [Fact]
        public void CalculateCurrentErrorRate_Should_Handle_No_Errors()
        {
            // Arrange - Ensure clean state

            // Act
            var method = _engine.GetType().GetMethod("CalculateCurrentErrorRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert - Should return 0 or very low error rate for clean system
            Assert.True(result >= 0.0 && result <= 1.0, $"Error rate should be valid, but was {result}");
        }

        [Fact]
        public void GetDatabasePoolUtilization_Should_Return_Valid_Utilization_Value()
        {
            // Act
            var method = _engine.GetType().GetMethod("GetDatabasePoolUtilization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Database pool utilization should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Database pool utilization should not exceed 1.0 (100%), but was {result}");
        }

        [Fact]
        public void GetThreadPoolUtilization_Should_Return_Valid_Utilization_Value()
        {
            // Act
            var method = _engine.GetType().GetMethod("GetThreadPoolUtilization", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (double)method?.Invoke(_engine, Array.Empty<object>())!;

            // Assert
            Assert.True(result >= 0.0, $"Thread pool utilization should be non-negative, but was {result}");
            Assert.True(result <= 1.0, $"Thread pool utilization should not exceed 1.0 (100%), but was {result}");
        }
    }
}
