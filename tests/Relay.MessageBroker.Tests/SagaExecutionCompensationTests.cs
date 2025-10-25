using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Interfaces;
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

    [Fact]
    public async Task ExecuteAsync_CompensationWithTimeoutException_ShouldRetry()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-017",
            Amount = 100m,
            FailAtStep = "ProcessPayment",
            CompensationExceptionType = "TimeoutException" // Throw TimeoutException during compensation
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Compensation should retry TimeoutException and eventually succeed
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        // Since TimeoutException is retryable, compensation should succeed after retries
        Assert.True(result.CompensationSucceeded);
    }

    [Fact]
    public async Task ExecuteAsync_CompensationWithHttpRequestException_ShouldRetry()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-018",
            Amount = 100m,
            FailAtStep = "ProcessPayment",
            CompensationExceptionType = "HttpRequestException" // Throw HttpRequestException during compensation
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Compensation should retry HttpRequestException and eventually succeed
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        // Since HttpRequestException is retryable, compensation should succeed after retries
        Assert.True(result.CompensationSucceeded);
    }

    [Fact]
    public async Task ExecuteAsync_CompensationWithIOException_ShouldRetry()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-019",
            Amount = 100m,
            FailAtStep = "ProcessPayment",
            CompensationExceptionType = "IOException" // Throw IOException during compensation
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Compensation should retry IOException and eventually succeed
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        // Since IOException is retryable, compensation should succeed after retries
        Assert.True(result.CompensationSucceeded);
    }

    [Fact]
    public async Task ExecuteAsync_CompensationWithSocketException_ShouldRetry()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-020",
            Amount = 100m,
            FailAtStep = "ProcessPayment",
            CompensationExceptionType = "SocketException" // Throw SocketException during compensation
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Compensation should retry SocketException and eventually succeed
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ProcessPayment", result.FailedStep);
        // Since SocketException is retryable, compensation should succeed after retries
        Assert.True(result.CompensationSucceeded);
    }

    [Fact]
    public async Task ExecuteAsync_CompensationWithInvalidOperationException_ShouldNotRetry()
    {
        // Arrange
        var saga = new TestOrderSaga();
        var data = new OrderSagaData
        {
            OrderId = "ORDER-021",
            Amount = 100m,
            FailAtStep = "ShipOrder", // Fail at the last step so first two steps execute and need compensation
            CompensationExceptionType = "InvalidOperationException" // Throw InvalidOperationException during ProcessPayment compensation
        };

        // Act
        var result = await saga.ExecuteAsync(data);

        // Assert - Compensation should not retry InvalidOperationException and should fail
        Assert.False(result.IsSuccess);
        Assert.Equal(SagaState.Compensated, result.Data.State);
        Assert.Equal("ShipOrder", result.FailedStep);
        // Since InvalidOperationException is not retryable, compensation should fail
        Assert.False(result.CompensationSucceeded);
    }
}