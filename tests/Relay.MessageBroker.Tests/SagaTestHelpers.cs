using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

// Test implementation classes
public partial class OrderSagaData : SagaDataBase
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
        if (data.SkipSteps?.Contains(Name) == true)
        {
            return ValueTask.CompletedTask; // Skip this step
        }

        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        if (data.TimeoutAtStep == Name)
        {
            throw new TimeoutException($"Timeout at {Name}");
        }

        data.ReserveInventoryExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailCompensationAtStep == Name)
        {
            throw new InvalidOperationException($"Compensation failed at {Name}");
        }

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
        if (data.SkipSteps?.Contains(Name) == true)
        {
            return ValueTask.CompletedTask; // Skip this step
        }

        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        if (data.TimeoutAtStep == Name)
        {
            throw new TimeoutException($"Timeout at {Name}");
        }

        data.ProcessPaymentExecuted = true;
        data.ExecutionOrder.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailCompensationAtStep == Name)
        {
            throw new InvalidOperationException($"Compensation failed at {Name}");
        }

        data.ProcessPaymentCompensated = true;
        data.CompensationOrder.Add($"{Name}-Compensation");
        return ValueTask.CompletedTask;
    }
}

// Enhanced OrderSagaData for additional test scenarios
public partial class OrderSagaData
{
    public string? FailCompensationAtStep { get; set; }
    public string? TimeoutAtStep { get; set; }
    public string[]? SkipSteps { get; set; }
}

// Additional test classes for edge cases

public class ShipOrderStep : ISagaStep<OrderSagaData>
{
    public string Name => "ShipOrder";

    public ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.FailAtStep == Name)
        {
            throw new InvalidOperationException($"Failed at {Name}");
        }

        if (data.TimeoutAtStep == Name)
        {
            throw new TimeoutException($"Timeout at {Name}");
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

// Additional test classes for edge cases

public class EmptySagaData : SagaDataBase
{
    public string CorrelationId { get; set; } = string.Empty;
}

public class EmptySaga : Saga<EmptySagaData>
{
    // No steps added - empty saga
}

public class SingleStepSagaData : SagaDataBase
{
    public string CorrelationId { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool ShouldFail { get; set; }
    public bool StepExecuted { get; set; }
    public bool StepCompensated { get; set; }
}

public class SingleStepSaga : Saga<SingleStepSagaData>
{
    public SingleStepSaga()
    {
        AddStep(new SingleStep());
    }
}

public class SingleStep : ISagaStep<SingleStepSagaData>
{
    public string Name => "SingleStep";

    public ValueTask ExecuteAsync(SingleStepSagaData data, CancellationToken cancellationToken = default)
    {
        if (data.ShouldFail)
        {
            throw new InvalidOperationException("Step failed as requested");
        }

        data.StepExecuted = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(SingleStepSagaData data, CancellationToken cancellationToken = default)
    {
        data.StepCompensated = true;
        return ValueTask.CompletedTask;
    }
}

public class LargeSagaData : SagaDataBase
{
    public string CorrelationId { get; set; } = string.Empty;
    public List<string> ExecutedSteps { get; set; } = new();
}

public class LargeSaga : Saga<LargeSagaData>
{
    public LargeSaga()
    {
        // Add 10 steps
        for (int i = 1; i <= 10; i++)
        {
            AddStep(new NumberedStep(i));
        }
    }
}

public class NumberedStep : ISagaStep<LargeSagaData>
{
    private readonly int _number;

    public NumberedStep(int number)
    {
        _number = number;
    }

    public string Name => $"Step{_number}";

    public ValueTask ExecuteAsync(LargeSagaData data, CancellationToken cancellationToken = default)
    {
        data.ExecutedSteps.Add(Name);
        return ValueTask.CompletedTask;
    }

    public ValueTask CompensateAsync(LargeSagaData data, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

// Enhanced OrderSagaData for additional test scenarios