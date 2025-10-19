using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaExecutionTests
{
    [Fact]
    public async Task ExecuteAsync_AllStepsSucceed_ShouldCompleteSuccessfully()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-001",
            Amount = 100m
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.Equal(3, result.Data.CurrentStep); // All 3 steps completed
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ProcessPaymentExecuted);
        Assert.True(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_ShouldCompensatePreviousSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-002",
            Amount = 100m,
            FailAtStep = "ProcessPayment" // Fail at step 2
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.True(result.CompensationSucceeded);

        // First step executed and compensated
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ReserveInventoryCompensated);

        // Second step failed, no compensation needed
        Assert.False(result.Data.ProcessPaymentExecuted);
        Assert.False(result.Data.ProcessPaymentCompensated);

        // Third step never executed
        Assert.False(result.Data.ShipOrderExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_FirstStepFails_ShouldNotCompensateAnything()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-003",
            Amount = 100m,
            FailAtStep = "ReserveInventory" // Fail at first step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ReserveInventory", result.FailedStep);

        // No steps succeeded, nothing to compensate
        Assert.False(result.Data.ReserveInventoryExecuted);
        Assert.False(result.Data.ReserveInventoryCompensated);
    }

    [Fact]
    public async Task ExecuteAsync_LastStepFails_ShouldCompensateAllPreviousSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-004",
            Amount = 100m,
            FailAtStep = "ShipOrder" // Fail at last step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ShipOrder", result.FailedStep);
        Assert.True(result.CompensationSucceeded);

        // All previous steps compensated
        Assert.True(result.Data.ReserveInventoryCompensated);
        Assert.True(result.Data.ProcessPaymentCompensated);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateTimestamps()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-005",
            Amount = 100m
        };
        var startTime = DateTimeOffset.UtcNow;

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.True((result.Data.CreatedAt - startTime).Duration() <= TimeSpan.FromSeconds(1));
        Assert.True(result.Data.UpdatedAt >= result.Data.CreatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteStepsInOrder()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-006",
            Amount = 100m
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        Assert.Equal(new[] { "ReserveInventory", "ProcessPayment", "ShipOrder" }, result.Data.ExecutionOrder);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompensateInReverseOrder()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-007",
            Amount = 100m,
            FailAtStep = "ShipOrder"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert
        // When ShipOrder fails, only previously executed steps are compensated (in reverse order)
        Assert.Equal(new[] { "ProcessPayment-Compensation", "ReserveInventory-Compensation" }, result.Data.CompensationOrder);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-008",
            Amount = 100m
        };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await saga.ExecuteAsync(data, cts.Token));
    }

    [Fact]
    public void SagaId_ShouldReturnTypeName()
    {
        // Arrange
        var saga = new TestOrderSaga();

        // Act
        var sagaId = saga.SagaId;

        // Assert
        Assert.Equal("TestOrderSaga", sagaId);
    }

    [Fact]
    public void Steps_ShouldReturnAllAddedSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();

        // Act
        var steps = saga.Steps;

        // Assert
        Assert.Equal(3, steps.Count);
        Assert.Equal("ReserveInventory", steps[0].Name);
        Assert.Equal("ProcessPayment", steps[1].Name);
        Assert.Equal("ShipOrder", steps[2].Name);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackCurrentStep()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-009",
            Amount = 100m,
            FailAtStep = "ProcessPayment"
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Failed at step 2, so CurrentStep should be 1 (0-indexed)
        Assert.Equal(1, result.Data.CurrentStep);
    }

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
    public async Task ExecuteAsync_CompensationFails_ShouldStillMarkAsCompensated()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-015",
            Amount = 100m,
            FailAtStep = "ProcessPayment",
            FailCompensationAtStep = "ReserveInventory" // Fail compensation of first step
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Even with compensation failure, state is set to Compensated
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.False(result.CompensationSucceeded); // Compensation failed

        // First step executed but compensation failed
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.False(result.Data.ReserveInventoryCompensated);

        // Second step failed
        Assert.False(result.Data.ProcessPaymentExecuted);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ShouldTriggerCompensation()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-016",
            Amount = 100m,
            TimeoutAtStep = "ProcessPayment" // Simulate timeout during payment
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Timeout triggers compensation, resulting in Compensated state
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.True(result.CompensationSucceeded);

        // First step executed and should be compensated
        Assert.True(result.Data.ReserveInventoryExecuted);
        Assert.True(result.Data.ReserveInventoryCompensated);
    }

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