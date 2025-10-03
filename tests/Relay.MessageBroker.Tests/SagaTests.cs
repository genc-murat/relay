using FluentAssertions;
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
        result.IsSuccess.Should().BeTrue();
        result.Data.State.Should().Be(SagaState.Completed);
        result.Data.CurrentStep.Should().Be(3); // All 3 steps completed
        result.Data.ReserveInventoryExecuted.Should().BeTrue();
        result.Data.ProcessPaymentExecuted.Should().BeTrue();
        result.Data.ShipOrderExecuted.Should().BeTrue();
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
        result.IsSuccess.Should().BeFalse();
        result.Data.State.Should().Be(SagaState.Compensated);
        result.FailedStep.Should().Be("ProcessPayment");
        result.CompensationSucceeded.Should().BeTrue();
        
        // First step executed and compensated
        result.Data.ReserveInventoryExecuted.Should().BeTrue();
        result.Data.ReserveInventoryCompensated.Should().BeTrue();
        
        // Second step failed, no compensation needed
        result.Data.ProcessPaymentExecuted.Should().BeFalse();
        result.Data.ProcessPaymentCompensated.Should().BeFalse();
        
        // Third step never executed
        result.Data.ShipOrderExecuted.Should().BeFalse();
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
        result.IsSuccess.Should().BeFalse();
        result.Data.State.Should().Be(SagaState.Compensated);
        result.FailedStep.Should().Be("ReserveInventory");
        
        // No steps succeeded, nothing to compensate
        result.Data.ReserveInventoryExecuted.Should().BeFalse();
        result.Data.ReserveInventoryCompensated.Should().BeFalse();
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
        result.IsSuccess.Should().BeFalse();
        result.Data.State.Should().Be(SagaState.Compensated);
        result.FailedStep.Should().Be("ShipOrder");
        result.CompensationSucceeded.Should().BeTrue();
        
        // All previous steps compensated
        result.Data.ReserveInventoryCompensated.Should().BeTrue();
        result.Data.ProcessPaymentCompensated.Should().BeTrue();
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
        result.Data.CreatedAt.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1));
        result.Data.UpdatedAt.Should().BeOnOrAfter(result.Data.CreatedAt);
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
        result.Data.ExecutionOrder.Should().Equal(
            "ReserveInventory",
            "ProcessPayment",
            "ShipOrder"
        );
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
        result.Data.CompensationOrder.Should().Equal(
            "ProcessPayment-Compensation",
            "ReserveInventory-Compensation"
        );
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

        // Act
        Func<Task> act = async () => await saga.ExecuteAsync(data, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void SagaId_ShouldReturnTypeName()
    {
        // Arrange
        var saga = new TestOrderSaga();

        // Act
        var sagaId = saga.SagaId;

        // Assert
        sagaId.Should().Be("TestOrderSaga");
    }

    [Fact]
    public void Steps_ShouldReturnAllAddedSteps()
    {
        // Arrange
        var saga = new TestOrderSaga();

        // Act
        var steps = saga.Steps;

        // Assert
        steps.Should().HaveCount(3);
        steps[0].Name.Should().Be("ReserveInventory");
        steps[1].Name.Should().Be("ProcessPayment");
        steps[2].Name.Should().Be("ShipOrder");
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
        result.Data.CurrentStep.Should().Be(1);
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
        result.IsSuccess.Should().BeTrue();
        result.Data.ReserveInventoryExecuted.Should().BeFalse(); // First step skipped
        result.Data.ProcessPaymentExecuted.Should().BeTrue(); // Second step executed
        result.Data.ShipOrderExecuted.Should().BeTrue(); // Third step executed
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
