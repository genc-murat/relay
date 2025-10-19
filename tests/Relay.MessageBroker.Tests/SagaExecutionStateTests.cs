using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaExecutionStateTests
{
    [Fact]
    public async Task ExecuteAsync_MultipleTimes_ShouldResumeFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-010",
            Amount = 100m,
            CurrentStep = 1 // Resume from second step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Data.ReserveInventoryExecuted); // First step skipped
        Assert.True(result.Data.ProcessPaymentExecuted); // Second step executed
        Assert.True(result.Data.ShipOrderExecuted); // Third step executed
    }

    [Fact]
    public async Task ExecuteAsync_WithAbortedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-011",
            Amount = 100m,
            State = SagaState.Aborted,
            CurrentStep = 0 // Start from beginning despite aborted state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-012",
            Amount = 100m,
            State = SagaState.Completed,
            CurrentStep = 0 // Start from beginning despite completed state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-013",
            Amount = 100m,
            State = SagaState.Failed,
            CurrentStep = 0 // Start from beginning despite failed state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompensatedState_ShouldExecuteFromCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-014",
            Amount = 100m,
            State = SagaState.Compensated,
            CurrentStep = 0 // Start from beginning despite compensated state
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Saga executes normally from CurrentStep, ignoring initial state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecution_ShouldHandleProperly()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-017",
            Amount = 100m,
            State = SagaState.Running // Already running
        };

        // Act - Try to execute while already running
        var result = await saga.ExecuteAsync(data);

        // Assert - Should continue from current state
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
    }

    [Fact]
    public async Task ExecuteAsync_StateTransitions_FromRunningToCompleted()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-019",
            Amount = 100m,
            State = SagaState.Running
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_StateTransitions_FromRunningToCompensated()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-020",
            Amount = 100m,
            State = SagaState.Running,
            FailAtStep = "ProcessPayment"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_ResumeFromMiddleStep_Succeeds()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-021",
            Amount = 100m,
            State = SagaState.Compensating, // Initial state doesn't matter
            CurrentStep = 1 // Resume from ProcessPayment step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Executes from CurrentStep, ignoring initial state
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.IsSuccess);
        Assert.False(result.Data.ReserveInventoryExecuted); // Skipped
        Assert.True(result.Data.ProcessPaymentExecuted); // Executed
        Assert.True(result.Data.ShipOrderExecuted); // Executed
    }
}