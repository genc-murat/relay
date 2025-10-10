using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaTimeoutTests
{
    [Fact]
    public async Task SagaTimeoutHandler_CheckRunningSagasForTimeout_ShouldDetectTimeout()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var logger = NullLogger<SagaTimeoutHandler<OrderSagaData>>.Instance;
        var handler = new SagaTimeoutHandler<OrderSagaData>(persistence, logger);

        // Create a running saga that was last updated 10 minutes ago (timed out)
        var timedOutSaga = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "TIMEOUT-001",
            State = SagaState.Running,
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        // Create a running saga that was last updated 1 minute ago (not timed out)
        var activeSaga = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "ACTIVE-001",
            State = SagaState.Running,
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        await persistence.SaveAsync(timedOutSaga);
        await persistence.SaveAsync(activeSaga);

        // Act
        var defaultTimeout = TimeSpan.FromMinutes(5);
        var result = await handler.CheckAndHandleTimeoutsAsync(defaultTimeout);

        // Assert
        result.CheckedCount.Should().Be(2);
        result.TimedOutCount.Should().Be(1);

        // Verify timed out saga was marked for compensation
        var updatedSaga = await persistence.GetByIdAsync(timedOutSaga.SagaId);
        updatedSaga.Should().NotBeNull();
        updatedSaga!.State.Should().Be(SagaState.Compensating);
        updatedSaga.Metadata.Should().ContainKey("TimedOut");
        updatedSaga.Metadata["TimedOut"].Should().Be(true);
    }

    [Fact]
    public async Task SagaTimeoutHandler_CompensatingSagaTimeout_ShouldMarkAsFailed()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var logger = NullLogger<SagaTimeoutHandler<OrderSagaData>>.Instance;
        var handler = new SagaTimeoutHandler<OrderSagaData>(persistence, logger);

        // Create a compensating saga that has timed out
        var timedOutSaga = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "COMP-TIMEOUT-001",
            State = SagaState.Compensating,
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        await persistence.SaveAsync(timedOutSaga);

        // Act
        var result = await handler.CheckAndHandleTimeoutsAsync(TimeSpan.FromMinutes(5));

        // Assert
        result.CheckedCount.Should().Be(1);
        result.TimedOutCount.Should().Be(1);

        // Verify saga was marked as failed
        var updatedSaga = await persistence.GetByIdAsync(timedOutSaga.SagaId);
        updatedSaga.Should().NotBeNull();
        updatedSaga!.State.Should().Be(SagaState.Failed);
        updatedSaga.Metadata.Should().ContainKey("CompensationTimedOut");
    }

    [Fact]
    public async Task SagaTimeoutHandler_CustomTimeout_ShouldUseCustomValue()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var logger = NullLogger<SagaTimeoutHandler<OrderSagaData>>.Instance;
        var handler = new SagaTimeoutHandler<OrderSagaData>(persistence, logger);

        // Create a saga with custom 2-minute timeout
        var sagaWithCustomTimeout = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CUSTOM-TIMEOUT-001",
            State = SagaState.Running,
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-3),  // 3 minutes ago
            Metadata = new Dictionary<string, object>
            {
                ["Timeout"] = 120 // 2 minutes in seconds
            }
        };

        await persistence.SaveAsync(sagaWithCustomTimeout);

        // Act - Use default timeout of 5 minutes, but saga has custom 2-minute timeout
        var result = await handler.CheckAndHandleTimeoutsAsync(TimeSpan.FromMinutes(5));

        // Assert - Should timeout because 3 minutes > 2 minute custom timeout
        result.CheckedCount.Should().Be(1);
        result.TimedOutCount.Should().Be(1);

        var updatedSaga = await persistence.GetByIdAsync(sagaWithCustomTimeout.SagaId);
        updatedSaga!.State.Should().Be(SagaState.Compensating);
    }

    [Fact]
    public async Task SagaTimeoutHandler_NoActiveSagas_ShouldReturnZeroCounts()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var logger = NullLogger<SagaTimeoutHandler<OrderSagaData>>.Instance;
        var handler = new SagaTimeoutHandler<OrderSagaData>(persistence, logger);

        // Create only completed sagas
        await persistence.SaveAsync(new OrderSagaData { State = SagaState.Completed });
        await persistence.SaveAsync(new OrderSagaData { State = SagaState.Failed });

        // Act
        var result = await handler.CheckAndHandleTimeoutsAsync(TimeSpan.FromMinutes(5));

        // Assert
        result.CheckedCount.Should().Be(0);
        result.TimedOutCount.Should().Be(0);
    }

    [Fact]
    public async Task SagaWithTimeoutInterface_ShouldUseInterfaceTimeout()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<SagaDataWithTimeout>();
        var logger = NullLogger<SagaTimeoutHandler<SagaDataWithTimeout>>.Instance;
        var handler = new SagaTimeoutHandler<SagaDataWithTimeout>(persistence, logger);

        // Create saga that implements ISagaDataWithTimeout with 1-minute timeout
        var saga = new SagaDataWithTimeout
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "INTERFACE-TIMEOUT-001",
            State = SagaState.Running,
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) // 2 minutes ago
        };

        await persistence.SaveAsync(saga);

        // Act - Default timeout is 5 minutes, but interface specifies 1 minute
        var result = await handler.CheckAndHandleTimeoutsAsync(TimeSpan.FromMinutes(5));

        // Assert - Should timeout because 2 minutes > 1 minute interface timeout
        result.CheckedCount.Should().Be(1);
        result.TimedOutCount.Should().Be(1);
    }

    [Fact]
    public async Task SagaTimeoutHandler_MultipleTimeouts_ShouldHandleAll()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var logger = NullLogger<SagaTimeoutHandler<OrderSagaData>>.Instance;
        var handler = new SagaTimeoutHandler<OrderSagaData>(persistence, logger);

        // Create multiple timed out sagas
        for (int i = 0; i < 5; i++)
        {
            await persistence.SaveAsync(new OrderSagaData
            {
                SagaId = Guid.NewGuid(),
                CorrelationId = $"TIMEOUT-{i}",
                State = SagaState.Running,
                UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            });
        }

        // Act
        var result = await handler.CheckAndHandleTimeoutsAsync(TimeSpan.FromMinutes(5));

        // Assert
        result.CheckedCount.Should().Be(5);
        result.TimedOutCount.Should().Be(5);
    }

    [Fact]
    public async Task SagaTimeoutHandler_CancellationRequested_ShouldStopProcessing()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var logger = NullLogger<SagaTimeoutHandler<OrderSagaData>>.Instance;
        var handler = new SagaTimeoutHandler<OrderSagaData>(persistence, logger);

        await persistence.SaveAsync(new OrderSagaData
        {
            State = SagaState.Running,
            UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        });

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should not throw, just stop processing
        var action = async () => await handler.CheckAndHandleTimeoutsAsync(
            TimeSpan.FromMinutes(5),
            cts.Token);

        await action.Should().NotThrowAsync();
    }

    // Test helper class
    private class SagaDataWithTimeout : OrderSagaData, ISagaDataWithTimeout
    {
        public TimeSpan Timeout => TimeSpan.FromMinutes(1);
    }
}
