using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Interfaces;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaExecutionEdgeCaseTests
{
    [Fact]
    public async Task ExecuteAsync_EmptySaga_ShouldCompleteImmediately()
    {
        // Arrange
        var saga = new EmptySaga();
        var data = new EmptySagaData
        {
            CorrelationId = "EMPTY-001"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(0, result.Data.CurrentStep);
    }

    [Fact]
    public async Task ExecuteAsync_SingleStepSaga_Success()
    {
        // Arrange
        var saga = new SingleStepSaga();
        var data = new SingleStepSagaData
        {
            CorrelationId = "SINGLE-001",
            Value = 42
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(1, result.Data.CurrentStep);
        Assert.True(result.Data.StepExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_SingleStepSaga_Failure()
    {
        // Arrange
        var saga = new SingleStepSaga();
        var data = new SingleStepSagaData
        {
            CorrelationId = "SINGLE-002",
            Value = 42,
            ShouldFail = true
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("SingleStep", result.FailedStep);
        Assert.False(result.CompensationSucceeded); // No steps were successfully executed, so nothing to compensate
        Assert.False(result.Data.StepCompensated); // Step never executed, so no compensation
    }

    [Fact]
    public async Task ExecuteAsync_WithLargeNumberOfSteps_ShouldHandleCorrectly()
    {
        // Arrange
        var saga = new LargeSaga();
        var data = new LargeSagaData
        {
            CorrelationId = "LARGE-001"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(10, result.Data.CurrentStep); // 10 steps completed
        Assert.Equal(10, result.Data.ExecutedSteps.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithStepSkipping_ShouldSkipSpecifiedSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-018",
            Amount = 100m,
            SkipSteps = new[] { "ProcessPayment" } // Skip payment step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.False(result.Data.ProcessPaymentExecuted); // Skipped
        Assert.True(result.Data.ShipOrderExecuted);
    }
}