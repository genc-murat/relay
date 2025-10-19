using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Services;
using Moq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaExecutionCompensationTests
{
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
}