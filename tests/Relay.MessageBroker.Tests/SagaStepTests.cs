using FluentAssertions;
using Relay.MessageBroker.Saga;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaStepTests
{
    [Fact]
    public void Name_DefaultImplementation_ShouldReturnTypeName()
    {
        // Arrange
        var step = new TestSagaStep();

        // Act
        var name = step.Name;

        // Assert
        name.Should().Be("TestSagaStep");
    }

    [Fact]
    public void Name_CustomOverride_ShouldReturnCustomName()
    {
        // Arrange
        var step = new CustomNameSagaStep();

        // Act
        var name = step.Name;

        // Assert
        name.Should().Be("CustomStepName");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBeCallable()
    {
        // Arrange
        var step = new TestSagaStep();
        var data = new TestSagaData();

        // Act
        await step.ExecuteAsync(data);

        // Assert
        step.ExecuteCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CompensateAsync_ShouldBeCallable()
    {
        // Arrange
        var step = new TestSagaStep();
        var data = new TestSagaData();

        // Act
        await step.CompensateAsync(data);

        // Assert
        step.CompensateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var step = new CancellationAwareSagaStep();
        var data = new TestSagaData();
        var cts = new CancellationTokenSource();

        // Act
        await step.ExecuteAsync(data, cts.Token);

        // Assert
        step.ReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task CompensateAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var step = new CancellationAwareSagaStep();
        var data = new TestSagaData();
        var cts = new CancellationTokenSource();

        // Act
        await step.CompensateAsync(data, cts.Token);

        // Assert
        step.CompensateReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ShouldPropagate()
    {
        // Arrange
        var step = new FailingSagaStep();
        var data = new TestSagaData();

        // Act
        Func<Task> act = async () => await step.ExecuteAsync(data);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Execute failed");
    }

    [Fact]
    public async Task CompensateAsync_WithException_ShouldPropagate()
    {
        // Arrange
        var step = new FailingCompensationSagaStep();
        var data = new TestSagaData();

        // Act
        Func<Task> act = async () => await step.CompensateAsync(data);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Compensation failed");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldModifyData()
    {
        // Arrange
        var step = new DataModifyingSagaStep();
        var data = new TestSagaData { Value = 10 };

        // Act
        await step.ExecuteAsync(data);

        // Assert
        data.Value.Should().Be(20);
    }

    [Fact]
    public async Task CompensateAsync_ShouldRevertData()
    {
        // Arrange
        var step = new DataModifyingSagaStep();
        var data = new TestSagaData { Value = 20 };

        // Act
        await step.CompensateAsync(data);

        // Assert
        data.Value.Should().Be(10);
    }
}

// Test helper classes
public class TestSagaData : SagaDataBase
{
    public int Value { get; set; }
}

public class TestSagaStep : SagaStep<TestSagaData>
{
    public bool ExecuteCalled { get; private set; }
    public bool CompensateCalled { get; private set; }

    public override ValueTask ExecuteAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        ExecuteCalled = true;
        return ValueTask.CompletedTask;
    }

    public override ValueTask CompensateAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        CompensateCalled = true;
        return ValueTask.CompletedTask;
    }
}

public class CustomNameSagaStep : SagaStep<TestSagaData>
{
    public override string Name => "CustomStepName";

    public override ValueTask ExecuteAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask CompensateAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

public class CancellationAwareSagaStep : SagaStep<TestSagaData>
{
    public CancellationToken ReceivedToken { get; private set; }
    public CancellationToken CompensateReceivedToken { get; private set; }

    public override ValueTask ExecuteAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return ValueTask.CompletedTask;
    }

    public override ValueTask CompensateAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        CompensateReceivedToken = cancellationToken;
        return ValueTask.CompletedTask;
    }
}

public class FailingSagaStep : SagaStep<TestSagaData>
{
    public override ValueTask ExecuteAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Execute failed");
    }

    public override ValueTask CompensateAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

public class FailingCompensationSagaStep : SagaStep<TestSagaData>
{
    public override ValueTask ExecuteAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public override ValueTask CompensateAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Compensation failed");
    }
}

public class DataModifyingSagaStep : SagaStep<TestSagaData>
{
    public override ValueTask ExecuteAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        data.Value = 20;
        return ValueTask.CompletedTask;
    }

    public override ValueTask CompensateAsync(TestSagaData data, CancellationToken cancellationToken = default)
    {
        data.Value = 10;
        return ValueTask.CompletedTask;
    }
}
