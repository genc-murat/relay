using Relay.MessageBroker.Saga;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaTests
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
}

// Test implementation classes
public class OrderSagaData : SagaDataBase
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // Test tracking properties
    public string? FailAtStep { get; set; }
    public bool ReserveInventoryExecuted { get; set; }
    public bool ReserveInventoryCompensated { get; set; }
    public bool ProcessPaymentExecuted { get; set; }
    public bool ProcessPaymentCompensated { get; set; }
    public bool ShipOrderExecuted { get; set; }
    public bool ShipOrderCompensated { get; set; }
    public List<string> ExecutionOrder { get; set; } = new();
    public List<string> CompensationOrder { get; set; } = new();
}

public class TestOrderSaga : Saga<OrderSagaData>
{
    public TestOrderSaga()
    {
        AddStep(new ReserveInventoryStep());
        AddStep(new ProcessPaymentStep());
        AddStep(new ShipOrderStep());
    }
}

public class ReserveInventoryStep : ISagaStep<OrderSagaData>
{
    public string Name => "ReserveInventory";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        data.ReserveInventoryExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        data.ReserveInventoryCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}

public class ProcessPaymentStep : ISagaStep<OrderSagaData>
{
    public string Name => "ProcessPayment";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        data.ProcessPaymentExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        data.ProcessPaymentCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}

public class ShipOrderStep : ISagaStep<OrderSagaData>
{
    public string Name => "ShipOrder";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        data.ShipOrderExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        data.ShipOrderCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}
