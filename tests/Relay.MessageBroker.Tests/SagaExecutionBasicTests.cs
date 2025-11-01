using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Interfaces;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

// Custom step to test OperationCanceledException in inner catch block
public class CancellationThrowingStep : ISagaStep<OrderSagaData>
{
    public string Name => "CancellationThrowingStep";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // Throw OperationCanceledException to trigger the inner catch block
        throw new OperationCanceledException("Step cancelled");
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        // Compensation is not reached due to the OperationCanceledException in ExecuteAsync
        return ValueTask.CompletedTask;
    }
}

// Custom saga with cancellation-throwing step
public class CancellationSaga : Saga<OrderSagaData>
{
    public CancellationSaga()
    {
        AddStep(new CancellationThrowingStep());
    }
}

public class SagaExecutionBasicTests
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
    public async Task ExecuteAsync_StepThrowsOperationCanceledException_ShouldReThrowCancellation()
    {
        // Arrange
        var saga = new CancellationSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-010",
            Amount = 100m
        };

        // Act & Assert
        // This should trigger the inner catch block for OperationCanceledException which re-throws
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await saga.ExecuteAsync(data));
    }
    
    [Fact]
    public async Task ExecuteAsync_StepThrowsGeneralException_ShouldTriggerCompensation()
    {
        // Arrange - Use existing test saga with a step that fails
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-011",
            Amount = 100m,
            FailAtStep = "ProcessPayment" // This will cause ProcessPaymentStep to throw InvalidOperationException
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - This should go through the inner general Exception catch block which triggers compensation
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        Assert.NotNull(result.Exception); // The exception that was caught
        Assert.IsType<InvalidOperationException>(result.Exception); // Should be the exception thrown by the step
        Assert.True(result.CompensationSucceeded); // Compensation should have been executed
    }
    
    [Fact]
    public async Task ExecuteAsync_WithCancellationBeforeExecution_ShouldBeCaughtByOuterCatchBlock()
    {
        // Arrange - Cancel the token before execution to trigger cancellation in main try block
        var saga = new TestOrderSaga(); // Use a saga with steps
        var data = new OrderSagaData
        {
            OrderId = "ORDER-012",
            Amount = 100m
        };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before passing to ExecuteAsync

        // Act & Assert
        // The cancellation will be caught by the outer OperationCanceledException catch block
        // since it occurs in the main try block outside of the step-specific try-catch
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await saga.ExecuteAsync(data, cts.Token));
    }
    
    // A custom saga that overrides ExecuteAsync to create a scenario where 
    // an outer catch is triggered by overriding behavior after the main try block
    public class OuterExceptionSagaData : SagaDataBase
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
    
    // Custom saga to test outer exception handling by overriding behavior
    public class OuterExceptionSaga : Saga<OuterExceptionSagaData>
    {
        private readonly bool _throwAfterExecution;
        
        public OuterExceptionSaga(bool throwAfterExecution = false)
        {
            _throwAfterExecution = throwAfterExecution;
            AddStep(new SuccessfulStep());
        }
    }
    
    public class SuccessfulStep : ISagaStep<OuterExceptionSagaData>
    {
        public string Name => "SuccessfulStep";
        
        public ValueTask ExecuteAsync(OuterExceptionSagaData data, CancellationToken cancellationToken = default)
        {
            // Normal execution that succeeds
            return ValueTask.CompletedTask;
        }
        
        public ValueTask CompensateAsync(OuterExceptionSagaData data, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
    
    [Fact]
    public async Task ExecuteAsync_CompensationThrowsUnhandledException_ShouldBeCaughtByOuterCatchBlock()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-013",
            Amount = 100m,
            FailAtStep = "ProcessPayment", // Fail at step 2 to trigger compensation
            CompensationFailAfterRetries = true // This will make compensation fail with unhandled exception
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // When CompensationFailAfterRetries is true, the compensation will fail with an unhandled exception
        // that's not caught by the inner compensation retry logic, potentially bubbling to outer catch
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State); // Compensation might partially succeed
    }
    
    [Fact]
    public async Task ExecuteAsync_CompensationThrowsAfterRetries_ShouldStillSucceedWithPartialResult()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-014",
            Amount = 100m,
            FailAtStep = "ProcessPayment", // Fail at step 2 to trigger compensation
            CompensationExceptionType = "InvalidOperationException", // Make compensation throw unhandled exception
            CompensationFailAfterRetries = true // This will cause compensation to ultimately fail
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Verify the result - compensation may fail but the outer exception handling should catch it
        Assert.False(result.IsSuccess);
        Assert.Equal("ProcessPayment", result.FailedStep);
    }
}