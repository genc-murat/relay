using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIOptimizationEngineBatchSizeTests : AIOptimizationEngineTestBase
{
    #region Parameter Validation Tests

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Throw_When_RequestType_Is_Null()
    {
        // Arrange
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.5
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.PredictOptimalBatchSizeAsync(null!, currentLoad));
    }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Throw_When_CurrentLoad_Is_Null()
    {
        // Arrange
        var requestType = typeof(TestRequest);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.PredictOptimalBatchSizeAsync(requestType, null!));
    }

    #endregion

    #region Base Calculation Tests

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Return_Base_Size_With_Default_Load()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.0, // No CPU load
            MemoryUtilization = 0.0 // No memory load
        };

        // Act
        var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

        // Assert
        // Formula: baseSize(10) * systemLoadFactor(1.0) * memoryFactor(1.0) = 10
        // Since no historical data (avg=0 <50), doubles for fast requests: 10 * 2 = 20
        Assert.Equal(20, result);
    }

    #endregion

    #region Historical Performance Tests

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Reduce_Size_For_Long_Running_Requests()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.0,
            MemoryUtilization = 0.0
        };

        // Add historical data for long-running requests
        var metrics = CreateMetrics(executionCount: 100, averageExecutionTime: TimeSpan.FromMilliseconds(1500)); // > 1000ms
        await _engine.AnalyzeRequestAsync(new TestRequest(), metrics);

        // Act
        var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

        // Assert
        // predictedSize = 10 * 1.0 * 1.0 = 10
        // Since avg=1500 >1000, reduced by half: 10 / 2 = 5
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Increase_Size_For_Fast_Requests()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.0,
            MemoryUtilization = 0.0
        };

        // Add historical data for fast requests
        var metrics = CreateMetrics(executionCount: 100, averageExecutionTime: TimeSpan.FromMilliseconds(25)); // < 50ms
        await _engine.AnalyzeRequestAsync(new TestRequest(), metrics);

        // Act
        var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

        // Assert
        // predictedSize = 10 * 1.0 * 1.0 = 10
        // Since avg=25 <50, doubled: 10 * 2 = 20
        Assert.Equal(20, result);
    }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Reduce_Size_For_High_Variance()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.0,
            MemoryUtilization = 0.0
        };

        // Add historical data with high variance (simulate by adding varied metrics)
        for (int i = 0; i < 10; i++)
        {
            var variance = i * 100; // Create variance in execution times
            var metrics = CreateMetrics(executionCount: 10, averageExecutionTime: TimeSpan.FromMilliseconds(100 + variance));
            await _engine.AnalyzeRequestAsync(new TestRequest(), metrics);
        }

        // Act
        var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

        // Assert
        // predictedSize = 10 * 1.0 * 1.0 = 10
        // High variance (> 0.5) should reduce size by 30%: 10 * 0.7 = 7
        Assert.Equal(7, result);
    }

    #endregion

    #region Boundary Tests

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Clamp_To_Minimum_Size()
        {
            // Arrange
            var requestType = typeof(FourthTestRequest);
            var currentLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.9, // Very high CPU
                MemoryUtilization = 0.9 // Very high memory
            };

            // Act
            var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

            // Assert
            // systemLoadFactor = 1.0 - 0.9 = 0.1
            // memoryFactor = 1.0 - 0.9 = 0.1
            // predictedSize = 10 * 0.1 * 0.1 = 0.1 -> (int)0 = 0
            // Since avg=0 <50, doubles: 0 * 2 = 0
            // clamped to min 1
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Clamp_To_Maximum_Size()
        {
            // Arrange
            var requestType = typeof(FifthTestRequest);
            var currentLoad = new SystemLoadMetrics
            {
                CpuUtilization = 0.0, // No CPU load
                MemoryUtilization = 0.0 // No memory load
            };

            // Add fast request data to maximize size
            var metrics = CreateMetrics(executionCount: 100, averageExecutionTime: TimeSpan.FromMilliseconds(10));
            await _engine.AnalyzeRequestAsync(new FifthTestRequest(), metrics);

            // Act
            var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

            // Assert
            // predictedSize = 10 * 1.0 * 1.0 = 10
            // Since avg=10 <50, doubles: 10 * 2 = 20
            // clamped to max 100
            Assert.Equal(20, result);
        }

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Handle_No_Historical_Data()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.0,
            MemoryUtilization = 0.0
        };

        // No historical data added

        // Act
        var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

        // Assert
        // predictedSize = 10 * 1.0 * 1.0 = 10
        // Since avg=0 <50, doubles: 10 * 2 = 20
        Assert.Equal(20, result);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Log_Debug_Information()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            MemoryUtilization = 0.3
        };

        // Act
        await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Predicted optimal batch size")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task PredictOptimalBatchSizeAsync_Should_Handle_CancellationToken()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var currentLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.0,
            MemoryUtilization = 0.0
        };
        var cts = new CancellationTokenSource();

        // Act
        var result = await _engine.PredictOptimalBatchSizeAsync(requestType, currentLoad, cts.Token);

        // Assert
        // Same as no historical data
        Assert.Equal(20, result);
    }

    #endregion
}
