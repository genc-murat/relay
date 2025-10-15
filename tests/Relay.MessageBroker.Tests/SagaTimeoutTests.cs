using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
        Assert.Equal(2, result.CheckedCount);
        Assert.Equal(1, result.TimedOutCount);

        // Verify timed out saga was marked for compensation
        var updatedSaga = await persistence.GetByIdAsync(timedOutSaga.SagaId);
        Assert.NotNull(updatedSaga);
        Assert.Equal(SagaState.Compensating, updatedSaga!.State);
        Assert.True(updatedSaga.Metadata.ContainsKey("TimedOut"));
        Assert.Equal(true, updatedSaga.Metadata["TimedOut"]);
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
        Assert.Equal(1, result.CheckedCount);
        Assert.Equal(1, result.TimedOutCount);

        // Verify saga was marked as failed
        var updatedSaga = await persistence.GetByIdAsync(timedOutSaga.SagaId);
        Assert.NotNull(updatedSaga);
        Assert.Equal(SagaState.Failed, updatedSaga!.State);
        Assert.True(updatedSaga.Metadata.ContainsKey("CompensationTimedOut"));
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
        Assert.Equal(1, result.CheckedCount);
        Assert.Equal(1, result.TimedOutCount);

        var updatedSaga = await persistence.GetByIdAsync(sagaWithCustomTimeout.SagaId);
        Assert.Equal(SagaState.Compensating, updatedSaga!.State);
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
        Assert.Equal(0, result.CheckedCount);
        Assert.Equal(0, result.TimedOutCount);
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
        Assert.Equal(1, result.CheckedCount);
        Assert.Equal(1, result.TimedOutCount);
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
        Assert.Equal(5, result.CheckedCount);
        Assert.Equal(5, result.TimedOutCount);
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
        var exception = await Record.ExceptionAsync(async () => await handler.CheckAndHandleTimeoutsAsync(
            TimeSpan.FromMinutes(5),
            cts.Token));

        Assert.Null(exception);
    }

    // Test helper class
    private class SagaDataWithTimeout : OrderSagaData, ISagaDataWithTimeout
    {
        public TimeSpan Timeout => TimeSpan.FromMinutes(1);
    }

    #region SagaTimeoutService Tests

    [Fact]
    public void SagaTimeoutService_Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<SagaTimeoutService>>();
        var checkInterval = TimeSpan.FromSeconds(45);
        var defaultTimeout = TimeSpan.FromMinutes(10);

        // Act
        var service = new SagaTimeoutService(serviceProvider.Object, logger.Object, checkInterval, defaultTimeout);

        // Assert - Constructor validation is implicit through successful creation
        Assert.NotNull(service);
    }

    [Fact]
    public void SagaTimeoutService_Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<SagaTimeoutService>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SagaTimeoutService(null!, logger.Object));

        Assert.Equal("serviceProvider", exception.ParamName);
    }

    [Fact]
    public void SagaTimeoutService_Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SagaTimeoutService(serviceProvider.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void SagaTimeoutService_Constructor_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<SagaTimeoutService>>();

        // Act
        var service = new SagaTimeoutService(serviceProvider.Object, logger.Object);

        // Assert - We can't directly test private fields, but constructor should succeed
        Assert.NotNull(service);
    }



    [Fact]
    public async Task SagaTimeoutService_CheckAndHandleTimeoutsAsync_ShouldProcessAllHandlers()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        var scopedProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<SagaTimeoutService>>();

        var handler1 = new Mock<ISagaTimeoutHandler>();
        var handler2 = new Mock<ISagaTimeoutHandler>();

        handler1.Setup(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SagaTimeoutCheckResult { CheckedCount = 5, TimedOutCount = 1 });
        handler2.Setup(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SagaTimeoutCheckResult { CheckedCount = 3, TimedOutCount = 0 });

        serviceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);
        scopeFactory.Setup(sf => sf.CreateScope()).Returns(scope.Object);
        scope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);
        scopedProvider.Setup(sp => sp.GetService(typeof(IEnumerable<ISagaTimeoutHandler>))).Returns(new[] { handler1.Object, handler2.Object });

        var service = new SagaTimeoutService(serviceProvider.Object, logger.Object);

        // Act
        var result = await (Task<SagaTimeoutCheckResult>)service.GetType()
            .GetMethod("CheckAndHandleTimeoutsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, new object[] { CancellationToken.None })!;

        // Assert
        handler1.Verify(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        handler2.Verify(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(8, result.CheckedCount);
        Assert.Equal(1, result.TimedOutCount);
    }

    [Fact]
    public async Task SagaTimeoutService_CheckAndHandleTimeoutsAsync_HandlerThrowsException_ShouldLogErrorAndContinue()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        var scopedProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<SagaTimeoutService>>();

        var goodHandler = new Mock<ISagaTimeoutHandler>();
        var badHandler = new Mock<ISagaTimeoutHandler>();

        goodHandler.Setup(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SagaTimeoutCheckResult { CheckedCount = 2, TimedOutCount = 0 });
        badHandler.Setup(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler error"));

        serviceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);
        scopeFactory.Setup(sf => sf.CreateScope()).Returns(scope.Object);
        scope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);
        scopedProvider.Setup(sp => sp.GetService(typeof(IEnumerable<ISagaTimeoutHandler>))).Returns(new[] { goodHandler.Object, badHandler.Object });

        var service = new SagaTimeoutService(serviceProvider.Object, logger.Object);

        // Act
        var result = await (Task<SagaTimeoutCheckResult>)service.GetType()
            .GetMethod("CheckAndHandleTimeoutsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, new object[] { CancellationToken.None })!;

        // Assert
        goodHandler.Verify(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        badHandler.Verify(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, result.CheckedCount);
        Assert.Equal(0, result.TimedOutCount);

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error in timeout handler")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task SagaTimeoutService_CheckAndHandleTimeoutsAsync_WithTimeouts_ReturnsCorrectResult()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        var scopedProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<SagaTimeoutService>>();

        var handler = new Mock<ISagaTimeoutHandler>();
        handler.Setup(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SagaTimeoutCheckResult { CheckedCount = 10, TimedOutCount = 3 });

        serviceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);
        scopeFactory.Setup(sf => sf.CreateScope()).Returns(scope.Object);
        scope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);
        scopedProvider.Setup(sp => sp.GetService(typeof(IEnumerable<ISagaTimeoutHandler>))).Returns(new[] { handler.Object });

        var service = new SagaTimeoutService(serviceProvider.Object, logger.Object);

        // Act - Call the private method via reflection
        var result = await (Task<SagaTimeoutCheckResult>)service.GetType()
            .GetMethod("CheckAndHandleTimeoutsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, new object[] { CancellationToken.None })!;

        // Assert - The method completed without throwing
        Assert.NotNull(result);
        Assert.Equal(10, result.CheckedCount);
        Assert.Equal(3, result.TimedOutCount);
    }

    [Fact]
    public async Task SagaTimeoutService_CheckAndHandleTimeoutsAsync_NoTimeouts_ReturnsCorrectResult()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        var scopedProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<SagaTimeoutService>>();

        var handler = new Mock<ISagaTimeoutHandler>();
        handler.Setup(h => h.CheckAndHandleTimeoutsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SagaTimeoutCheckResult { CheckedCount = 5, TimedOutCount = 0 });

        serviceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);
        scopeFactory.Setup(sf => sf.CreateScope()).Returns(scope.Object);
        scope.Setup(s => s.ServiceProvider).Returns(scopedProvider.Object);
        scopedProvider.Setup(sp => sp.GetService(typeof(IEnumerable<ISagaTimeoutHandler>))).Returns(new[] { handler.Object });

        var service = new SagaTimeoutService(serviceProvider.Object, logger.Object);

        // Act - Call the private method via reflection
        var result = await (Task<SagaTimeoutCheckResult>)service.GetType()
            .GetMethod("CheckAndHandleTimeoutsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(service, new object[] { CancellationToken.None })!;

        // Assert - The method completed without throwing
        Assert.NotNull(result);
        Assert.Equal(5, result.CheckedCount);
        Assert.Equal(0, result.TimedOutCount);
    }



    #endregion
}
